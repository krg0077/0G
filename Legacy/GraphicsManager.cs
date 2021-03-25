using UnityEngine;

namespace _0G.Legacy
{
    public enum GraphicsTextureQuality { Standard, Lossless, }

    public class GraphicsManager : Manager
    {
        public override float priority => 70;

        protected PersistentData<int> m_TextureQuality = new PersistentData<int>(
            Persist.PlayerPrefs, "0G.graphics.texture_quality");

        /// <summary>
        /// Get or set the texture quality.
        /// </summary>
        public GraphicsTextureQuality TextureQuality
        {
            get => (GraphicsTextureQuality)m_TextureQuality.Value;
            set => m_TextureQuality.Value = (int)value;
        }

        public override void Awake()
        {
            if (m_TextureQuality.HasStoredValue)
            {
                m_TextureQuality.LoadValue();
            }
            else
            {
                TextureQuality = GetDefaultTextureQuality();
            }
        }

        public static GraphicsTextureQuality GetDefaultTextureQuality()
        {
            return SystemInfo.systemMemorySize > 8000 ? GraphicsTextureQuality.Lossless : GraphicsTextureQuality.Standard;
        }
    }
}