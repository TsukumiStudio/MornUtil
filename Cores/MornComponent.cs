using UnityEngine;

namespace MornLib
{
    public static class MornComponent
    {
        public static bool TryFindOnlyOneComponent<T>(out T result) where T : MonoBehaviour
        {
            var objects = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            if (objects.Length == 1)
            {
                result = objects[0];
                return true;
            }

            result = null;
            return false;
        }
    }
}