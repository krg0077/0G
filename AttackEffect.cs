using _0G.Legacy;
using UnityEngine;

namespace _0G
{
    public abstract class AttackEffect : MonoBehaviour
    {
        protected Attack m_Attack;

        private void Awake()
        {
            m_Attack = GetComponent<Attack>();
        }
    }
}