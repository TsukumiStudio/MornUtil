using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// PolygonCollider2D のポイントを World 経由で別 PolygonCollider2D に移植するコンテキストメニュー
    /// Transform の Scale / Rotation / Position が異なっていても見た目が同じになるように World 座標で受け渡す
    /// </summary>
    internal static class PolygonCollider2DContextMenu
    {
        private const string CopyMenuPath = "CONTEXT/PolygonCollider2D/ポイントをコピー(World)";
        private const string PasteMenuPath = "CONTEXT/PolygonCollider2D/ポイントを貼り付け(World)";
        private const string MergeMenuPath = "CONTEXT/PolygonCollider2D/同じGameObjectのPolygonCollider2Dを1つにまとめる";

        private static Vector2[][] _clipboardWorldPaths;

        [MenuItem(CopyMenuPath)]
        private static void CopyPointsAsWorld(MenuCommand command)
        {
            var src = command.context as PolygonCollider2D;
            if (src == null)
            {
                return;
            }

            var pathCount = src.pathCount;
            var paths = new Vector2[pathCount][];
            var totalPoints = 0;
            for (var i = 0; i < pathCount; i++)
            {
                var localPoints = src.GetPath(i);
                var worldPoints = new Vector2[localPoints.Length];
                for (var j = 0; j < localPoints.Length; j++)
                {
                    var local = (Vector3)(localPoints[j] + src.offset);
                    var world = src.transform.TransformPoint(local);
                    worldPoints[j] = new Vector2(world.x, world.y);
                }

                paths[i] = worldPoints;
                totalPoints += worldPoints.Length;
            }

            _clipboardWorldPaths = paths;
            Debug.Log($"[PolygonCollider2D] {pathCount} path / {totalPoints} 点を World 座標でコピーしました");
        }

        [MenuItem(PasteMenuPath, true)]
        private static bool ValidatePastePoints(MenuCommand command)
        {
            return _clipboardWorldPaths != null && _clipboardWorldPaths.Length > 0;
        }

        [MenuItem(PasteMenuPath)]
        private static void PastePointsFromWorld(MenuCommand command)
        {
            var dst = command.context as PolygonCollider2D;
            if (dst == null || _clipboardWorldPaths == null)
            {
                return;
            }

            Undo.RecordObject(dst, "Paste PolygonCollider2D Points");
            dst.pathCount = _clipboardWorldPaths.Length;
            var totalPoints = 0;
            for (var i = 0; i < _clipboardWorldPaths.Length; i++)
            {
                var worldPoints = _clipboardWorldPaths[i];
                var localPoints = new Vector2[worldPoints.Length];
                for (var j = 0; j < worldPoints.Length; j++)
                {
                    var localWithOffset = dst.transform.InverseTransformPoint(worldPoints[j]);
                    localPoints[j] = new Vector2(localWithOffset.x, localWithOffset.y) - dst.offset;
                }

                dst.SetPath(i, localPoints);
                totalPoints += localPoints.Length;
            }

            EditorUtility.SetDirty(dst);
            Debug.Log($"[PolygonCollider2D] {dst.pathCount} path / {totalPoints} 点を {dst.name} に貼り付けました");
        }

        [MenuItem(MergeMenuPath, true)]
        private static bool ValidateMergeColliders(MenuCommand command)
        {
            var target = command.context as PolygonCollider2D;
            return target != null && target.GetComponents<PolygonCollider2D>().Length >= 2;
        }

        [MenuItem(MergeMenuPath)]
        private static void MergeColliders(MenuCommand command)
        {
            var target = command.context as PolygonCollider2D;
            if (target == null)
            {
                return;
            }

            var siblings = target.GetComponents<PolygonCollider2D>();
            if (siblings.Length < 2)
            {
                return;
            }

            var merged = new List<Vector2[]>();
            foreach (var c in siblings)
            {
                var delta = c.offset - target.offset;
                for (var i = 0; i < c.pathCount; i++)
                {
                    var src = c.GetPath(i);
                    var converted = new Vector2[src.Length];
                    for (var j = 0; j < src.Length; j++)
                    {
                        converted[j] = src[j] + delta;
                    }

                    merged.Add(converted);
                }
            }

            Undo.RecordObject(target, "Merge PolygonCollider2D");
            target.pathCount = merged.Count;
            for (var i = 0; i < merged.Count; i++)
            {
                target.SetPath(i, merged[i]);
            }

            foreach (var c in siblings)
            {
                if (c != target)
                {
                    Undo.DestroyObjectImmediate(c);
                }
            }

            EditorUtility.SetDirty(target);
            Debug.Log($"[PolygonCollider2D] {siblings.Length} 個の PolygonCollider2D を {target.name} に統合 ({merged.Count} path)");
        }
    }
}
