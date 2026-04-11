using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MornLib
{
    public static class MornTransformEx
    {
        private static readonly StringBuilder _stringBuilder = new();
        private static readonly List<string> _cachedList = new();

        public static void DestroyChildren(this Transform transform)
        {
            var totalCount = transform.childCount;
            for (var i = totalCount - 1; i >= 0; i--)
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
        }

        public static string GetPath(this Transform transform)
        {
            _cachedList.Clear();
            var parent = transform;
            while (parent != null)
            {
                _cachedList.Add(parent.name);
                parent = parent.parent;
            }

            _cachedList.Reverse();
            _stringBuilder.Clear();
            foreach (var name in _cachedList)
            {
                _stringBuilder.Append("/").Append(name);
            }

            return _stringBuilder.ToString();
        }

        public static Vector3 GetConvertedDifUsingLocalAxis(this Transform transform, Vector3 dif)
        {
            return transform.right * dif.x + transform.up * dif.y + transform.forward * dif.z;
        }

        public static void PositionLerp(this Transform transform, Vector3 aim, float k)
        {
            transform.position = Vector3.Lerp(transform.position, aim, Mathf.Clamp01(k));
        }

        public static void LocalPositionLerp(this Transform transform, Vector3 aim, float k)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, aim, Mathf.Clamp01(k));
        }

        public static void RotationLerp(this Transform transform, Quaternion aim, float k)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, aim, Mathf.Clamp01(k));
        }

        public static void LocalRotationLerp(this Transform transform, Quaternion aim, float k)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, aim, Mathf.Clamp01(k));
        }

        public static void EulerAnglesLerp(this Transform transform, Vector3 aim, float k)
        {
            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, aim, Mathf.Clamp01(k));
        }

        public static void LocalEulerAnglesLerp(this Transform transform, Vector3 aim, float k)
        {
            transform.localEulerAngles = Vector3.Lerp(transform.localEulerAngles, aim, Mathf.Clamp01(k));
        }
    }
}