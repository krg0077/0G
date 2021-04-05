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
    }
}