#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MornLib
{
    internal static class DefineSymbolRegisterer
    {
        public static void Register(string symbolName)
        {
            EditorApplication.delayCall += () => ApplyToAllTargets(symbolName);
        }

        public static void RemoveObsoleteSymbols(string prefix, HashSet<string> validSymbols)
        {
            EditorApplication.delayCall += () => RemoveObsoleteSymbolsInternal(prefix, validSymbols);
        }

        private static void RemoveObsoleteSymbolsInternal(string prefix, HashSet<string> validSymbols)
        {
            var anyChanged = false;
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown || IsObsolete(group))
                {
                    continue;
                }

                try
                {
                    var named = NamedBuildTarget.FromBuildTargetGroup(group);
                    var defines = PlayerSettings.GetScriptingDefineSymbols(named).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    var toRemove = defines.Where(s => s.StartsWith(prefix) && !validSymbols.Contains(s)).ToList();
                    if (toRemove.Count == 0)
                    {
                        continue;
                    }

                    foreach (var symbol in toRemove)
                    {
                        defines.Remove(symbol);
                        Debug.Log($"[MornUtil] Defineシンボル[{symbol}]を{group}から削除しました。");
                    }

                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines));
                    anyChanged = true;
                }
                catch
                {
                    // 無効なBuildTargetGroupは無視
                }
            }

            if (anyChanged)
            {
                EditorUtility.RequestScriptReload();
            }
        }

        private static void ApplyToAllTargets(string symbolName)
        {
            var anyChanged = false;
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown || IsObsolete(group))
                {
                    continue;
                }

                anyChanged |= TryAddDefine(group, symbolName);
            }

            if (anyChanged)
            {
                Debug.Log($"[MornUtil] Defineシンボル[{symbolName}]をすべてのBuildTargetGroupに追加しました。");
                EditorUtility.RequestScriptReload();
            }
        }

        private static bool IsObsolete(BuildTargetGroup g)
        {
            var mem = typeof(BuildTargetGroup).GetMember(g.ToString()).FirstOrDefault();
            return mem != null && Attribute.IsDefined(mem, typeof(ObsoleteAttribute));
        }

        public static void SortAllDefineSymbols()
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown || IsObsolete(group))
                {
                    continue;
                }

                try
                {
                    var named = NamedBuildTarget.FromBuildTargetGroup(group);
                    var defines = PlayerSettings.GetScriptingDefineSymbols(named).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s).ToList();
                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines));
                }
                catch
                {
                    // 無効なBuildTargetGroupは無視
                }
            }
        }

        private static bool TryAddDefine(BuildTargetGroup group, string symbol)
        {
            try
            {
                var named = NamedBuildTarget.FromBuildTargetGroup(group);
                var defines = PlayerSettings.GetScriptingDefineSymbols(named).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (defines.Contains(symbol))
                {
                    return false;
                }

                defines.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", defines.Distinct()));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MornUtil] Defineシンボル[{symbol}]の追加に失敗: {e.Message}");
                return false;
            }
        }
    }
}
#endif