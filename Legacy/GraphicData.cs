using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace _0G.Legacy
{
    [System.Serializable]
    public struct GraphicData
    {
        public bool ExcludeFromGallery;

#if ODIN_INSPECTOR
        [HideIf("ExcludeFromGallery")]
#endif
        public int GalleryOrderOffset;

        public Material BaseSharedMaterial;

        public Texture2D EditorSprite;

        public List<StateAnimation> StateAnimations;
    }
}