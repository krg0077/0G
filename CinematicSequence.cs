using UnityEngine;

namespace _0G
{
    [CreateAssetMenu(
        fileName = "SomeSequence_CinematicSequence.asset",
        menuName = "0G Scriptable Object/Cinematic Sequence",
        order = 111
    )]
    public class CinematicSequence : ScriptableObject
    {
        public CinematicAction[] Actions;
    }
}