using UnityEngine;

namespace _0G
{
    public class CinematicTrigger : MonoBehaviour
    {
        public CinematicSequence Sequence;

        private void OnTriggerEnter(Collider other)
        {
            if (!enabled || !other.CompareTag("Player")) return;
            enabled = false;
            CinematicDirector.Instance.RunSequence(Sequence);
        }
    }
}