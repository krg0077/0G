using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace _0G
{
    [PreferBinarySerialization]
    public class ElanicData : ScriptableObject
    {
        // CONSTANTS

        public const string SUFFIX = "_ElanicData";

        // SERIALIZED FIELDS

#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        public int serializedVersion = 1;

        public List<Texture2D> Imprints = new List<Texture2D>();
        public List<Color32> Colors = new List<Color32>();
        public List<ElanicFrame> Frames = new List<ElanicFrame>();
    }
}