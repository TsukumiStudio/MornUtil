using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MornLib
{
    public sealed class RaycastTargetVisualizerWindow : EditorWindow
    {
        private enum Tab
        {
            UGUI,
            Collider2D,
            Collider3D,
        }

        private const string PrefPrefix = "MornRaycastViz_";

        private static RaycastTargetVisualizerWindow _instance;

        // --- Common ---
        private Tab _currentTab;
        private float _borderWidth = 2f;
        private bool _showBorder = true;
        private bool _showFill = true;
        private int _labelFontSize = 10;
        private Color _labelColor = Color.white;
        private float _updateInterval = 0.1f;
        private float _lastUpdateTime;
        private GUIStyle _labelStyle;

        // --- UGUI ---
        private bool _uguiEnabled;
        private Color _uguiFillColor = new(1f, 0f, 0f, 0.3f);
        private Color _uguiBorderColor = new(1f, 0f, 0f, 0.8f);
        private bool _checkCanvasGroup = true;
        private readonly List<Graphic> _cachedGraphics = new();

        // --- Collider2D ---
        private bool _collider2DEnabled;
        private Color _c2dFillColor = new(0f, 1f, 0f, 0.2f);
        private Color _c2dBorderColor = new(0f, 1f, 0f, 0.8f);
        private bool _c2dShowTriggers = true;
        private bool _c2dShowNonTriggers = true;
        private readonly List<Collider2D> _cachedColliders2D = new();

        // --- Collider3D ---
        private bool _collider3DEnabled;
        private Color _c3dFillColor = new(0.1f, 0.2f, 1f, 0.15f);
        private Color _c3dBorderColor = new(0.2f, 0.4f, 1f, 0.8f);
        private bool _c3dShowTriggers = true;
        private bool _c3dShowNonTriggers = true;
        private bool _c3dIncludeMesh;
        private readonly List<Collider> _cachedColliders3D = new();

        private bool AnyEnabled => _uguiEnabled || _collider2DEnabled || _collider3DEnabled;

        [MenuItem("Tools/Raycastターゲット可視化")]
        public static void ShowWindow()
        {
            _instance = GetWindow<RaycastTargetVisualizerWindow>("Raycastターゲット可視化");
            _instance.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            _instance = this;
            LoadPrefs();
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            SavePrefs();
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _instance = null;
        }

        private void LoadPrefs()
        {
            _currentTab = (Tab)EditorPrefs.GetInt(PrefPrefix + "Tab", 0);
            _showFill = EditorPrefs.GetBool(PrefPrefix + "ShowFill", true);
            _showBorder = EditorPrefs.GetBool(PrefPrefix + "ShowBorder", true);
            _borderWidth = EditorPrefs.GetFloat(PrefPrefix + "BorderWidth", 2f);
            _updateInterval = EditorPrefs.GetFloat(PrefPrefix + "UpdateInterval", 0.1f);
            _labelFontSize = EditorPrefs.GetInt(PrefPrefix + "LabelFontSize", 10);
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "LabelColor", ""), out var lc))
                _labelColor = lc;

            _uguiEnabled = EditorPrefs.GetBool(PrefPrefix + "UGUI_Enabled", false);
            _checkCanvasGroup = EditorPrefs.GetBool(PrefPrefix + "CheckCanvasGroup", true);
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "FillColor", ""), out var fc))
                _uguiFillColor = fc;
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "BorderColor", ""), out var bc))
                _uguiBorderColor = bc;

            _collider2DEnabled = EditorPrefs.GetBool(PrefPrefix + "C2D_Enabled", false);
            _c2dShowTriggers = EditorPrefs.GetBool(PrefPrefix + "C2D_ShowTriggers", true);
            _c2dShowNonTriggers = EditorPrefs.GetBool(PrefPrefix + "C2D_ShowNonTriggers", true);
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "C2D_FillColor", ""), out var cfc))
                _c2dFillColor = cfc;
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "C2D_BorderColor", ""), out var cbc))
                _c2dBorderColor = cbc;

            _collider3DEnabled = EditorPrefs.GetBool(PrefPrefix + "C3D_Enabled", false);
            _c3dShowTriggers = EditorPrefs.GetBool(PrefPrefix + "C3D_ShowTriggers", true);
            _c3dShowNonTriggers = EditorPrefs.GetBool(PrefPrefix + "C3D_ShowNonTriggers", true);
            _c3dIncludeMesh = EditorPrefs.GetBool(PrefPrefix + "C3D_IncludeMesh", false);
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "C3D_FillColor", ""), out var c3fc))
                _c3dFillColor = c3fc;
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "C3D_BorderColor", ""), out var c3bc))
                _c3dBorderColor = c3bc;
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefPrefix + "Tab", (int)_currentTab);
            EditorPrefs.SetBool(PrefPrefix + "ShowFill", _showFill);
            EditorPrefs.SetBool(PrefPrefix + "ShowBorder", _showBorder);
            EditorPrefs.SetFloat(PrefPrefix + "BorderWidth", _borderWidth);
            EditorPrefs.SetFloat(PrefPrefix + "UpdateInterval", _updateInterval);
            EditorPrefs.SetInt(PrefPrefix + "LabelFontSize", _labelFontSize);
            EditorPrefs.SetString(PrefPrefix + "LabelColor", "#" + ColorUtility.ToHtmlStringRGBA(_labelColor));

            EditorPrefs.SetBool(PrefPrefix + "UGUI_Enabled", _uguiEnabled);
            EditorPrefs.SetBool(PrefPrefix + "CheckCanvasGroup", _checkCanvasGroup);
            EditorPrefs.SetString(PrefPrefix + "FillColor", "#" + ColorUtility.ToHtmlStringRGBA(_uguiFillColor));
            EditorPrefs.SetString(PrefPrefix + "BorderColor", "#" + ColorUtility.ToHtmlStringRGBA(_uguiBorderColor));

            EditorPrefs.SetBool(PrefPrefix + "C2D_Enabled", _collider2DEnabled);
            EditorPrefs.SetBool(PrefPrefix + "C2D_ShowTriggers", _c2dShowTriggers);
            EditorPrefs.SetBool(PrefPrefix + "C2D_ShowNonTriggers", _c2dShowNonTriggers);
            EditorPrefs.SetString(PrefPrefix + "C2D_FillColor", "#" + ColorUtility.ToHtmlStringRGBA(_c2dFillColor));
            EditorPrefs.SetString(PrefPrefix + "C2D_BorderColor", "#" + ColorUtility.ToHtmlStringRGBA(_c2dBorderColor));

            EditorPrefs.SetBool(PrefPrefix + "C3D_Enabled", _collider3DEnabled);
            EditorPrefs.SetBool(PrefPrefix + "C3D_ShowTriggers", _c3dShowTriggers);
            EditorPrefs.SetBool(PrefPrefix + "C3D_ShowNonTriggers", _c3dShowNonTriggers);
            EditorPrefs.SetBool(PrefPrefix + "C3D_IncludeMesh", _c3dIncludeMesh);
            EditorPrefs.SetString(PrefPrefix + "C3D_FillColor", "#" + ColorUtility.ToHtmlStringRGBA(_c3dFillColor));
            EditorPrefs.SetString(PrefPrefix + "C3D_BorderColor", "#" + ColorUtility.ToHtmlStringRGBA(_c3dBorderColor));
        }

        private void ResetPrefs()
        {
            var keys = new[]
            {
                "Tab", "ShowFill", "ShowBorder", "BorderWidth", "UpdateInterval", "LabelFontSize", "LabelColor",
                "UGUI_Enabled", "CheckCanvasGroup", "FillColor", "BorderColor",
                "C2D_Enabled", "C2D_ShowTriggers", "C2D_ShowNonTriggers", "C2D_FillColor", "C2D_BorderColor",
                "C3D_Enabled", "C3D_ShowTriggers", "C3D_ShowNonTriggers", "C3D_IncludeMesh", "C3D_FillColor", "C3D_BorderColor",
            };
            foreach (var key in keys)
                EditorPrefs.DeleteKey(PrefPrefix + key);

            // Reset fields to defaults
            _currentTab = Tab.UGUI;
            _showFill = true;
            _showBorder = true;
            _borderWidth = 2f;
            _labelFontSize = 10;
            _labelColor = Color.white;
            _updateInterval = 0.1f;

            _uguiEnabled = false;
            _uguiFillColor = new Color(1f, 0f, 0f, 0.3f);
            _uguiBorderColor = new Color(1f, 0f, 0f, 0.8f);
            _checkCanvasGroup = true;

            _collider2DEnabled = false;
            _c2dFillColor = new Color(0f, 1f, 0f, 0.2f);
            _c2dBorderColor = new Color(0f, 1f, 0f, 0.8f);
            _c2dShowTriggers = true;
            _c2dShowNonTriggers = true;

            _collider3DEnabled = false;
            _c3dFillColor = new Color(0.1f, 0.2f, 1f, 0.15f);
            _c3dBorderColor = new Color(0.2f, 0.4f, 1f, 0.8f);
            _c3dShowTriggers = true;
            _c3dShowNonTriggers = true;
            _c3dIncludeMesh = false;
        }

        private string TabLabel(Tab tab)
        {
            return tab switch
            {
                Tab.UGUI => (_uguiEnabled ? "\u2705 " : "\u274c ") + "UGUI",
                Tab.Collider2D => (_collider2DEnabled ? "\u2705 " : "\u274c ") + "2D",
                Tab.Collider3D => (_collider3DEnabled ? "\u2705 " : "\u274c ") + "3D",
                _ => tab.ToString(),
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Raycastターゲット可視化", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            // Common settings
            EditorGUILayout.LabelField("共通設定", EditorStyles.boldLabel);
            _showFill = EditorGUILayout.Toggle("塗りつぶし表示", _showFill);
            _showBorder = EditorGUILayout.Toggle("枠線表示", _showBorder);
            _borderWidth = EditorGUILayout.Slider("枠線の太さ", _borderWidth, 1f, 10f);
            _labelFontSize = EditorGUILayout.IntSlider("文字サイズ", _labelFontSize, 6, 24);
            _labelColor = EditorGUILayout.ColorField("文字色", _labelColor);
            _updateInterval = EditorGUILayout.Slider("更新間隔", _updateInterval, 0.01f, 1f);

            EditorGUILayout.Space();

            // Status
            if (_uguiEnabled)
                EditorGUILayout.LabelField($"UGUI: {_cachedGraphics.Count}");
            if (_collider2DEnabled)
                EditorGUILayout.LabelField($"Collider2D: {_cachedColliders2D.Count}");
            if (_collider3DEnabled)
                EditorGUILayout.LabelField($"Collider3D: {_cachedColliders3D.Count}");

            if (GUILayout.Button("強制更新"))
            {
                UpdateCache();
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("設定リセット"))
            {
                ResetPrefs();
                _labelStyle = null;
                _lastUpdateTime = 0f;
                UpdateCache();
                SceneView.RepaintAll();
                Repaint();
            }

            EditorGUILayout.Space();

            // Tab
            var tabLabels = new[] { TabLabel(Tab.UGUI), TabLabel(Tab.Collider2D), TabLabel(Tab.Collider3D) };
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, tabLabels);

            EditorGUILayout.Space();

            // Tab-specific
            switch (_currentTab)
            {
                case Tab.UGUI:
                    _uguiEnabled = EditorGUILayout.Toggle("有効化", _uguiEnabled);
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledGroupScope(!_uguiEnabled))
                    {
                        DrawUGUISettings();
                    }
                    break;
                case Tab.Collider2D:
                    _collider2DEnabled = EditorGUILayout.Toggle("有効化", _collider2DEnabled);
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledGroupScope(!_collider2DEnabled))
                    {
                        DrawCollider2DSettings();
                    }
                    break;
                case Tab.Collider3D:
                    _collider3DEnabled = EditorGUILayout.Toggle("有効化", _collider3DEnabled);
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledGroupScope(!_collider3DEnabled))
                    {
                        DrawCollider3DSettings();
                    }
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                _labelStyle = null;
                _lastUpdateTime = 0f;
                UpdateCache();
                SavePrefs();
                SceneView.RepaintAll();
            }
        }

        private void DrawUGUISettings()
        {
            _uguiFillColor = EditorGUILayout.ColorField("塗りつぶし色", _uguiFillColor);
            _uguiBorderColor = EditorGUILayout.ColorField("枠線色", _uguiBorderColor);
            _checkCanvasGroup = EditorGUILayout.Toggle("CanvasGroupを考慮", _checkCanvasGroup);
            EditorGUILayout.HelpBox(
                "有効時、blocksRaycasts=falseのCanvasGroup配下のGraphicを除外します",
                MessageType.Info);
        }

        private void DrawCollider2DSettings()
        {
            _c2dFillColor = EditorGUILayout.ColorField("塗りつぶし色", _c2dFillColor);
            _c2dBorderColor = EditorGUILayout.ColorField("枠線色", _c2dBorderColor);
            _c2dShowTriggers = EditorGUILayout.Toggle("Trigger表示", _c2dShowTriggers);
            _c2dShowNonTriggers = EditorGUILayout.Toggle("非Trigger表示", _c2dShowNonTriggers);
        }

        private void DrawCollider3DSettings()
        {
            _c3dFillColor = EditorGUILayout.ColorField("塗りつぶし色", _c3dFillColor);
            _c3dBorderColor = EditorGUILayout.ColorField("枠線色", _c3dBorderColor);
            _c3dShowTriggers = EditorGUILayout.Toggle("Trigger表示", _c3dShowTriggers);
            _c3dShowNonTriggers = EditorGUILayout.Toggle("非Trigger表示", _c3dShowNonTriggers);
            _c3dIncludeMesh = EditorGUILayout.Toggle("MeshColliderを含む", _c3dIncludeMesh);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!AnyEnabled) return;
            UpdateCache();
            _lastUpdateTime = Time.realtimeSinceStartup;
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!AnyEnabled) return;
            if (Time.realtimeSinceStartup - _lastUpdateTime > _updateInterval)
            {
                UpdateCache();
                _lastUpdateTime = Time.realtimeSinceStartup;
                SceneView.RepaintAll();
                Repaint();
            }
        }

        // ==================== Cache ====================

        private void UpdateCache()
        {
            if (_uguiEnabled) UpdateCachedGraphics();
            else _cachedGraphics.Clear();

            if (_collider2DEnabled) UpdateCachedColliders2D();
            else _cachedColliders2D.Clear();

            if (_collider3DEnabled) UpdateCachedColliders3D();
            else _cachedColliders3D.Clear();
        }

        private void ForEachRoot(System.Action<GameObject> action)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                action(prefabStage.prefabContentsRoot);
                return;
            }

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    action(root);
                }
            }
        }

        private void UpdateCachedGraphics()
        {
            _cachedGraphics.Clear();
            ForEachRoot(root =>
            {
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic != null && IsGraphicRaycastable(graphic))
                        _cachedGraphics.Add(graphic);
                }
            });
        }

        private void UpdateCachedColliders2D()
        {
            _cachedColliders2D.Clear();
            ForEachRoot(root =>
            {
                foreach (var col in root.GetComponentsInChildren<Collider2D>(true))
                {
                    if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                    if (col.isTrigger && !_c2dShowTriggers) continue;
                    if (!col.isTrigger && !_c2dShowNonTriggers) continue;
                    _cachedColliders2D.Add(col);
                }
            });
        }

        private void UpdateCachedColliders3D()
        {
            _cachedColliders3D.Clear();
            ForEachRoot(root =>
            {
                foreach (var col in root.GetComponentsInChildren<Collider>(true))
                {
                    if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                    if (col is MeshCollider && !_c3dIncludeMesh) continue;
                    if (col.isTrigger && !_c3dShowTriggers) continue;
                    if (!col.isTrigger && !_c3dShowNonTriggers) continue;
                    _cachedColliders3D.Add(col);
                }
            });
        }

        private bool IsGraphicRaycastable(Graphic graphic)
        {
            if (!graphic.raycastTarget) return false;
            if (!graphic.gameObject.activeInHierarchy) return false;
            if (!_checkCanvasGroup) return true;

            var canvasGroups = graphic.GetComponentsInParent<CanvasGroup>(true);
            foreach (var cg in canvasGroups)
            {
                if (!cg.blocksRaycasts) return false;
                if (cg.ignoreParentGroups) break;
            }
            return true;
        }

        // ==================== Drawing ====================

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_uguiEnabled) DrawUGUIOverlay();
            if (_collider2DEnabled) DrawCollider2DOverlay();
            if (_collider3DEnabled) DrawCollider3DOverlay();
        }

        private void DrawUGUIOverlay()
        {
            Handles.BeginGUI();
            foreach (var graphic in _cachedGraphics)
            {
                if (graphic == null || !graphic.gameObject.activeInHierarchy) continue;
                DrawGraphicVisualization(graphic);
            }
            Handles.EndGUI();
        }

        private void DrawCollider2DOverlay()
        {
            foreach (var col in _cachedColliders2D)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                DrawCollider2DVisualization(col);
            }
        }

        private void DrawCollider3DOverlay()
        {
            foreach (var col in _cachedColliders3D)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                DrawCollider3DVisualization(col);
            }
        }

        private GUIStyle GetLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
                {
                    fontSize = _labelFontSize,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = _labelColor },
                };
            }
            return _labelStyle;
        }

        // --- UGUI Drawing ---

        private void DrawGraphicVisualization(Graphic graphic)
        {
            var rt = graphic.rectTransform;
            if (rt == null || graphic.canvas == null) return;

            var worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            var guiCorners = new Vector3[4];
            for (var i = 0; i < 4; i++)
                guiCorners[i] = HandleUtility.WorldToGUIPoint(worldCorners[i]);

            if (_showFill)
                Handles.DrawSolidRectangleWithOutline(guiCorners, _uguiFillColor, Color.clear);
            if (_showBorder)
            {
                var oldColor = Handles.color;
                Handles.color = _uguiBorderColor;
                for (var i = 0; i < 4; i++)
                    Handles.DrawLine(guiCorners[i], guiCorners[(i + 1) % 4], _borderWidth);
                Handles.color = oldColor;
            }

            var style = GetLabelStyle();
            var center = (guiCorners[0] + guiCorners[2]) / 2f;
            var labelWidth = _labelFontSize * 10f;
            var labelHeight = _labelFontSize + 4f;
            GUI.Label(
                new Rect(center.x - labelWidth / 2f, center.y - labelHeight / 2f, labelWidth, labelHeight),
                graphic.GetType().Name, style);
        }

        // --- Collider2D Drawing ---

        private void DrawCollider2DVisualization(Collider2D col)
        {
            var fill = _c2dFillColor;
            var border = _c2dBorderColor;
            if (col.isTrigger) { fill.a *= 0.5f; border.a *= 0.7f; }

            switch (col)
            {
                case BoxCollider2D box: DrawBoxCollider2D(box, fill, border); break;
                case CircleCollider2D circle: DrawCircleCollider2D(circle, fill, border); break;
                case CapsuleCollider2D capsule: DrawCapsuleCollider2D(capsule, border); break;
                case PolygonCollider2D polygon: DrawPolygonCollider2D(polygon, fill, border); break;
                case EdgeCollider2D edge: DrawEdgeCollider2D(edge, border); break;
            }

            var worldPos = col.transform.TransformPoint(col.offset);
            var labelText = col.isTrigger ? $"{col.gameObject.name} (T)" : col.gameObject.name;
            Handles.Label(worldPos, labelText, GetLabelStyle());
        }

        private void DrawBoxCollider2D(BoxCollider2D box, Color fill, Color border)
        {
            var t = box.transform;
            var halfX = box.size.x / 2f;
            var halfY = box.size.y / 2f;
            var corners = new[]
            {
                t.TransformPoint(box.offset + new Vector2(-halfX, -halfY)),
                t.TransformPoint(box.offset + new Vector2(-halfX, halfY)),
                t.TransformPoint(box.offset + new Vector2(halfX, halfY)),
                t.TransformPoint(box.offset + new Vector2(halfX, -halfY)),
            };

            if (_showFill) Handles.DrawSolidRectangleWithOutline(corners, fill, Color.clear);
            if (_showBorder)
            {
                Handles.color = border;
                for (var i = 0; i < 4; i++)
                    Handles.DrawLine(corners[i], corners[(i + 1) % 4], _borderWidth);
            }
        }

        private void DrawCircleCollider2D(CircleCollider2D circle, Color fill, Color border)
        {
            var t = circle.transform;
            var center = t.TransformPoint(circle.offset);
            var scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
            var radius = circle.radius * scale;

            if (_showFill) { Handles.color = fill; Handles.DrawSolidDisc(center, Vector3.forward, radius); }
            if (_showBorder) { Handles.color = border; Handles.DrawWireDisc(center, Vector3.forward, radius, _borderWidth); }
        }

        private void DrawCapsuleCollider2D(CapsuleCollider2D capsule, Color border)
        {
            var t = capsule.transform;
            var center = t.TransformPoint(capsule.offset);
            var sx = Mathf.Abs(t.lossyScale.x);
            var sy = Mathf.Abs(t.lossyScale.y);
            var halfW = capsule.size.x * sx / 2f;
            var halfH = capsule.size.y * sy / 2f;

            if (!_showBorder) return;
            Handles.color = border;
            const int segments = 32;
            var prev = center + new Vector3(halfW, 0, 0);
            for (var i = 1; i <= segments; i++)
            {
                var angle = 2f * Mathf.PI * i / segments;
                var next = center + new Vector3(halfW * Mathf.Cos(angle), halfH * Mathf.Sin(angle), 0);
                Handles.DrawLine(prev, next, _borderWidth);
                prev = next;
            }
        }

        private void DrawPolygonCollider2D(PolygonCollider2D polygon, Color fill, Color border)
        {
            var t = polygon.transform;
            for (var p = 0; p < polygon.pathCount; p++)
            {
                var path = polygon.GetPath(p);
                if (path.Length < 2) continue;
                var wp = new Vector3[path.Length];
                for (var i = 0; i < path.Length; i++) wp[i] = t.TransformPoint(path[i]);

                if (_showFill && path.Length >= 3) { Handles.color = fill; Handles.DrawAAConvexPolygon(wp); }
                if (_showBorder)
                {
                    Handles.color = border;
                    for (var i = 0; i < wp.Length; i++)
                        Handles.DrawLine(wp[i], wp[(i + 1) % wp.Length], _borderWidth);
                }
            }
        }

        private void DrawEdgeCollider2D(EdgeCollider2D edge, Color border)
        {
            if (!_showBorder) return;
            var t = edge.transform;
            var points = edge.points;
            if (points.Length < 2) return;
            Handles.color = border;
            for (var i = 0; i < points.Length - 1; i++)
                Handles.DrawLine(t.TransformPoint(points[i]), t.TransformPoint(points[i + 1]), _borderWidth);
        }

        // --- Collider3D Drawing ---

        private void DrawCollider3DVisualization(Collider col)
        {
            var fill = _c3dFillColor;
            var border = _c3dBorderColor;
            if (col.isTrigger) { fill.a *= 0.5f; border.a *= 0.7f; }

            switch (col)
            {
                case BoxCollider box: DrawBoxCollider3D(box, fill, border); break;
                case SphereCollider sphere: DrawSphereCollider3D(sphere, fill, border); break;
                case CapsuleCollider capsule: DrawCapsuleCollider3D(capsule, border); break;
                case MeshCollider mesh: DrawMeshCollider3D(mesh, border); break;
            }

            var worldPos = col.bounds.center;
            var labelText = col.isTrigger ? $"{col.gameObject.name} (T)" : col.gameObject.name;
            Handles.Label(worldPos, labelText, GetLabelStyle());
        }

        private void DrawBoxCollider3D(BoxCollider box, Color fill, Color border)
        {
            var t = box.transform;
            var half = box.size / 2f;
            var offsets = new[]
            {
                new Vector3(-half.x, -half.y, -half.z), // 0
                new Vector3(-half.x, -half.y, half.z),  // 1
                new Vector3(-half.x, half.y, -half.z),  // 2
                new Vector3(-half.x, half.y, half.z),   // 3
                new Vector3(half.x, -half.y, -half.z),  // 4
                new Vector3(half.x, -half.y, half.z),   // 5
                new Vector3(half.x, half.y, -half.z),   // 6
                new Vector3(half.x, half.y, half.z),    // 7
            };
            var v = new Vector3[8];
            for (var i = 0; i < 8; i++)
                v[i] = t.TransformPoint(box.center + offsets[i]);

            if (_showFill)
            {
                Handles.DrawSolidRectangleWithOutline(new[] { v[0], v[1], v[5], v[4] }, fill, Color.clear); // bottom
                Handles.DrawSolidRectangleWithOutline(new[] { v[2], v[3], v[7], v[6] }, fill, Color.clear); // top
                Handles.DrawSolidRectangleWithOutline(new[] { v[0], v[1], v[3], v[2] }, fill, Color.clear); // left
                Handles.DrawSolidRectangleWithOutline(new[] { v[4], v[5], v[7], v[6] }, fill, Color.clear); // right
                Handles.DrawSolidRectangleWithOutline(new[] { v[0], v[4], v[6], v[2] }, fill, Color.clear); // front
                Handles.DrawSolidRectangleWithOutline(new[] { v[1], v[5], v[7], v[3] }, fill, Color.clear); // back
            }
            if (_showBorder)
            {
                Handles.color = border;
                Handles.DrawLine(v[0], v[1], _borderWidth); Handles.DrawLine(v[1], v[5], _borderWidth);
                Handles.DrawLine(v[5], v[4], _borderWidth); Handles.DrawLine(v[4], v[0], _borderWidth);
                Handles.DrawLine(v[2], v[3], _borderWidth); Handles.DrawLine(v[3], v[7], _borderWidth);
                Handles.DrawLine(v[7], v[6], _borderWidth); Handles.DrawLine(v[6], v[2], _borderWidth);
                Handles.DrawLine(v[0], v[2], _borderWidth); Handles.DrawLine(v[1], v[3], _borderWidth);
                Handles.DrawLine(v[4], v[6], _borderWidth); Handles.DrawLine(v[5], v[7], _borderWidth);
            }
        }

        private void DrawSphereCollider3D(SphereCollider sphere, Color fill, Color border)
        {
            var t = sphere.transform;
            var center = t.TransformPoint(sphere.center);
            var scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));
            var radius = sphere.radius * scale;

            if (_showFill)
            {
                Handles.color = fill;
                Handles.DrawSolidDisc(center, Vector3.up, radius);
                Handles.DrawSolidDisc(center, Vector3.right, radius);
                Handles.DrawSolidDisc(center, Vector3.forward, radius);
            }
            if (_showBorder)
            {
                Handles.color = border;
                Handles.DrawWireDisc(center, Vector3.up, radius, _borderWidth);
                Handles.DrawWireDisc(center, Vector3.right, radius, _borderWidth);
                Handles.DrawWireDisc(center, Vector3.forward, radius, _borderWidth);
            }
        }

        private void DrawCapsuleCollider3D(CapsuleCollider capsule, Color border)
        {
            if (!_showBorder) return;
            var t = capsule.transform;
            var center = t.TransformPoint(capsule.center);
            var ls = t.lossyScale;
            var radius = capsule.radius;
            var height = capsule.height;

            // direction: 0=X, 1=Y, 2=Z
            Vector3 up;
            float axisScale, radScale;
            switch (capsule.direction)
            {
                case 0:
                    up = t.right;
                    axisScale = Mathf.Abs(ls.x);
                    radScale = Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    break;
                case 2:
                    up = t.forward;
                    axisScale = Mathf.Abs(ls.z);
                    radScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
                    break;
                default:
                    up = t.up;
                    axisScale = Mathf.Abs(ls.y);
                    radScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z));
                    break;
            }

            var r = radius * radScale;
            var halfH = Mathf.Max(height * axisScale / 2f - r, 0f);

            Handles.color = border;
            Handles.DrawWireDisc(center + up * halfH, up, r, _borderWidth);
            Handles.DrawWireDisc(center - up * halfH, up, r, _borderWidth);

            // 4 lines connecting the two discs
            var right = Vector3.Cross(up, Vector3.one != up ? Vector3.one : Vector3.right).normalized;
            var forward = Vector3.Cross(up, right).normalized;
            var top = center + up * halfH;
            var bottom = center - up * halfH;
            Handles.DrawLine(top + right * r, bottom + right * r, _borderWidth);
            Handles.DrawLine(top - right * r, bottom - right * r, _borderWidth);
            Handles.DrawLine(top + forward * r, bottom + forward * r, _borderWidth);
            Handles.DrawLine(top - forward * r, bottom - forward * r, _borderWidth);
        }

        private void DrawMeshCollider3D(MeshCollider mesh, Color border)
        {
            if (!_showBorder) return;
            var sharedMesh = mesh.sharedMesh;
            if (sharedMesh == null) return;

            var t = mesh.transform;
            var triangles = sharedMesh.triangles;
            var vertices = sharedMesh.vertices;

            Handles.color = border;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = t.TransformPoint(vertices[triangles[i]]);
                var b = t.TransformPoint(vertices[triangles[i + 1]]);
                var c = t.TransformPoint(vertices[triangles[i + 2]]);
                Handles.DrawLine(a, b, _borderWidth);
                Handles.DrawLine(b, c, _borderWidth);
                Handles.DrawLine(c, a, _borderWidth);
            }
        }
    }
}
