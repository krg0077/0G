using System.Collections.Generic;
using UnityEngine;

namespace _0G
{
    public class Clock : MonoBehaviour
    {
        private static readonly List<Clock> s_Instances = new List<Clock>();

        public static int InstanceCount => s_Instances.Count;

        public static void Setup(GameObject anchor)
        {
#if CHRONOS_TIME_CONTROL
            anchor.NewChildGameObjectTyped<Chronos.Timekeeper>("Timekeeper").AddClock("Root");
#else
            anchor.AddComponent<Clock>();
#endif
        }

        public static Clock GetInstance(int index)
        {
            return s_Instances[index];
        }

        private void Awake()
        {
            s_Instances.Add(this);
        }

        private void OnDestroy()
        {
            s_Instances.Remove(this);
        }
    }
}