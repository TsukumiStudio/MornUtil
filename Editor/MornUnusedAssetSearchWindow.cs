using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MornLib
{
    internal sealed class MornUnusedAssetSearchWindow : EditorWindow
    {
        private sealed class UnusedAssetInfo
        {
            public string AssetPath { get; set; }
            public string AssetType { get; set; }
            public long FileSize { get; set; }
            public Object Asset { get; set; }
            public bool IsAddressable { get; set; }
        }

        private List<UnusedAssetInfo> _unusedAssets = new List<UnusedAssetInfo>();
        private Vector2 _scrollPosition;
        private bool _isSearching;
        private string _searchFilter = "";
        
        // 検索対象フィルター
        private bool _searchTextures = true;
        private bool _searchMaterials = true;
        private bool _searchPrefabs = true;
        private bool _searchScriptableObjects = true;
        private bool _searchAudioClips = true;
        private bool _searchAnimations = true;
        private bool _searchShaders = false;
        private bool _includeAddressables = false;
        
        // 除外パス
        private readonly string[] _excludePaths = new[]
        {
            "Assets/Plugins",
            "Assets/TextMesh Pro",
            "Assets/AddressableAssetsData",
            "Packages/",
            "Assets/StreamingAssets"
        };

        [MenuItem("Tools/MornUtil/未使用アセット検索")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornUnusedAssetSearchWindow>("未使用アセット検索");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("検索対象アセットタイプ", EditorStyles.boldLabel);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    _searchTextures = EditorGUILayout.ToggleLeft("Texture", _searchTextures, GUILayout.Width(80));
                    _searchMaterials = EditorGUILayout.ToggleLeft("Material", _searchMaterials, GUILayout.Width(80));
                    _searchPrefabs = EditorGUILayout.ToggleLeft("Prefab", _searchPrefabs, GUILayout.Width(80));
                    _searchScriptableObjects = EditorGUILayout.ToggleLeft("ScriptableObject", _searchScriptableObjects, GUILayout.Width(120));
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    _searchAudioClips = EditorGUILayout.ToggleLeft("AudioClip", _searchAudioClips, GUILayout.Width(80));
                    _searchAnimations = EditorGUILayout.ToggleLeft("Animation", _searchAnimations, GUILayout.Width(80));
                    _searchShaders = EditorGUILayout.ToggleLeft("Shader", _searchShaders, GUILayout.Width(80));
                    _includeAddressables = EditorGUILayout.ToggleLeft("Addressable含む", _includeAddressables, GUILayout.Width(120));
                }
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
                if (GUILayout.Button("未使用アセットを検索", GUILayout.Height(30)))
                {
                    SearchUnusedAssets();
                }
            }
            
            if (_isSearching)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("検索中... (大規模プロジェクトでは時間がかかる場合があります)", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            
            if (_unusedAssets.Count > 0)
            {
                var totalSize = _unusedAssets.Sum(a => a.FileSize);
                EditorGUILayout.LabelField($"未使用アセット: {_unusedAssets.Count} 件 (合計サイズ: {FormatFileSize(totalSize)})");
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("選択中のアセットを削除", GUILayout.Height(25)))
                    {
                        DeleteSelectedAssets();
                    }
                    
                    if (GUILayout.Button("CSVエクスポート", GUILayout.Height(25)))
                    {
                        ExportToCSV();
                    }
                }
                
                EditorGUILayout.Space();
                
                using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scrollView.scrollPosition;
                    
                    var filteredAssets = _unusedAssets
                        .Where(a => string.IsNullOrEmpty(_searchFilter) || 
                                   a.AssetPath.ToLower().Contains(_searchFilter.ToLower()))
                        .OrderByDescending(a => a.FileSize)
                        .ToList();
                    
                    foreach (var info in filteredAssets)
                    {
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.ObjectField(info.Asset, typeof(Object), false);
                                
                                if (info.IsAddressable)
                                {
                                    GUILayout.Label("[Addressable]", GUILayout.Width(90));
                                }
                                
                                GUILayout.Label($"[{info.AssetType}]", GUILayout.Width(100));
                                GUILayout.Label(FormatFileSize(info.FileSize), GUILayout.Width(80));
                                
                                if (GUILayout.Button("選択", GUILayout.Width(50)))
                                {
                                    Selection.activeObject = info.Asset;
                                    EditorGUIUtility.PingObject(info.Asset);
                                }
                            }
                            
                            EditorGUILayout.LabelField("パス:", info.AssetPath);
                        }
                    }
                }
            }
            else if (!_isSearching)
            {
                EditorGUILayout.HelpBox("未使用アセットは見つかりませんでした。", MessageType.Info);
            }
        }

        private void SearchUnusedAssets()
        {
            _isSearching = true;
            _unusedAssets.Clear();
            
            try
            {
                // すべてのアセットのGUIDを取得
                var allAssetGuids = AssetDatabase.FindAssets(BuildSearchFilter());
                var totalCount = allAssetGuids.Length;
                
                // 使用されているアセットのGUIDセットを作成
                var usedAssetGuids = new HashSet<string>();
                
                // プログレスバー表示
                EditorUtility.DisplayProgressBar("未使用アセット検索", "依存関係を解析中...", 0);
                
                // すべてのアセットの依存関係を調べる
                for (var i = 0; i < totalCount; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "未使用アセット検索", 
                        $"依存関係を解析中... ({i + 1}/{totalCount})", 
                        (float)i / totalCount))
                    {
                        break;
                    }
                    
                    var assetPath = AssetDatabase.GUIDToAssetPath(allAssetGuids[i]);
                    
                    // 除外パスをチェック
                    if (IsExcludedPath(assetPath)) continue;
                    
                    // シーンやプレハブなど、他のアセットを参照するアセットから依存関係を取得
                    if (IsRootAsset(assetPath))
                    {
                        var dependencies = AssetDatabase.GetDependencies(assetPath, true);
                        foreach (var depPath in dependencies)
                        {
                            if (depPath != assetPath)
                            {
                                var depGuid = AssetDatabase.AssetPathToGUID(depPath);
                                usedAssetGuids.Add(depGuid);
                            }
                        }
                    }
                }
                
                // ビルド設定に含まれるシーンも確認
                foreach (var scene in EditorBuildSettings.scenes)
                {
                    if (scene.enabled)
                    {
                        var dependencies = AssetDatabase.GetDependencies(scene.path, true);
                        foreach (var depPath in dependencies)
                        {
                            var depGuid = AssetDatabase.AssetPathToGUID(depPath);
                            usedAssetGuids.Add(depGuid);
                        }
                    }
                }
                
                // Addressableアセットのチェック
                var addressableGuids = new HashSet<string>();
                if (AddressableAssetSettingsDefaultObject.Settings != null)
                {
                    foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
                    {
                        foreach (var entry in group.entries)
                        {
                            addressableGuids.Add(entry.guid);
                            
                            // Addressableアセットの依存関係も使用済みとして扱う
                            var depPaths = AssetDatabase.GetDependencies(entry.AssetPath, true);
                            foreach (var depPath in depPaths)
                            {
                                var depGuid = AssetDatabase.AssetPathToGUID(depPath);
                                usedAssetGuids.Add(depGuid);
                            }
                        }
                    }
                }
                
                // 未使用アセットを特定
                EditorUtility.DisplayProgressBar("未使用アセット検索", "未使用アセットを特定中...", 0.8f);
                
                for (var i = 0; i < totalCount; i++)
                {
                    var guid = allAssetGuids[i];
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    
                    if (IsExcludedPath(assetPath)) continue;
                    if (IsRootAsset(assetPath)) continue;
                    
                    var isAddressable = addressableGuids.Contains(guid);
                    
                    // Addressableアセットを含めない設定の場合、スキップ
                    if (isAddressable && !_includeAddressables) continue;
                    
                    if (!usedAssetGuids.Contains(guid))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                        if (asset != null)
                        {
                            var fileInfo = new System.IO.FileInfo(assetPath);
                            
                            _unusedAssets.Add(new UnusedAssetInfo
                            {
                                AssetPath = assetPath,
                                AssetType = asset.GetType().Name,
                                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                                Asset = asset,
                                IsAddressable = isAddressable
                            });
                        }
                    }
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
            
            if (_searchTextures) filters.Add("t:Texture");
            if (_searchMaterials) filters.Add("t:Material");
            if (_searchPrefabs) filters.Add("t:Prefab");
            if (_searchScriptableObjects) filters.Add("t:ScriptableObject");
            if (_searchAudioClips) filters.Add("t:AudioClip");
            if (_searchAnimations) filters.Add("t:AnimationClip");
            if (_searchShaders) filters.Add("t:Shader");
            
            return string.Join(" ", filters);
        }

        private bool IsExcludedPath(string assetPath)
        {
            return _excludePaths.Any(excludePath => assetPath.StartsWith(excludePath));
        }

        private bool IsRootAsset(string assetPath)
        {
            // シーン、プレハブ、ScriptableObjectなど、他のアセットを参照するアセット
            return assetPath.EndsWith(".unity") || 
                   assetPath.EndsWith(".prefab") || 
                   assetPath.EndsWith(".asset") ||
                   assetPath.EndsWith(".controller") ||
                   assetPath.EndsWith(".overrideController") ||
                   assetPath.EndsWith(".mat");
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }

        private void DeleteSelectedAssets()
        {
            var selectedAssets = Selection.objects
                .Select(obj => AssetDatabase.GetAssetPath(obj))
                .Where(path => _unusedAssets.Any(a => a.AssetPath == path))
                .ToList();
            
            if (selectedAssets.Count == 0)
            {
                EditorUtility.DisplayDialog("削除", "削除するアセットを選択してください。", "OK");
                return;
            }
            
            if (EditorUtility.DisplayDialog(
                "アセット削除の確認", 
                $"{selectedAssets.Count}個のアセットを削除しますか？\nこの操作は元に戻せません。", 
                "削除", "キャンセル"))
            {
                foreach (var assetPath in selectedAssets)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                
                AssetDatabase.Refresh();
                SearchUnusedAssets(); // 再検索
            }
        }

        private void ExportToCSV()
        {
            var path = EditorUtility.SaveFilePanel(
                "CSVエクスポート", 
                "", 
                $"UnusedAssets_{DateTime.Now:yyyyMMdd_HHmmss}.csv", 
                "csv");
            
            if (string.IsNullOrEmpty(path)) return;
            
            using (var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("AssetPath,Type,FileSize(Bytes),IsAddressable");
                
                foreach (var asset in _unusedAssets.OrderBy(a => a.AssetPath))
                {
                    writer.WriteLine($"{asset.AssetPath},{asset.AssetType},{asset.FileSize},{asset.IsAddressable}");
                }
            }
            
            EditorUtility.DisplayDialog("エクスポート完了", $"CSVファイルを保存しました:\n{path}", "OK");
        }
    }
}