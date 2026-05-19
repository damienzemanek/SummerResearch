using System.Linq;
using UnityEngine;

namespace EMILtools.Extensions
{
    public static class GOEX
    {
        public static GameObject SetActiveThen(this GameObject gameObject, bool val)
        {
            gameObject.SetActive(val);
            return gameObject;
        }

        public static void FillWithChildren(this GameObject[] array, GameObject parent)
        {
            if (parent == null) return;

            var children = parent.transform.Cast<Transform>()
                .Select(t => t.gameObject)
                .Take(array.Length)
                .ToArray();

            for (int i = 0; i < children.Length; i++)
                array[i] = children[i];
        }

        public static GameObject[] Children(this Transform parent)
        {
            if (parent == null)
                return System.Array.Empty<GameObject>();

            return parent.Cast<Transform>()
                .Select(t => t.gameObject)
                .ToArray();
        }
    }

}