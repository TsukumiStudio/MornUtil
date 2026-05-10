using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MornLib
{
    public sealed class MornAssetBrowser : EditorWindow
    {
        private const string PrefKey = "MornUtil.AssetBrowser.TypeFilter";
        private const string PrefKeyMax = "MornUtil.AssetBrowser.MaxItems";
        private const string PrefKeyExcludePackages = "MornUtil.AssetBrowser.ExcludePackages";
        private const float RowHeight = 18f;
        private string _typeFilter = "";
        private string _searchFilter = "";
        private int _maxItems = 200;
        private bool _excludePackages = true;
        private Vector2 _scroll;
        private readonly List<(string path, Type type, string name)> _all = new();
        private readonly List<(string path, Type type, string name)> _shown = new();

        [MenuItem("Tools/MornUtil/Asset Browser")]
        private static void Open()
        {
            GetWindow<MornAssetBrowser>("Asset Browser").Show();
        }

        private void OnEnable()
        {
            _typeFilter = EditorPrefs.GetString(PrefKey, "");
            _maxItems = EditorPrefs.GetInt(PrefKeyMax, 200);
            _excludePackages = EditorPrefs.GetBool(PrefKeyExcludePackages, true);
            ReloadAll();
            ApplyFilter();
        }

        private void OnFocus()
        {
            ReloadAll();
            ApplyFilter();
        }

        private void ReloadAll()
        {
            _all.Clear();
            string[] searchFolders = _excludePackages ? new[] { "Assets" } : null;
            var guids = searchFolders != null
                ? AssetDatabase.FindAssets("t:Object", searchFolders)
                : AssetDatabase.FindAssets("t:Object");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type == null) continue;
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                _all.Add((path, type, name));
            }
            _all.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }

        private void ApplyFilter()
        {
            _shown.Clear();
            var f = _searchFilter ?? "";
            var filterType = string.IsNullOrEmpty(_typeFilter) ? null : Type.GetType(_typeFilter);
            foreach (var entry in _all)
            {
                if (filterType != null && IsTypeMatch(filterType, entry.type) == false) continue;
                if (string.IsNullOrEmpty(f) == false
                    && entry.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0
                    && entry.type.Name.IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                _shown.Add(entry);
            }
        }

        private static bool IsTypeMatch(Type filterType, Type assetType)
        {
            if (filterType.IsGenericTypeDefinition)
            {
                for (var t = assetType; t != null; t = t.BaseType)
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == filterType) return true;
                }
                return false;
            }
            return filterType.IsAssignableFrom(assetType);
        }

        private void SetTypeFilter(string aqn)
        {
            _typeFilter = aqn ?? "";
            EditorPrefs.SetString(PrefKey, _typeFilter);
            ApplyFilter();
            Repaint();
        }

        private string CurrentTypeLabel()
        {
            if (string.IsNullOrEmpty(_typeFilter)) return "All Types";
            var t = Type.GetType(_typeFilter);
            return t != null ? t.Name : "All Types";
        }

        private void ShowTypeMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent($"All  ({_all.Count} assets)"), string.IsNullOrEmpty(_typeFilter), () => SetTypeFilter(""));
            menu.AddSeparator("");
            var counts = new Dictionary<Type, int>();
            foreach (var entry in _all)
            {
                for (var t = entry.type; t != null && t != typeof(Object) && t != typeof(object); t = t.BaseType)
                {
                    var key = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
                    counts.TryGetValue(key, out var c);
                    counts[key] = c + 1;
                }
            }
            foreach (var kv in counts.OrderBy(kv => kv.Key.FullName ?? kv.Key.Name, StringComparer.Ordinal))
            {
                var t = kv.Key;
                var ns = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace.Replace('.', '/');
                var displayName = t.IsGenericTypeDefinition ? t.Name.Substring(0, t.Name.IndexOf('`')) + "<>" : t.Name;
                var suffix = t.IsAbstract || t.IsGenericTypeDefinition ? " [base]" : "";
                var path = $"{ns}/{displayName}{suffix}  ({kv.Value} assets)";
                var aqn = t.AssemblyQualifiedName;
                menu.AddItem(new GUIContent(path), _typeFilter == aqn, () => SetTypeFilter(aqn));
            }
            menu.ShowAsContext();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button($"Filter: {CurrentTypeLabel()} ▾", EditorStyles.toolbarDropDown, GUILayout.MinWidth(180)))
                {
                    ShowTypeMenu();
                }
                var newSearch = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
                if (newSearch != _searchFilter)
                {
                    _searchFilter = newSearch;
                    ApplyFilter();
                }
                GUILayout.Label("Max", EditorStyles.miniLabel, GUILayout.Width(28));
                var newMax = EditorGUILayout.IntField(_maxItems, EditorStyles.toolbarTextField, GUILayout.Width(50));
                if (newMax != _maxItems)
                {
                    _maxItems = Mathf.Max(1, newMax);
                    EditorPrefs.SetInt(PrefKeyMax, _maxItems);
                }
                var newExclude = GUILayout.Toggle(_excludePackages, "Assets only", EditorStyles.toolbarButton, GUILayout.Width(80));
                if (newExclude != _excludePackages)
                {
                    _excludePackages = newExclude;
                    EditorPrefs.SetBool(PrefKeyExcludePackages, _excludePackages);
                    ReloadAll();
                    ApplyFilter();
                }
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ReloadAll();
                    ApplyFilter();
                }
            }
            var displayCount = Mathf.Min(_shown.Count, _maxItems);
            var truncated = _shown.Count > _maxItems ? $" (truncated, showing {displayCount})" : "";
            EditorGUILayout.LabelField($"{_shown.Count} / {_all.Count} assets{truncated}", EditorStyles.miniLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < displayCount; i++)
            {
                DrawAssetRow(_shown[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetRow((string path, Type type, string name) entry)
        {
            var rect = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
            var selectedPath = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            var isSelected = selectedPath == entry.path;
            if (isSelected)
            {
                var bg = focusedWindow == this
                    ? new Color(0.24f, 0.49f, 0.91f, 0.6f)
                    : new Color(0.45f, 0.45f, 0.45f, 0.5f);
                EditorGUI.DrawRect(rect, bg);
            }
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(entry.path);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
                Event.current.Use();
                Repaint();
                return;
            }
            var iconRect = new Rect(rect.x + 2, rect.y + 1, 16, 16);
            var labelRect = new Rect(rect.x + 22, rect.y, rect.width - 22, rect.height);
            var icon = AssetDatabase.GetCachedIcon(entry.path);
            if (icon != null) GUI.DrawTexture(iconRect, icon);
            var labelStyle = isSelected ? EditorStyles.whiteLabel : EditorStyles.label;
            GUI.Label(labelRect, entry.name, labelStyle);
        }
    }
}
