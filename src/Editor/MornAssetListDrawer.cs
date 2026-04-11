using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal static class MornAssetListDrawer
    {
        public static void DrawAssetList<T>(
            List<T> assets,
            bool[] selectedAssets,
            System.Func<T, bool> filterPredicate = null) where T : Object
        {
            var filteredAssets = filterPredicate != null ? 
                assets.FindAll(asset => filterPredicate(asset)) : 
                assets;

            for (int i = 0; i < filteredAssets.Count; i++)
            {
                var asset = filteredAssets[i];
                var index = assets.IndexOf(asset);

                EditorGUILayout.BeginHorizontal();

                // チェックボックス専用のスペースを確保
                var toggleValue = GUILayout.Toggle(selectedAssets[index], "", GUILayout.Width(20));
                selectedAssets[index] = toggleValue;

                // ObjectFieldで表示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(asset, typeof(T), false);
                EditorGUI.EndDisabledGroup();

                // Selectボタン
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        public static void DrawAssetListFromPaths(
            List<string> assetPaths,
            bool[] selectedAssets,
            System.Func<string, bool> filterPredicate = null)
        {
            var filteredPaths = filterPredicate != null ? 
                assetPaths.FindAll(path => filterPredicate(path)) : 
                assetPaths;

            for (int i = 0; i < filteredPaths.Count; i++)
            {
                var path = filteredPaths[i];
                var index = assetPaths.IndexOf(path);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);

                EditorGUILayout.BeginHorizontal();

                // チェックボックス専用のスペースを確保
                var toggleValue = GUILayout.Toggle(selectedAssets[index], "", GUILayout.Width(20));
                selectedAssets[index] = toggleValue;

                // ObjectFieldで表示
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
                EditorGUI.EndDisabledGroup();

                // Selectボタン
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        public static void DrawSearchControls(ref string searchPath, ref string searchFilter, System.Action onRefresh)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 検索パス
            EditorGUILayout.LabelField("Path:", GUILayout.Width(35));
            searchPath = EditorGUILayout.TextField(searchPath, GUILayout.Width(200));
            
            if (GUILayout.Button("...", GUILayout.Width(25)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("Select Search Path", searchPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 絶対パスを相対パスに変換
                    var dataPath = Application.dataPath;
                    if (selectedPath.StartsWith(dataPath))
                    {
                        searchPath = "Assets" + selectedPath.Substring(dataPath.Length);
                    }
                    else
                    {
                        searchPath = selectedPath;
                    }
                    onRefresh?.Invoke();
                }
            }
            
            // 検索フィルター
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                onRefresh?.Invoke();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        public static void DrawSelectButtons(bool[] selectedAssets, List<string> filteredItems, List<string> allItems)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All", GUILayout.Width(80)))
            {
                foreach (var item in filteredItems)
                {
                    var index = allItems.IndexOf(item);
                    if (index >= 0)
                        selectedAssets[index] = true;
                }
            }

            if (GUILayout.Button("Deselect All", GUILayout.Width(80)))
            {
                for (int i = 0; i < selectedAssets.Length; i++)
                {
                    selectedAssets[i] = false;
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Selected: {System.Array.FindAll(selectedAssets, x => x).Length}", GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
    }
}