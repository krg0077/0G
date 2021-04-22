using System.Collections.Generic;
using UnityEngine;

namespace _0G
{
    public static class ExtensionMethods
    {
        // GAME OBJECT

        public static GameObject NewChildGameObject<T>(this GameObject parentGameObject, string name = null) where T : Component
        {
            GameObject go;
            System.Type type = typeof(T);
            name = name ?? type.ToString();
            if (type == typeof(Transform))
            {
                go = new GameObject(name);
            }
            else
            {
                go = new GameObject(name, type);
            }
            go.transform.parent = parentGameObject.transform;
            return go;
        }

        public static T NewChildGameObjectTyped<T>(this GameObject parentGameObject, string name = null) where T : Component
            => parentGameObject.NewChildGameObject<T>(name).GetComponent<T>();

        // LIST <T>

        public static void Shift<T>(this List<T> list, T item, int indexDelta)
        {
            int index = list.IndexOf(item);
            list.RemoveAt(index);
            list.Insert(index + indexDelta, item);
        }

        // RECT TRANSFORM

        public static void SetAnchoredPosition(this RectTransform rectTransform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 p = rectTransform.anchoredPosition;
            if (x.HasValue) p.x = x.Value;
            if (y.HasValue) p.y = y.Value;
            if (z.HasValue) p.z = z.Value;
            rectTransform.anchoredPosition = p;
        }

        // TRANSFORM

        public static void SetPosition(this Transform transform, float? x = null, float? y = null, float? z = null )
        {
            Vector3 p = transform.position;
            if (x.HasValue) p.x = x.Value;
            if (y.HasValue) p.y = y.Value;
            if (z.HasValue) p.z = z.Value;
            transform.position = p;
        }
        
        // UNITY ENGINE OBJECT

        /// <summary>
        /// Is the object reference not equal to null?
        /// </summary>
        public static bool IsSet(this Object obj) => !ReferenceEquals(obj, null);
    }
}