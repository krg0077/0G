using System.Collections.Generic;
using UnityEngine;

namespace _0G
{
    public class Gate : Lock
    {
        // EVENTS AND DELEGATES

        public delegate float OutputValueHandler();

        // FIELDS

        private readonly Dictionary<object, OutputValueHandler> m_Filters = new Dictionary<object, OutputValueHandler>();

        private float m_Output = -1;

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