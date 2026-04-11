using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornTMPFontCacheCleanerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<TMP_FontAsset> _fontAssets = new List<TMP_FontAsset>();
        private Dictionary<TMP_FontAsset, FontAssetInfo> _fontInfos = new Dictionary<TMP_FontAsset, FontAssetInfo>();
        private bool[] _selectedFonts;
        private bool _selectAll;
        private bool _showOnlyDynamic = true;
        private string _searchFilter = "";
        
        private class FontAssetInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public AtlasPopulationMode PopulationMode { get; set; }
            public int CharacterCount { get; set; }
            public int GlyphCount { get; set; }
            public long MemorySize { get; set; }
            public bool HasTypelessData { get; set; }
            public string AtlasSize { get; set; }
        }
        
        [MenuItem("Tools/MornUtil/TMP Font Cache Cleaner")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornTMPFontCacheCleanerWindow>();
            window.titleContent = new GUIContent("TMP Font Cache Cleaner");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshFontList();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            DrawToolbar();
            DrawFontList();
            DrawBottomButtons();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshFontList();
            }
            
            EditorGUILayout.Space();
            
            _showOnlyDynamic = GUILayout.Toggle(_showOnlyDynamic, "Dynamic Fonts Only", EditorStyles.toolbarButton, GUILayout.Width(120));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            
            GUILayout.FlexibleSpace();
            
            var totalMemory = _fontInfos.Values.Sum(info => info.MemorySize);
            var selectedMemory = GetSelectedFontsMemorySize();
            
            EditorGUILayout.LabelField($"Total: {FormatBytes(totalMemory)}", EditorStyles.toolbarButton);
            if (selectedMemory > 0)
            {
                EditorGUILayout.LabelField($"Selected: {FormatBytes(selectedMemory)}", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFontList()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginChangeCheck();
            _selectAll = EditorGUILayout.Toggle(_selectAll, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < _selectedFonts.Length; i++)
                {
                    if (IsVisibleFont(i))
                    {
                        _selectedFonts[i] = _selectAll;
                    }
                }
            }
            
            EditorGUILayout.LabelField("Font Name", EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Atlas Size", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Glyphs", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Memory", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Font list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            for (int i = 0; i < _fontAssets.Count; i++)
            {
                if (!IsVisibleFont(i))
                    continue;
                
                var font = _fontAssets[i];
                var info = _fontInfos[font];
                
                EditorGUILayout.BeginHorizontal();
                
                _selectedFonts[i] = EditorGUILayout.Toggle(_selectedFonts[i], GUILayout.Width(20));
                
                // Font name (clickable)
                if (GUILayout.Button(info.Name, EditorStyles.label, GUILayout.Width(180)))
                {
                    Selection.activeObject = font;
                    EditorGUIUtility.PingObject(font);
                }
                
                // Population mode
                var modeStyle = new GUIStyle(EditorStyles.label);
                if (info.PopulationMode == AtlasPopulationMode.Dynamic)
                {
                    modeStyle.normal.textColor = new Color(1f, 0.7f, 0.3f);
                }
                EditorGUILayout.LabelField(info.PopulationMode.ToString(), modeStyle, GUILayout.Width(60));
                
                // Atlas size
                EditorGUILayout.LabelField(info.AtlasSize, GUILayout.Width(80));
                
                // Character count
                EditorGUILayout.LabelField(info.CharacterCount.ToString(), GUILayout.Width(70));
                
                // Glyph count
                EditorGUILayout.LabelField(info.GlyphCount.ToString(), GUILayout.Width(60));
                
                // Memory size
                var memoryStyle = new GUIStyle(EditorStyles.label);
                if (info.MemorySize > 10 * 1024 * 1024) // > 10MB
                {
                    memoryStyle.normal.textColor = Color.red;
                }
                else if (info.MemorySize > 5 * 1024 * 1024) // > 5MB
                {
                    memoryStyle.normal.textColor = new Color(1f, 0.6f, 0f);
                }
                EditorGUILayout.LabelField(FormatBytes(info.MemorySize), memoryStyle, GUILayout.Width(80));
                
                // Status
                var statusStyle = new GUIStyle(EditorStyles.label);
                if (info.HasTypelessData)
                {
                    statusStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);
                    EditorGUILayout.LabelField("Can Clean", statusStyle, GUILayout.Width(80));
                }
                else
                {
                    EditorGUILayout.LabelField("-", GUILayout.Width(80));
                }
                
                GUILayout.FlexibleSpace();
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace();
            
            GUI.enabled = HasSelectedFonts();
            
            if (GUILayout.Button("Clear Selected Font Caches", GUILayout.Height(30), GUILayout.Width(200)))
            {
                ClearSelectedFontCaches();
            }
            
            GUI.enabled = true;
            
            if (GUILayout.Button("Clear All Dynamic Font Caches", GUILayout.Height(30), GUILayout.Width(200)))
            {
                if (EditorUtility.DisplayDialog("Clear All Dynamic Font Caches",
                    "This will clear the cache of all dynamic TMP fonts. Continue?",
                    "Yes", "Cancel"))
                {
                    ClearAllDynamicFontCaches();
                }
            }
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }
        
        private void RefreshFontList()
        {
            _fontAssets.Clear();
            _fontInfos.Clear();
            
            // Find all TMP_FontAsset in the project
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (font != null)
                {
                    _fontAssets.Add(font);
                    _fontInfos[font] = GetFontAssetInfo(font, path);
                }
            }
            
            // Sort by memory size (descending)
            _fontAssets.Sort((a, b) => _fontInfos[b].MemorySize.CompareTo(_fontInfos[a].MemorySize));
            
            _selectedFonts = new bool[_fontAssets.Count];
            _selectAll = false;
        }
        
        private FontAssetInfo GetFontAssetInfo(TMP_FontAsset font, string path)
        {
            var info = new FontAssetInfo
            {
                Name = font.name,
                Path = path,
                PopulationMode = font.atlasPopulationMode,
                CharacterCount = font.characterTable?.Count ?? 0,
                GlyphCount = font.glyphTable?.Count ?? 0,
                MemorySize = 0,
                HasTypelessData = false,
                AtlasSize = "N/A"
            };
            
            // Calculate memory size and atlas information
            var so = new SerializedObject(font);
            var prop = so.FindProperty("m_AtlasTextures");
            var atlasSizes = new List<string>();
            
            if (prop != null && prop.isArray)
            {
                for (int i = 0; i < prop.arraySize; i++)
                {
                    var textureProp = prop.GetArrayElementAtIndex(i);
                    if (textureProp.objectReferenceValue is Texture2D texture)
                    {
                        // Get texture memory size
                        var textureSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
                        info.MemorySize += textureSize;
                        
                        // Add atlas size info
                        atlasSizes.Add($"{texture.width}x{texture.height}");
                        
                        // For dynamic fonts, any texture size > minimum is cleanable
                        if (info.PopulationMode == AtlasPopulationMode.Dynamic)
                        {
                            // 256x256 Alpha8 = 64KB is a reasonable minimum size
                            if (textureSize > 64 * 1024)
                            {
                                info.HasTypelessData = true;
                            }
                        }
                    }
                }
            }
            
            // Set atlas size string
            if (atlasSizes.Count > 0)
            {
                info.AtlasSize = string.Join(", ", atlasSizes);
            }
            
            return info;
        }
        
        private bool IsVisibleFont(int index)
        {
            var font = _fontAssets[index];
            var info = _fontInfos[font];
            
            // Filter by dynamic mode
            if (_showOnlyDynamic && info.PopulationMode != AtlasPopulationMode.Dynamic)
                return false;
            
            // Filter by search
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                return info.Name.ToLower().Contains(_searchFilter.ToLower());
            }
            
            return true;
        }
        
        private bool HasSelectedFonts()
        {
            for (int i = 0; i < _selectedFonts.Length; i++)
            {
                if (_selectedFonts[i] && IsVisibleFont(i))
                    return true;
            }
            return false;
        }
        
        private long GetSelectedFontsMemorySize()
        {
            long size = 0;
            for (int i = 0; i < _selectedFonts.Length; i++)
            {
                if (_selectedFonts[i] && IsVisibleFont(i))
                {
                    size += _fontInfos[_fontAssets[i]].MemorySize;
                }
            }
            return size;
        }
        
        private void ClearSelectedFontCaches()
        {
            var selectedFonts = new List<TMP_FontAsset>();
            for (int i = 0; i < _selectedFonts.Length; i++)
            {
                if (_selectedFonts[i] && IsVisibleFont(i))
                {
                    selectedFonts.Add(_fontAssets[i]);
                }
            }
            
            if (selectedFonts.Count == 0)
                return;
            
            var message = $"Clear cache for {selectedFonts.Count} font(s)?";
            if (EditorUtility.DisplayDialog("Clear Font Caches", message, "Yes", "Cancel"))
            {
                ClearFontCaches(selectedFonts);
            }
        }
        
        private void ClearAllDynamicFontCaches()
        {
            var dynamicFonts = _fontAssets.Where(font => 
                _fontInfos[font].PopulationMode == AtlasPopulationMode.Dynamic).ToList();
            
            if (dynamicFonts.Count > 0)
            {
                ClearFontCaches(dynamicFonts);
            }
        }
        
        private void ClearFontCaches(List<TMP_FontAsset> fonts)
        {
            int clearedCount = 0;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                foreach (var font in fonts)
                {
                    if (ClearFontCache(font))
                    {
                        clearedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            
            EditorUtility.DisplayDialog("Clear Complete", 
                $"Cleared cache for {clearedCount} font(s).", "OK");
            
            RefreshFontList();
        }
        
        private bool ClearFontCache(TMP_FontAsset font)
        {
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                return false;
            
            bool cleared = false;
            
            // Clear character and glyph tables for dynamic fonts
            if (font.characterTable != null)
            {
                font.characterTable.Clear();
                cleared = true;
            }
            
            if (font.glyphTable != null)
            {
                font.glyphTable.Clear();
                cleared = true;
            }
            
            // Clear atlas textures - safe handling for null atlasTextures
            try
            {
                if (font.atlasTextures == null || font.atlasTextures.Length == 0)
                {
                    // Initialize with minimal atlas if not present
                    var defaultSize = 256;
                    var newTexture = new Texture2D(defaultSize, defaultSize, TextureFormat.Alpha8, false, true);
                    newTexture.name = font.name + " Atlas";
                    
                    // Fill with transparent pixels
                    var pixels = new Color32[defaultSize * defaultSize];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = new Color32(0, 0, 0, 0);
                    }
                    newTexture.SetPixels32(pixels);
                    newTexture.Apply();
                    
                    font.atlasTextures = new Texture2D[] { newTexture };
                    cleared = true;
                }
                else
                {
                    // Reset existing atlas textures
                    foreach (var texture in font.atlasTextures)
                    {
                        if (texture != null)
                        {
                            var pixels = new Color32[texture.width * texture.height];
                            for (int i = 0; i < pixels.Length; i++)
                            {
                                pixels[i] = new Color32(0, 0, 0, 0);
                            }
                            texture.SetPixels32(pixels);
                            texture.Apply();
                        }
                    }
                    cleared = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to clear atlas textures for font '{font.name}': {e.Message}");
                // Fallback: try to initialize with minimal atlas
                try
                {
                    var defaultSize = 256;
                    var newTexture = new Texture2D(defaultSize, defaultSize, TextureFormat.Alpha8, false, true);
                    newTexture.name = font.name + " Atlas";
                    font.atlasTextures = new Texture2D[] { newTexture };
                    cleared = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to initialize atlas for font '{font.name}': {ex.Message}");
                }
            }
            
            // Note: atlas properties are read-only in newer TMP versions
            
            // Clear lookup tables
            if (font.characterLookupTable != null)
            {
                font.characterLookupTable.Clear();
            }
            
            if (font.glyphLookupTable != null)
            {
                font.glyphLookupTable.Clear();
            }
            
            // Force TMP to rebuild the font
            font.ReadFontAssetDefinition();
            
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            
            return cleared;
        }
        
        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F1} GB";
        }
    }
}