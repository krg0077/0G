using System.Collections.Generic;
using UnityEngine;

namespace _0G
{
    public class Lock
    {
        // EVENTS AND DELEGATES

        public event LockHandler Locked;

        public delegate void LockHandler(bool isLocked, object lockingObject, Options options);
        
        // STRUCTS

        public struct Options
        {
            public float AnimateTime;
        }

        // FIELDS
        
        private readonly List<object> m_Locks = new List<object>();

        // PROPERTIES

        public bool IsLocked => m_Locks.Count > 0;
        
        // METHODS
        
        public void SetLock(bool isLocking, object lockingObject, Options options = new Options())
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
            if (IsSet(lockingObject) && !m_Locks.Contains(lockingObject))
                AddLockInternal(lockingObject, options);
        }

        public void RemoveLock(object lockingObject, Options options = new Options())
        {
            if (IsSet(lockingObject) && m_Locks.Contains(lockingObject))
                RemoveLockInternal(lockingObject, options);
        }

        public void InvertLock(object lockingObject, Options options = new Options())
        {
            if (!IsSet(lockingObject)) return;
            if (!m_Locks.Contains(lockingObject))
            {
                AddLockInternal(lockingObject, options);
            }
            else
            {
                RemoveLockInternal(lockingObject, options);
            }
        }

        private void AddLockInternal(object lockingObject, Options options)
        {
            m_Locks.Add(lockingObject);
            if (m_Locks.Count == 1) // if first lock added
                Locked?.Invoke(true, lockingObject, options);
        }

        private void RemoveLockInternal(object lockingObject, Options options)
        {
            m_Locks.Remove(lockingObject);
            if (m_Locks.Count == 0) // if last lock removed
                Locked?.Invoke(false, lockingObject, options);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private static bool IsSet(object objectToCheck)
        {
            if (objectToCheck != null) return true;
            Debug.LogError("The object is null.");
            return false;
        }
    }
}