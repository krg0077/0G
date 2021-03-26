using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace _0G
{
    [System.Serializable]
    public struct ElanicFrame
    {
        public int ImprintIndex;
        
        [HideInInspector]
        public uint[] DiffPixelPosition;
        [HideInInspector]
        public short[] DiffPixelColorIndex;

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public int DiffPixelCount => DiffPixelColorIndex?.Length ?? 0;

        public bool HasDiffData => DiffPixelCount > 0;
    }
}