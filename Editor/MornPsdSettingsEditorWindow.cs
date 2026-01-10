using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.PSD;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornPsdSettingsEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<string> _psdPaths = new List<string>();
        private bool[] _selectedTextures;
        
        private int _maxSize = 2048;
        private TextureResizeAlgorithm _resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        private TextureImporterFormat _format = TextureImporterFormat.Automatic;
        private TextureImporterCompression _compression = TextureImporterCompression.Compressed;
        private int _compressionQuality = 50;
        private bool _useCrunchCompression;
        private bool _generateMipMaps = true;
        private bool _sRGBTexture = true;
        private FilterMode _filterMode = FilterMode.Bilinear;
        private TextureWrapMode _wrapMode = TextureWrapMode.Repeat;
        private int _anisoLevel = 1;
        
        private SpriteImportMode _spriteImportMode = SpriteImportMode.Multiple;
        private uint _pixelsPerUnit = 100;
        
        private bool _showSettings = true;
        private bool _showPsdSettings = true;
        private bool _showTextureList = true;
        private string _searchFilter = "";
        private string _searchPath = "Assets";
        
        [MenuItem("Tools/MornUtil/PSD Settings Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornPsdSettingsEditorWindow>();
            window.titleContent = new GUIContent("PSD Settings Editor");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshTextureList();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            DrawToolbar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawSettingsSection();
            DrawPsdSettingsSection();
            DrawTextureListSection();
            
            EditorGUILayout.EndScrollView();
            
            DrawActionButtons();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToolbar()
        {
            // シンプルなツールバーに変更
        }
        
        private void DrawSettingsSection()
        {
            _showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showSettings, "Texture Import Settings");
            
            if (_showSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("Size Settings", EditorStyles.boldLabel);
                _maxSize = EditorGUILayout.IntPopup("Max Size", _maxSize, 
                    new[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" },
                    new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 });
                _resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("Resize Algorithm", _resizeAlgorithm);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Format Settings", EditorStyles.boldLabel);
                _format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", _format);
                _compression = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", _compression);
                
                if (_compression == TextureImporterCompression.CompressedHQ || _compression == TextureImporterCompression.CompressedLQ)
                {
                    _compressionQuality = EditorGUILayout.IntSlider("Compression Quality", _compressionQuality, 0, 100);
                }
                
                _useCrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", _useCrunchCompression);
                
                EditorGUILayout.HelpBox("MaxSize and Compression settings will be applied via PlatformTextureSettings using direct API.", MessageType.Info);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Texture Settings", EditorStyles.boldLabel);
                _generateMipMaps = EditorGUILayout.Toggle("Generate Mip Maps", _generateMipMaps);
                _sRGBTexture = EditorGUILayout.Toggle("sRGB (Color Texture)", _sRGBTexture);
                _filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", _filterMode);
                _wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", _wrapMode);
                _anisoLevel = EditorGUILayout.IntSlider("Aniso Level", _anisoLevel, 0, 16);
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawPsdSettingsSection()
        {
            _showPsdSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showPsdSettings, "PSD Specific Settings");
            
            if (_showPsdSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox("PSD-specific sprite settings (Sprite Mode, Pixels Per Unit, Character Mode, etc.) are preserved to avoid modifying existing sprite import data such as names, pivots, and rect dimensions.", MessageType.Info);
                
                EditorGUILayout.LabelField("Sprite Settings (Read-only)", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                _spriteImportMode = (SpriteImportMode)EditorGUILayout.EnumPopup("Sprite Mode (Preserved)", _spriteImportMode);
                _pixelsPerUnit = (uint)EditorGUILayout.IntField("Pixels Per Unit (Preserved)", (int)_pixelsPerUnit);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawTextureListSection()
        {
            _showTextureList = EditorGUILayout.BeginFoldoutHeaderGroup(_showTextureList, $"PSD/PSB List ({GetFilteredPaths().Count}/{_psdPaths.Count})");
            
            if (_showTextureList)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // 検索コントロール
                MornAssetListDrawer.DrawSearchControls(ref _searchPath, ref _searchFilter, RefreshTextureList);
                
                var filteredPaths = GetFilteredPaths();
                
                MornAssetListDrawer.DrawSelectButtons(_selectedTextures, filteredPaths, _psdPaths);
                
                MornAssetListDrawer.DrawAssetListFromPaths(_psdPaths, _selectedTextures, 
                    path => string.IsNullOrEmpty(_searchFilter) || System.IO.Path.GetFileName(path).ToLower().Contains(_searchFilter.ToLower()));
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Apply Settings to Selected", GUILayout.Height(30), GUILayout.Width(200)))
            {
                ApplySettingsToSelected();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void RefreshTextureList()
        {
            _psdPaths.Clear();
            
            // 検索パスが指定されている場合はそのフォルダのみ検索
            string[] searchFolders = null;
            if (!string.IsNullOrEmpty(_searchPath) && _searchPath != "Assets")
            {
                searchFolders = new[] { _searchPath };
            }
            
            var allGuids = AssetDatabase.FindAssets("", searchFolders);
            
            foreach (var guid in allGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                
                if ((path.EndsWith(".psd") || path.EndsWith(".psb")) && 
                    (searchFolders == null || path.StartsWith(_searchPath)))
                {
                    _psdPaths.Add(path);
                }
            }
            
            _selectedTextures = new bool[_psdPaths.Count];
        }
        
        private List<string> GetFilteredPaths()
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                return _psdPaths;
            }
            
            var filter = _searchFilter.ToLower();
            return _psdPaths.Where(path => System.IO.Path.GetFileName(path).ToLower().Contains(filter)).ToList();
        }
        
        private void ApplySettingsToSelected()
        {
            var selectedPaths = new List<string>();
            var selectedImporters = new List<PSDImporter>();
            
            for (int i = 0; i < _psdPaths.Count; i++)
            {
                if (!_selectedTextures[i])
                    continue;
                
                var path = _psdPaths[i];
                
                // PSD/PSBファイルのPSDインポーターを取得
                var psdImporter = AssetImporter.GetAtPath(path) as PSDImporter;
                
                if (psdImporter != null)
                {
                    selectedPaths.Add(path);
                    selectedImporters.Add(psdImporter);
                }
            }
            
            if (selectedImporters.Count == 0)
            {
                Debug.LogWarning("No PSD/PSB files selected for applying settings.");
                return;
            }
            
            EditorUtility.DisplayProgressBar("Applying PSD Settings", "Processing PSD/PSB files...", 0f);
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                for (int i = 0; i < selectedImporters.Count; i++)
                {
                    var psdImporter = selectedImporters[i];
                    var path = selectedPaths[i];
                    var fileName = System.IO.Path.GetFileName(path);
                    
                    EditorUtility.DisplayProgressBar("Applying PSD Settings", 
                        $"Processing {fileName} ({i + 1}/{selectedImporters.Count})", 
                        (float)i / selectedImporters.Count);
                    
                    // デバッグ出力（初回のみ）
                    // if (i == 0) DebugPSDImporterProperties(psdImporter);
                    
                    // PSDImporter固有の設定を適用
                    try
                    {
                        // PSDImporterの基本設定
                        ApplyPSDImporterSettings(psdImporter);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to set properties for {fileName}: {e.Message}");
                    }
                    
                    EditorUtility.SetDirty(psdImporter);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            
            // 個別に再インポートを実行
            EditorUtility.DisplayProgressBar("Applying PSD Settings", "Re-importing assets...", 1f);
            
            for (int i = 0; i < selectedPaths.Count; i++)
            {
                var path = selectedPaths[i];
                var fileName = System.IO.Path.GetFileName(path);
                
                EditorUtility.DisplayProgressBar("Re-importing PSD Settings", 
                    $"Re-importing {fileName} ({i + 1}/{selectedPaths.Count})", 
                    (float)i / selectedPaths.Count);
                
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            
            Debug.Log($"Applied PSD settings to {selectedImporters.Count} files.");
        }
        
        
        private void ApplyPSDImporterSettings(PSDImporter psdImporter)
        {
            // PSDImporterの直接プロパティを設定（singleSpriteImportDataに影響しないもののみ）
            try
            {
                // テクスチャ品質設定のみ適用
                psdImporter.anisoLevel = _anisoLevel;
                psdImporter.filterMode = _filterMode;
                psdImporter.mipmapEnabled = _generateMipMaps;
                psdImporter.wrapMode = _wrapMode;
                
                // spriteImportModeとspritePixelsPerUnitは既存のスプライトデータを変更する可能性があるため除外
                // psdImporter.spriteImportMode = _spriteImportMode;
                // psdImporter.spritePixelsPerUnit = _pixelsPerUnit;
                
                // PSD特有の設定も既存データに影響する可能性があるため除外
                // psdImporter.useCharacterMode = true;
                // psdImporter.useMosaicMode = false;
                
                Debug.Log("Successfully applied texture quality settings only (preserved sprite import data)");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to apply PSD properties: {e.Message}");
            }
            
            // PlatformTextureSettings経由で設定を適用
            ApplyPlatformTextureSettings(psdImporter);
            
            Debug.Log($"Applied PSD texture settings while preserving sprite import data.");
        }
        
        private void ApplyPlatformTextureSettings(PSDImporter psdImporter)
        {
            try
            {
                // Standaloneプラットフォーム設定を取得・適用
                var buildTarget = BuildTarget.NoTarget;
                var platformSettings = psdImporter.GetImporterPlatformSettings(buildTarget);
                if (platformSettings != null)
                {
                    Debug.Log($"BEFORE {buildTarget} - MaxSize: {platformSettings.maxTextureSize}, Format: {platformSettings.format}, Compression: {platformSettings.textureCompression}");
                    
                    // 設定値を適用
                    var targetMaxSize = _maxSize;
                    if (_generateMipMaps && !IsPowerOfTwo(_maxSize))
                    {
                        targetMaxSize = GetNearestPowerOfTwo(_maxSize);
                        Debug.LogWarning($"MipMaps enabled, adjusting MaxSize from {_maxSize} to nearest POT: {targetMaxSize}");
                    }
                    
                    platformSettings.maxTextureSize = targetMaxSize;
                    platformSettings.textureCompression = _compression;
                    platformSettings.compressionQuality = _compressionQuality;
                    platformSettings.crunchedCompression = _useCrunchCompression;
                    platformSettings.resizeAlgorithm = _resizeAlgorithm;
                    
                    if (_format != TextureImporterFormat.Automatic)
                    {
                        platformSettings.format = _format;
                    }
                    
                    // 設定を適用
                    psdImporter.SetImporterPlatformSettings(platformSettings);
                    
                    // 確認
                    var updatedSettings = psdImporter.GetImporterPlatformSettings(buildTarget);
                    Debug.Log($"AFTER {buildTarget} - MaxSize: {updatedSettings.maxTextureSize}, Format: {updatedSettings.format}, Compression: {updatedSettings.textureCompression}");
                    
                    if (updatedSettings.maxTextureSize == targetMaxSize)
                    {
                        Debug.Log($"✓ {buildTarget} platform settings successfully applied!");
                    }
                    else
                    {
                        Debug.LogWarning($"✗ {buildTarget} platform settings may not have been applied correctly. Expected: {targetMaxSize}, Got: {updatedSettings.maxTextureSize}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not get {buildTarget} platform settings");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Platform texture settings failed: {e.Message}");
            }
        }
        
        
        private bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
        
        private int GetNearestPowerOfTwo(int value)
        {
            var potValues = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
            return potValues.OrderBy(x => System.Math.Abs(x - value)).First();
        }
        
        
    }
}