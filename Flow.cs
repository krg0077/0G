using UnityEngine;

namespace _0G
{
    public class Flow : MonoBehaviour
    {
        public static Flow Instance { get; private set; }

        public static void Setup(GameObject anchor)
        {
            anchor.AddComponent<Flow>();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Debug.Log("0G");
        }

        private void Update()
        {
            if (Input.KeyDown(InputKey.UISubmit)) Debug.Log("Submit");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}