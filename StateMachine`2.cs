using System.Collections.Generic;
using UnityEngine;

namespace _0G
{
    public class StateMachine<TOwner, TState> where TOwner : Component
    {
        public delegate void StateChangedHandler(StateMachine<TOwner, TState> stateMachine, TState state, bool value);

        /// <summary>
        /// Fires whenever a state is changed.
        /// </summary>
        public static event StateChangedHandler StateChanged;

        /// <summary>
        /// An owner component for identification.
        /// </summary>
        public TOwner Owner { get; private set; }

        /// <summary>
        /// The states. Presence is true, absence is false.
        /// </summary>
        private readonly List<TState> m_States = new List<TState>();

        /// <summary>
        /// Construct a new state machine using this owner and state type.
        /// </summary>
        public StateMachine(TOwner owner) => Owner = owner;

        /// <summary>
        /// Reset all states to false.
        /// </summary>
        public void Clear() => m_States.Clear();

        /// <summary>
        /// Is or includes this state?
        /// </summary>
        public bool IsN(TState state) => m_States.Contains(state);

        /// <summary>
        /// Is exclusively this state?
        /// </summary>
        public bool IsX(TState state) => m_States.Contains(state) && m_States.Count == 1;

        /// <summary>
        /// Set this state to this value.
        /// </summary>
        public void Set(TState state, bool value)
        {
            if (value)
            {
                if (!m_States.Contains(state))
                {
                    m_States.Add(state);
                    StateChanged?.Invoke(this, state, value);
                }
            }
            else
            {
                if (m_States.Contains(state))
                {
                    m_States.Remove(state);
                    StateChanged?.Invoke(this, state, value);
                }
            }
        }
    }
}