using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornTextureMultipleCheckWindow : EditorWindow
    {
        private sealed class TextureInfo
        {
            public string AssetPath { get; set; }
            public Texture2D Texture { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsWidthMultipleOf4 { get; set; }
            public bool IsHeightMultipleOf4 { get; set; }
            public int RecommendedWidth { get; set; }
            public int RecommendedHeight { get; set; }
            public TextureImporterFormat Format { get; set; }
            public bool IsPowerOfTwo { get; set; }
            public long FileSize { get; set; }
        }

        private List<TextureInfo> _problematicTextures = new List<TextureInfo>();
        private Vector2 _scrollPosition;
        private bool _isSearching;
        private string _searchFilter = "";
        
        // フィルター設定
        private bool _showOnlyNonMultiple = true;
        private bool _showNonPowerOfTwo = true;
        private bool _includeReadOnlyTextures = false;
        private int _multipleValue = 4;
        
        // 除外パス
        private readonly string[] _excludePaths = new[]
        {
            "Assets/Plugins",
            "Assets/TextMesh Pro",
            "Packages/",
            "Assets/StreamingAssets"
        };
        
        // プレビュー用
        private Texture2D _selectedTexture;
        private float _previewSize = 128f;

        [MenuItem("Tools/MornUtil/テクスチャサイズチェック")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornTextureMultipleCheckWindow>("テクスチャサイズチェック");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("チェック設定", EditorStyles.boldLabel);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("倍数チェック値:", GUILayout.Width(100));
                    _multipleValue = EditorGUILayout.IntField(_multipleValue, GUILayout.Width(50));
                    
                    if (_multipleValue < 1) _multipleValue = 1;
                    
                    EditorGUILayout.LabelField($"の倍数でないテクスチャを検出", GUILayout.Width(200));
                }
                
                EditorGUILayout.Space(5);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    _showOnlyNonMultiple = EditorGUILayout.ToggleLeft($"{_multipleValue}の倍数でないもののみ表示", _showOnlyNonMultiple, GUILayout.Width(200));
                    _showNonPowerOfTwo = EditorGUILayout.ToggleLeft("2のべき乗でないものを含む", _showNonPowerOfTwo, GUILayout.Width(200));
                    _includeReadOnlyTextures = EditorGUILayout.ToggleLeft("読み取り専用テクスチャを含む", _includeReadOnlyTextures, GUILayout.Width(200));
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
                
                EditorGUILayout.LabelField("プレビューサイズ:", GUILayout.Width(100));
                _previewSize = EditorGUILayout.Slider(_previewSize, 64f, 256f, GUILayout.Width(200));
            }
            
            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledScope(_isSearching))
            {
                if (GUILayout.Button("テクスチャをチェック", GUILayout.Height(30)))
                {
                    CheckTextures();
                }
            }
            
            if (_isSearching)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("チェック中...", MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            
            if (_problematicTextures.Count > 0)
            {
                var displayedTextures = GetFilteredTextures();
                EditorGUILayout.LabelField($"問題のあるテクスチャ: {displayedTextures.Count} / {_problematicTextures.Count} 件");
                
                EditorGUILayout.Space();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("CSVエクスポート", GUILayout.Height(25)))
                    {
                        ExportToCSV();
                    }
                    
                    if (GUILayout.Button("選択したテクスチャの設定を開く", GUILayout.Height(25)))
                    {
                        OpenTextureImportSettings();
                    }
                }
                
                EditorGUILayout.Space();
                
                using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
                {
                    _scrollPosition = scrollView.scrollPosition;
                    
                    foreach (var info in displayedTextures)
                    {
                        DrawTextureInfo(info);
                    }
                }
            }
            else if (!_isSearching)
            {
                EditorGUILayout.HelpBox($"{_multipleValue}の倍数でないテクスチャは見つかりませんでした。", MessageType.Info);
            }
        }

        private void DrawTextureInfo(TextureInfo info)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // テクスチャプレビュー
                    var rect = GUILayoutUtility.GetRect(_previewSize, _previewSize, GUILayout.Width(_previewSize), GUILayout.Height(_previewSize));
                    if (info.Texture != null)
                    {
                        GUI.DrawTexture(rect, info.Texture, ScaleMode.ScaleToFit);
                    }
                    
                    using (new EditorGUILayout.VerticalScope())
                    {
                        // 基本情報
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(info.Texture, typeof(Texture2D), false);
                            
                            if (GUILayout.Button("選択", GUILayout.Width(50)))
                            {
                                Selection.activeObject = info.Texture;
                                EditorGUIUtility.PingObject(info.Texture);
                                _selectedTexture = info.Texture;
                            }
                        }
                        
                        EditorGUILayout.LabelField("パス:", info.AssetPath);
                        
                        // サイズ情報
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var widthStyle = info.IsWidthMultipleOf4 ? EditorStyles.label : new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
                            var heightStyle = info.IsHeightMultipleOf4 ? EditorStyles.label : new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
                            
                            EditorGUILayout.LabelField("サイズ:", GUILayout.Width(50));
                            EditorGUILayout.LabelField($"{info.Width}", widthStyle, GUILayout.Width(60));
                            EditorGUILayout.LabelField("x", GUILayout.Width(15));
                            EditorGUILayout.LabelField($"{info.Height}", heightStyle, GUILayout.Width(60));
                            
                            if (!info.IsWidthMultipleOf4 || !info.IsHeightMultipleOf4)
                            {
                                EditorGUILayout.LabelField($"→ 推奨: {info.RecommendedWidth} x {info.RecommendedHeight}", 
                                    new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } });
                            }
                        }
                        
                        // 追加情報
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"フォーマット: {info.Format}", GUILayout.Width(200));
                            EditorGUILayout.LabelField($"ファイルサイズ: {FormatFileSize(info.FileSize)}", GUILayout.Width(150));
                            
                            if (!info.IsPowerOfTwo)
                            {
                                EditorGUILayout.LabelField("[非2のべき乗]", 
                                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } }, 
                                    GUILayout.Width(100));
                            }
                        }
                        
                        // 問題の詳細
                        var problems = new List<string>();
                        if (!info.IsWidthMultipleOf4) problems.Add($"幅が{_multipleValue}の倍数でない");
                        if (!info.IsHeightMultipleOf4) problems.Add($"高さが{_multipleValue}の倍数でない");
                        if (!info.IsPowerOfTwo && _showNonPowerOfTwo) problems.Add("2のべき乗でない");
                        
                        if (problems.Count > 0)
                        {
                            EditorGUILayout.HelpBox(string.Join(", ", problems), MessageType.Warning);
                        }
                    }
                }
            }
            
            EditorGUILayout.Space(5);
        }

        private void CheckTextures()
        {
            _isSearching = true;
            _problematicTextures.Clear();
            
            try
            {
                var textureGuids = AssetDatabase.FindAssets("t:Texture2D");
                var totalCount = textureGuids.Length;
                
                for (var i = 0; i < totalCount; i++)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                    
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "テクスチャをチェック中", 
                        $"チェック中... ({i + 1}/{totalCount})\n{assetPath}", 
                        (float)i / totalCount))
                    {
                        break;
                    }
                    
                    if (IsExcludedPath(assetPath)) continue;
                    
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (texture == null) continue;
                    
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;
                    
                    // 読み取り専用チェック
                    if (!_includeReadOnlyTextures && !importer.isReadable) continue;
                    
                    var width = texture.width;
                    var height = texture.height;
                    var isWidthMultiple = (width % _multipleValue) == 0;
                    var isHeightMultiple = (height % _multipleValue) == 0;
                    var isPowerOfTwo = IsPowerOfTwo(width) && IsPowerOfTwo(height);
                    
                    // フィルタリング
                    if (_showOnlyNonMultiple && isWidthMultiple && isHeightMultiple) continue;
                    if (!_showNonPowerOfTwo && !isPowerOfTwo) continue;
                    
                    var fileInfo = new System.IO.FileInfo(assetPath);
                    
                    _problematicTextures.Add(new TextureInfo
                    {
                        AssetPath = assetPath,
                        Texture = texture,
                        Width = width,
                        Height = height,
                        IsWidthMultipleOf4 = isWidthMultiple,
                        IsHeightMultipleOf4 = isHeightMultiple,
                        RecommendedWidth = GetNextMultiple(width, _multipleValue),
                        RecommendedHeight = GetNextMultiple(height, _multipleValue),
                        Format = importer.textureFormat,
                        IsPowerOfTwo = isPowerOfTwo,
                        FileSize = fileInfo.Exists ? fileInfo.Length : 0
                    });
                }
            }
            finally
            {
                _isSearching = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private List<TextureInfo> GetFilteredTextures()
        {
            return _problematicTextures
                .Where(t => string.IsNullOrEmpty(_searchFilter) || 
                           t.AssetPath.ToLower().Contains(_searchFilter.ToLower()))
                .OrderByDescending(t => t.FileSize)
                .ToList();
        }

        private bool IsExcludedPath(string assetPath)
        {
            return _excludePaths.Any(excludePath => assetPath.StartsWith(excludePath));
        }

        private bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        private int GetNextMultiple(int value, int multiple)
        {
            if (value % multiple == 0) return value;
            return ((value / multiple) + 1) * multiple;
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

        private void OpenTextureImportSettings()
        {
            if (Selection.activeObject is Texture2D)
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.TextureImporterInspector"));
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "テクスチャを選択してください。", "OK");
            }
        }

        private void ExportToCSV()
        {
            var path = EditorUtility.SaveFilePanel(
                "CSVエクスポート", 
                "", 
                $"TextureSizeCheck_{DateTime.Now:yyyyMMdd_HHmmss}.csv", 
                "csv");
            
            if (string.IsNullOrEmpty(path)) return;
            
            using (var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine($"AssetPath,Width,Height,IsWidthMultipleOf{_multipleValue},IsHeightMultipleOf{_multipleValue},RecommendedWidth,RecommendedHeight,IsPowerOfTwo,Format,FileSize(Bytes)");
                
                foreach (var texture in _problematicTextures.OrderBy(t => t.AssetPath))
                {
                    writer.WriteLine($"{texture.AssetPath},{texture.Width},{texture.Height},{texture.IsWidthMultipleOf4},{texture.IsHeightMultipleOf4},{texture.RecommendedWidth},{texture.RecommendedHeight},{texture.IsPowerOfTwo},{texture.Format},{texture.FileSize}");
                }
            }
            
            EditorUtility.DisplayDialog("エクスポート完了", $"CSVファイルを保存しました:\n{path}", "OK");
        }
    }
}