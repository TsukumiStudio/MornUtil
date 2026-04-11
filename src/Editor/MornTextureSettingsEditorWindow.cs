using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornTextureSettingsEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<Texture2D> _textures = new List<Texture2D>();
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
        
        private bool _showSettings = true;
        private bool _showTextureList = true;
        private string _searchFilter = "";
        private string _searchPath = "Assets";
        
        [MenuItem("Tools/MornUtil/Texture Settings Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornTextureSettingsEditorWindow>();
            window.titleContent = new GUIContent("Texture Settings Editor");
            window.minSize = new Vector2(600, 400);
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
        
        private void DrawTextureListSection()
        {
            _showTextureList = EditorGUILayout.BeginFoldoutHeaderGroup(_showTextureList, $"Texture List ({GetFilteredTextures().Count}/{_textures.Count})");
            
            if (_showTextureList)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // 検索コントロール
                MornAssetListDrawer.DrawSearchControls(ref _searchPath, ref _searchFilter, RefreshTextureList);
                
                var filteredTextures = GetFilteredTextures();
                var filteredTextureNames = filteredTextures.ConvertAll(t => t.name);
                var allTextureNames = _textures.ConvertAll(t => t.name);
                
                MornAssetListDrawer.DrawSelectButtons(_selectedTextures, filteredTextureNames, allTextureNames);
                
                MornAssetListDrawer.DrawAssetList(_textures, _selectedTextures, 
                    texture => string.IsNullOrEmpty(_searchFilter) || texture.name.ToLower().Contains(_searchFilter.ToLower()));
                
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
            _textures.Clear();
            
            // 検索パスが指定されている場合はそのフォルダのみ検索
            string[] searchFolders = null;
            if (!string.IsNullOrEmpty(_searchPath) && _searchPath != "Assets")
            {
                searchFolders = new[] { _searchPath };
            }
            
            var guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                
                if (texture != null && 
                    (searchFolders == null || path.StartsWith(_searchPath)))
                {
                    _textures.Add(texture);
                }
            }
            
            _selectedTextures = new bool[_textures.Count];
        }
        
        private List<Texture2D> GetFilteredTextures()
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                return _textures;
            }
            
            var filter = _searchFilter.ToLower();
            return _textures.Where(t => t.name.ToLower().Contains(filter)).ToList();
        }
        
        private void ApplySettingsToSelected()
        {
            var selectedTextures = new List<Texture2D>();
            var selectedImporters = new List<TextureImporter>();
            var selectedPaths = new List<string>();
            
            for (int i = 0; i < _textures.Count; i++)
            {
                if (!_selectedTextures[i])
                    continue;
                
                var texture = _textures[i];
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null)
                {
                    selectedTextures.Add(texture);
                    selectedImporters.Add(importer);
                    selectedPaths.Add(path);
                }
            }
            
            if (selectedImporters.Count == 0)
            {
                Debug.LogWarning("No textures selected for applying settings.");
                return;
            }
            
            EditorUtility.DisplayProgressBar("Applying Texture Settings", "Processing textures...", 0f);
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                for (int i = 0; i < selectedImporters.Count; i++)
                {
                    var importer = selectedImporters[i];
                    var texture = selectedTextures[i];
                    
                    EditorUtility.DisplayProgressBar("Applying Texture Settings", 
                        $"Processing {texture.name} ({i + 1}/{selectedImporters.Count})", 
                        (float)i / selectedImporters.Count);
                    
                    importer.maxTextureSize = _maxSize;
                    importer.textureCompression = _compression;
                    importer.compressionQuality = _compressionQuality;
                    importer.crunchedCompression = _useCrunchCompression;
                    importer.mipmapEnabled = _generateMipMaps;
                    importer.sRGBTexture = _sRGBTexture;
                    importer.filterMode = _filterMode;
                    importer.wrapMode = _wrapMode;
                    importer.anisoLevel = _anisoLevel;
                    
                    if (_format != TextureImporterFormat.Automatic)
                    {
                        var platformSettings = importer.GetDefaultPlatformTextureSettings();
                        platformSettings.format = _format;
                        platformSettings.textureCompression = _compression;
                        platformSettings.resizeAlgorithm = _resizeAlgorithm;
                        importer.SetPlatformTextureSettings(platformSettings);
                    }
                    
                    EditorUtility.SetDirty(importer);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            
            // まとめて再インポートを実行
            EditorUtility.DisplayProgressBar("Applying Texture Settings", "Re-importing assets...", 1f);
            
            // バッチでインポート処理を実行
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in selectedPaths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            
            Debug.Log($"Applied texture settings to {selectedImporters.Count} textures.");
        }
    }
}