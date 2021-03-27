using System.IO;
using _0G.Legacy;
using UnityEngine;
using UnityEditor;

namespace _0G
{
    // ELANIC (Experimental Lossless Animation Compression)
    public class ElanicEditor : EditorWindow
    {
        // FIELDS

        private RasterAnimation m_RasterAnimation;

        // METHODS

        [MenuItem("0G/ELANIC Editor")]
        public static void ShowWindow()
        {
            _ = GetWindow(typeof(ElanicEditor), false, "ELANIC Editor");
        }

        private void OnGUI()
        {
            GUILayout.Space(4);

            m_RasterAnimation = (RasterAnimation)EditorGUILayout.ObjectField("Raster Animation", m_RasterAnimation, typeof(RasterAnimation), false);

            using (new EditorGUI.DisabledScope(m_RasterAnimation == null))
            {
                if (GUILayout.Button("Convert to ELANIC")) ConvertToElanic(m_RasterAnimation);
            }
        }

        public static void ConvertToElanic(RasterAnimation rasterAnimation)
        {
            string assetPath = AssetDatabase.GetAssetPath(rasterAnimation);
            string dirPath = Path.GetDirectoryName(assetPath);
            TextureImporterSettings originalSettings = null;
            int originalMaxTextureSize = 0;
            bool originalCrunchedCompression = false;
            TextureImporterCompression originalTextureCompression = default;

            // modify texture import settings
            foreach (Texture2D tex in rasterAnimation.FrameTextures)
            {
                string texPath = AssetDatabase.GetAssetPath(tex);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(texPath);
                if (originalSettings == null)
                {
                    originalSettings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(originalSettings);
                    originalMaxTextureSize = textureImporter.maxTextureSize;
                    originalCrunchedCompression = textureImporter.crunchedCompression;
                    originalTextureCompression = textureImporter.textureCompression;
                }
                textureImporter.npotScale = TextureImporterNPOTScale.None; // do not scale to power of 2
                textureImporter.isReadable = true; // read/write enabled
                textureImporter.maxTextureSize = 8192;
                textureImporter.crunchedCompression = false;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.SaveAndReimport();
            }
            Debug.LogFormat("Modified all PNG files in {0}.", rasterAnimation.name);

            // create the ELANIC data scriptable object asset
            ElanicData data = CreateInstance<ElanicData>();
            string animationName = rasterAnimation.name.Replace(RasterAnimation.SUFFIX, "");
            string dataPath = string.Format("{0}/{1}_ElanicData.asset", dirPath, animationName);
            if (Directory.Exists(dataPath)) AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(data, dataPath);
            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(rasterAnimation);
            rasterAnimation.ConvertToElanic(data);
            for (int i = 0; i < data.Imprints.Count; ++i)
            {
                // copy imprints; update raster references
                Texture2D tex = data.Imprints[i];
                string texPath = AssetDatabase.GetAssetPath(tex);
                string impPath = texPath.Insert(texPath.Length - 7, "Imprint_"); // e.g. SomeCharacter_SomeAnimation_Imprint_001.png
                Texture2D imp = AssetDatabase.LoadAssetAtPath<Texture2D>(impPath);
                if (imp == null)
                {
                    _ = AssetDatabase.CopyAsset(texPath, impPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.SetDirty(data);
                    imp = AssetDatabase.LoadAssetAtPath<Texture2D>(impPath);
                }
                data.Imprints[i] = imp;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("Created ELANIC Data at {0}.", dataPath);
            Debug.LogFormat("Copied imprints in {0}.", rasterAnimation.name);

            // revert texture import settings
            foreach (Texture2D tex in rasterAnimation.FrameTextures)
            {
                string texPath = AssetDatabase.GetAssetPath(tex);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(texPath);
                textureImporter.SetTextureSettings(originalSettings);
                textureImporter.maxTextureSize = originalMaxTextureSize;
                textureImporter.crunchedCompression = originalCrunchedCompression;
                textureImporter.textureCompression = originalTextureCompression;
                textureImporter.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.LogFormat("Reverted all PNG files in {0}.", rasterAnimation.name);
        }
    }
}