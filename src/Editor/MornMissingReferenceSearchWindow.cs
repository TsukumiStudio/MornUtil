using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornMissingReferenceSearchWindow : EditorWindow
    {
        private sealed class MissingReferenceInfo
        {
            public string AssetPath { get; set; }
            public string ComponentName { get; set; }
            public string PropertyPath { get; set; }
            public Object Asset { get; set; }
        }

        private List<MissingReferenceInfo> _missingReferences = new List<MissingReferenceInfo>();
        private Vector2 _scrollPosition;
        private bool _isSearching;
        private string _searchFilter = "";
        private bool _searchPrefabsOnly = true;
        private bool _searchScriptableObjects = true;
        private bool _searchScenes = false;

        [MenuItem("Tools/MornUtil/Missing参照検索")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornMissingReferenceSearchWindow>("Missing参照検索");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("検索対象:", GUILayout.Width(60));
                _searchPrefabsOnly = EditorGUILayout.ToggleLeft("Prefab", _searchPrefabsOnly, GUILayout.Width(80));
                _searchScriptableObjects = EditorGUILayout.ToggleLeft("ScriptableObject", _searchScriptableObjects, GUILayout.Width(120));
                _searchScenes = EditorGUILayout.ToggleLeft("Scene", _searchScenes, GUILayout.Width(80));
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                _searchFilter = EditorGUILayout.TextField("フィルター:", _searchFilter);
                
                if (GUILayout.Button("クリア", GUILayout.Width(60)))
                {
                    _searchFilter = "";
                }
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledScope(_isSearching))
            {
                if (GUILayout.Button("Missing参照を検索", GUILayout.Height(30)))
                {
                    SearchMissingReferences();
                }
            }
            
            if (_isSearching)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("検索中...", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            
            if (_missingReferences.Count > 0)
            {
                EditorGUILayout.LabelField($"Missing参照が見つかりました: {_missingReferences.Count} 件");
                EditorGUILayout.Space();
                
                using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scrollView.scrollPosition;
                    
                    foreach (var info in _missingReferences)
                    {
                        if (!string.IsNullOrEmpty(_searchFilter) && 
                            !info.AssetPath.ToLower().Contains(_searchFilter.ToLower()))
                        {
                            continue;
                        }
                        
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.ObjectField(info.Asset, typeof(Object), false);
                                
                                if (GUILayout.Button("選択", GUILayout.Width(50)))
                                {
                                    Selection.activeObject = info.Asset;
                                    EditorGUIUtility.PingObject(info.Asset);
                                }
                            }
                            
                            EditorGUILayout.LabelField("パス:", info.AssetPath);
                            EditorGUILayout.LabelField("コンポーネント:", info.ComponentName);
                            EditorGUILayout.LabelField("プロパティ:", info.PropertyPath);
                        }
                        
                        EditorGUILayout.Space();
                    }
                }
            }
            else if (!_isSearching)
            {
                EditorGUILayout.HelpBox("Missing参照は見つかりませんでした。", MessageType.Info);
            }
        }

        private void SearchMissingReferences()
        {
            _isSearching = true;
            _missingReferences.Clear();
            
            try
            {
                var searchFilter = BuildSearchFilter();
                var guids = AssetDatabase.FindAssets(searchFilter);
                var totalCount = guids.Length;
                
                for (var i = 0; i < totalCount; i++)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Missing参照を検索中", 
                        $"検索中... ({i + 1}/{totalCount})\n{assetPath}", 
                        (float)i / totalCount))
                    {
                        break;
                    }
                    
                    CheckAssetForMissingReferences(assetPath);
                }
            }
            finally
            {
                _isSearching = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private string BuildSearchFilter()
        {
            var filters = new List<string>();
            
            if (_searchPrefabsOnly)
                filters.Add("t:Prefab");
            
            if (_searchScriptableObjects)
                filters.Add("t:ScriptableObject");
                
            if (_searchScenes)
                filters.Add("t:Scene");
            
            return string.Join(" ", filters);
        }

        private void CheckAssetForMissingReferences(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null) return;
            
            var dependencies = EditorUtility.CollectDependencies(new[] { asset });
            
            if (dependencies.Any(d => d == null))
            {
                // Prefabの場合
                if (asset is GameObject gameObject)
                {
                    CheckGameObjectForMissingReferences(gameObject, assetPath);
                }
                // ScriptableObjectの場合
                else if (asset is ScriptableObject)
                {
                    CheckScriptableObjectForMissingReferences(asset, assetPath);
                }
            }
        }

        private void CheckGameObjectForMissingReferences(GameObject gameObject, string assetPath)
        {
            var components = gameObject.GetComponentsInChildren<Component>(true);
            
            foreach (var component in components)
            {
                if (component == null)
                {
                    _missingReferences.Add(new MissingReferenceInfo
                    {
                        AssetPath = assetPath,
                        ComponentName = "Missing Script",
                        PropertyPath = gameObject.name,
                        Asset = gameObject
                    });
                    continue;
                }
                
                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();
                
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (property.objectReferenceValue == null && 
                            property.objectReferenceInstanceIDValue != 0)
                        {
                            _missingReferences.Add(new MissingReferenceInfo
                            {
                                AssetPath = assetPath,
                                ComponentName = component.GetType().Name,
                                PropertyPath = property.propertyPath,
                                Asset = gameObject
                            });
                        }
                    }
                }
            }
        }

        private void CheckScriptableObjectForMissingReferences(Object scriptableObject, string assetPath)
        {
            var serializedObject = new SerializedObject(scriptableObject);
            var property = serializedObject.GetIterator();
            
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (property.objectReferenceValue == null && 
                        property.objectReferenceInstanceIDValue != 0)
                    {
                        _missingReferences.Add(new MissingReferenceInfo
                        {
                            AssetPath = assetPath,
                            ComponentName = scriptableObject.GetType().Name,
                            PropertyPath = property.propertyPath,
                            Asset = scriptableObject
                        });
                    }
                }
            }
        }
    }
}