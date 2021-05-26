using _0G.Legacy;

namespace _0G
{
    [System.Serializable]
    public struct CinematicAction
    {
        [Enum(typeof(CinematicCommand))]
        public int Command;
        [Enum(typeof(CharacterID))]
        public int Character;

        public float Float1;
        public float Float2;
        public float Float3;

        public string String1;
    }
}