using UnityEngine;

namespace _0G
{
    //     0G   [ z e r o    g r a v i t y ]

    public class _0GLoader : MonoBehaviour
    {
        public static _0GLoader Instance { get; protected set; }

        protected virtual void Awake()
        {
            Instance = this;
            GameObject anchor = gameObject;
            Clock.Setup(anchor);
            Flow.Setup(anchor);
            Player.Setup(anchor);
            PlayerCharacter.Setup(anchor);
            CinematicDirector.Setup(anchor);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}