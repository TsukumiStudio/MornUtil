#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornNoneReferenceSearchWindow : EditorWindow
    {
        private sealed class NoneReferenceInfo
        {
            public string AssetPath;
            public string GameObjectName;
            public string ComponentName;
            public string PropertyPath;
            public Object Asset;
        }

        private readonly List<NoneReferenceInfo> _results = new();
        private Vector2 _scrollPosition;
        private bool _searchPrefabs = true;
        private bool _searchScriptableObjects;
        private bool _searchScenes;
        private string _filter = "";
        private string _componentFilter = "";

        [MenuItem("Tools/MornUtil/None参照検索")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornNoneReferenceSearchWindow>("None参照検索");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("None参照検索", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SerializeField の ObjectReference が None (null) になっている箇所を検索します。", MessageType.Info);
            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("検索対象:", GUILayout.Width(60));
                _searchPrefabs = EditorGUILayout.ToggleLeft("Prefab", _searchPrefabs, GUILayout.Width(80));
                _searchScriptableObjects = EditorGUILayout.ToggleLeft("ScriptableObject", _searchScriptableObjects, GUILayout.Width(130));
                _searchScenes = EditorGUILayout.ToggleLeft("Scene", _searchScenes, GUILayout.Width(80));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _filter = EditorGUILayout.TextField("パスフィルター:", _filter);
                _componentFilter = EditorGUILayout.TextField("キーワード:", _componentFilter, GUILayout.Width(200));
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("検索", GUILayout.Height(30)))
            {
                Search();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"結果: {_results.Count} 件");

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var displayCount = 0;
            foreach (var info in _results)
            {
                if (!string.IsNullOrEmpty(_filter) && !info.AssetPath.ToLower().Contains(_filter.ToLower()))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(_componentFilter))
                {
                    var cf = _componentFilter.ToLower();
                    if (!info.ComponentName.ToLower().Contains(cf) &&
                        !info.PropertyPath.ToLower().Contains(cf) &&
                        !info.GameObjectName.ToLower().Contains(cf))
                    {
                        continue;
                    }
                }

                if (displayCount >= 100)
                {
                    EditorGUILayout.LabelField("... 表示上限 100 件に達しました。フィルターで絞り込んでください。", EditorStyles.miniLabel);
                    break;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(System.IO.Path.GetFileName(info.AssetPath), EditorStyles.linkLabel, GUILayout.ExpandWidth(false)))
                    {
                        if (info.Asset != null)
                        {
                            EditorGUIUtility.PingObject(info.Asset);
                            Selection.activeObject = info.Asset;
                        }
                    }

                    EditorGUILayout.LabelField($"{info.GameObjectName} > {info.ComponentName}.{info.PropertyPath}", EditorStyles.miniLabel);
                }

                displayCount++;
            }

            EditorGUILayout.EndScrollView();
        }

        private void Search()
        {
            _results.Clear();

            var filters = new List<string>();
            if (_searchPrefabs) filters.Add("t:Prefab");
            if (_searchScriptableObjects) filters.Add("t:ScriptableObject");
            if (_searchScenes) filters.Add("t:Scene");
            if (filters.Count == 0) return;

            var guids = AssetDatabase.FindAssets(string.Join(" ", filters));

            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (EditorUtility.DisplayCancelableProgressBar("None参照検索", assetPath, (float)i / guids.Length))
                {
                    break;
                }

                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset == null) continue;

                if (asset is GameObject go)
                {
                    CheckGameObject(go, assetPath);
                }
                else if (asset is ScriptableObject)
                {
                    CheckSerializedObject(asset, assetPath, asset.name, asset.GetType().Name);
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"[None参照検索] 完了: {_results.Count} 件");
        }

        private void CheckGameObject(GameObject root, string assetPath)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;

                var goName = component.gameObject.name;
                var compName = component.GetType().Name;
                var so = new SerializedObject(component);
                var prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != null) continue;

                    // m_Script, m_GameObject 等の Unity 内部フィールドは除外
                    if (IsInternalProperty(prop.propertyPath)) continue;

                    _results.Add(new NoneReferenceInfo
                    {
                        AssetPath = assetPath,
                        GameObjectName = goName,
                        ComponentName = compName,
                        PropertyPath = prop.propertyPath,
                        Asset = root,
                    });
                }
            }
        }

        private void CheckSerializedObject(Object obj, string assetPath, string objName, string typeName)
        {
            var so = new SerializedObject(obj);
            var prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (prop.objectReferenceValue != null) continue;
                if (IsInternalProperty(prop.propertyPath)) continue;

                _results.Add(new NoneReferenceInfo
                {
                    AssetPath = assetPath,
                    GameObjectName = objName,
                    ComponentName = typeName,
                    PropertyPath = prop.propertyPath,
                    Asset = obj,
                });
            }
        }

        private static bool IsInternalProperty(string path)
        {
            return path == "m_Script" ||
                   path == "m_GameObject" ||
                   path == "m_Father" ||
                   path == "m_CorrespondingSourceObject" ||
                   path == "m_PrefabInstance" ||
                   path == "m_PrefabAsset" ||
                   path.StartsWith("m_Children.") ||
                   path.StartsWith("m_Component.");
        }
    }
}
#endif
