using System.Collections.Generic;
using UnityEngine;

namespace _0G
{
    public class Gate
    {
        // EVENTS AND DELEGATES

        public event LockHandler Locked;

        public delegate void LockHandler(bool isLocked, object lockingObject, Options options);

        public delegate float OutputValueHandler();

        // STRUCTS

        public struct Options
        {
            public float AnimateTime;
        }

        // FIELDS

        private readonly Dictionary<object, OutputValueHandler> m_Filters = new Dictionary<object, OutputValueHandler>();

        private readonly List<object> m_Locks = new List<object>();

        private float m_Output = -1;

        // PROPERTIES

        public bool IsLocked => m_Locks.Count > 0;

        // METHODS

        public void AddFilter(object filteringObject, OutputValueHandler outputFn)
        {
            Debug.Assert(filteringObject != null && outputFn != null);

            if (m_Filters.ContainsKey(filteringObject))
            {
                m_Filters[filteringObject] = outputFn;
            }
            else
            {
                m_Filters.Add(filteringObject, outputFn);
            }

            m_Output = -1;
        }

        public void RemoveFilter(object filteringObject)
        {
            Debug.Assert(filteringObject != null);

            if (!m_Filters.ContainsKey(filteringObject)) return;

            m_Filters.Remove(filteringObject);

            m_Output = -1;
        }

        public void SetLock(object lockingObject, bool isLocking, Options options = new Options())
        {
            if (isLocking)
            {
                AddLock(lockingObject, options);
            }
            else
            {
                RemoveLock(lockingObject, options);
            }
        }

        public void AddLock(object lockingObject, Options options = new Options())
        {
            Debug.Assert(lockingObject != null);

            if (m_Locks.Contains(lockingObject)) return;

            m_Locks.Add(lockingObject);

            if (m_Locks.Count == 1)
            {
                Locked?.Invoke(true, lockingObject, options);
            }
        }

        public void RemoveLock(object lockingObject, Options options = new Options())
        {
            Debug.Assert(lockingObject != null);

            if (!m_Locks.Contains(lockingObject)) return;

            m_Locks.Remove(lockingObject);

            if (m_Locks.Count == 0)
            {
                Locked?.Invoke(false, lockingObject, options);
            }
        }

        public float GetOutput()
        {
            if (IsLocked) return 0;

            if (m_Output < 0)
            {
                m_Output = 1;

                foreach (var fn in m_Filters.Values)
                {
                    m_Output *= Mathf.Clamp(fn(), 0, 1);
                }
            }

            return m_Output;
        }
    }
}