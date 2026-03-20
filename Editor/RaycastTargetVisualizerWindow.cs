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
        }

        private const string PrefPrefix = "MornRaycastViz_";

        private static RaycastTargetVisualizerWindow _instance;

        // --- Common ---
        private Tab _currentTab;
        private float _borderWidth = 2f;
        private bool _showBorder = true;
        private bool _showFill = true;
        private int _labelFontSize = 10;
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
        private Color _colliderFillColor = new(0f, 1f, 0f, 0.2f);
        private Color _colliderBorderColor = new(0f, 1f, 0f, 0.8f);
        private bool _showTriggers = true;
        private bool _showNonTriggers = true;
        private readonly List<Collider2D> _cachedColliders = new();

        private bool AnyEnabled => _uguiEnabled || _collider2DEnabled;

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

            _uguiEnabled = EditorPrefs.GetBool(PrefPrefix + "UGUI_Enabled", false);
            _checkCanvasGroup = EditorPrefs.GetBool(PrefPrefix + "CheckCanvasGroup", true);
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "FillColor", ""), out var fc))
                _uguiFillColor = fc;
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "BorderColor", ""), out var bc))
                _uguiBorderColor = bc;

            _collider2DEnabled = EditorPrefs.GetBool(PrefPrefix + "C2D_Enabled", false);
            _showTriggers = EditorPrefs.GetBool(PrefPrefix + "C2D_ShowTriggers", true);
            _showNonTriggers = EditorPrefs.GetBool(PrefPrefix + "C2D_ShowNonTriggers", true);
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "C2D_FillColor", ""), out var cfc))
                _colliderFillColor = cfc;
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "C2D_BorderColor", ""), out var cbc))
                _colliderBorderColor = cbc;
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefPrefix + "Tab", (int)_currentTab);
            EditorPrefs.SetBool(PrefPrefix + "ShowFill", _showFill);
            EditorPrefs.SetBool(PrefPrefix + "ShowBorder", _showBorder);
            EditorPrefs.SetFloat(PrefPrefix + "BorderWidth", _borderWidth);
            EditorPrefs.SetFloat(PrefPrefix + "UpdateInterval", _updateInterval);
            EditorPrefs.SetInt(PrefPrefix + "LabelFontSize", _labelFontSize);

            EditorPrefs.SetBool(PrefPrefix + "UGUI_Enabled", _uguiEnabled);
            EditorPrefs.SetBool(PrefPrefix + "CheckCanvasGroup", _checkCanvasGroup);
            EditorPrefs.SetString(PrefPrefix + "FillColor", "#" + ColorUtility.ToHtmlStringRGBA(_uguiFillColor));
            EditorPrefs.SetString(PrefPrefix + "BorderColor", "#" + ColorUtility.ToHtmlStringRGBA(_uguiBorderColor));

            EditorPrefs.SetBool(PrefPrefix + "C2D_Enabled", _collider2DEnabled);
            EditorPrefs.SetBool(PrefPrefix + "C2D_ShowTriggers", _showTriggers);
            EditorPrefs.SetBool(PrefPrefix + "C2D_ShowNonTriggers", _showNonTriggers);
            EditorPrefs.SetString(PrefPrefix + "C2D_FillColor", "#" + ColorUtility.ToHtmlStringRGBA(_colliderFillColor));
            EditorPrefs.SetString(PrefPrefix + "C2D_BorderColor", "#" + ColorUtility.ToHtmlStringRGBA(_colliderBorderColor));
        }

        private string TabLabel(Tab tab)
        {
            return tab switch
            {
                Tab.UGUI => (_uguiEnabled ? "\u2705 " : "\u274c ") + "UGUI",
                Tab.Collider2D => (_collider2DEnabled ? "\u2705 " : "\u274c ") + "Collider2D",
                _ => tab.ToString(),
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Raycastターゲット可視化", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            // Common settings (top)
            EditorGUILayout.LabelField("共通設定", EditorStyles.boldLabel);
            _showFill = EditorGUILayout.Toggle("塗りつぶし表示", _showFill);
            _showBorder = EditorGUILayout.Toggle("枠線表示", _showBorder);
            if (_showBorder)
            {
                _borderWidth = EditorGUILayout.Slider("枠線の太さ", _borderWidth, 1f, 10f);
            }
            _labelFontSize = EditorGUILayout.IntSlider("文字サイズ", _labelFontSize, 6, 24);
            _updateInterval = EditorGUILayout.Slider("更新間隔", _updateInterval, 0.01f, 1f);

            EditorGUILayout.Space();

            // Tab
            var tabLabels = new[] { TabLabel(Tab.UGUI), TabLabel(Tab.Collider2D) };
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, tabLabels);

            EditorGUILayout.Space();

            // Tab-specific enable + settings
            switch (_currentTab)
            {
                case Tab.UGUI:
                    _uguiEnabled = EditorGUILayout.Toggle("UGUI可視化を有効にする", _uguiEnabled);
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledGroupScope(!_uguiEnabled))
                    {
                        DrawUGUISettings();
                    }
                    break;
                case Tab.Collider2D:
                    _collider2DEnabled = EditorGUILayout.Toggle("Collider2D可視化を有効にする", _collider2DEnabled);
                    EditorGUILayout.Space();
                    using (new EditorGUI.DisabledGroupScope(!_collider2DEnabled))
                    {
                        DrawCollider2DSettings();
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

            EditorGUILayout.Space();

            // Status
            if (_uguiEnabled)
                EditorGUILayout.LabelField($"UGUI: {_cachedGraphics.Count}");
            if (_collider2DEnabled)
                EditorGUILayout.LabelField($"Collider2D: {_cachedColliders.Count}");

            if (AnyEnabled)
            {
                EditorGUILayout.LabelField($"{_updateInterval:F2}秒ごとに自動更新中", EditorStyles.helpBox);
            }

            if (GUILayout.Button("強制更新"))
            {
                UpdateCache();
                SceneView.RepaintAll();
            }
        }

        private void DrawUGUISettings()
        {
            if (_showFill)
            {
                _uguiFillColor = EditorGUILayout.ColorField("塗りつぶし色", _uguiFillColor);
            }
            if (_showBorder)
            {
                _uguiBorderColor = EditorGUILayout.ColorField("枠線色", _uguiBorderColor);
            }
            _checkCanvasGroup = EditorGUILayout.Toggle("CanvasGroupを考慮", _checkCanvasGroup);
            EditorGUILayout.HelpBox(
                "有効時、blocksRaycasts=falseのCanvasGroup配下のGraphicを除外します",
                MessageType.Info);
        }

        private void DrawCollider2DSettings()
        {
            if (_showFill)
            {
                _colliderFillColor = EditorGUILayout.ColorField("塗りつぶし色", _colliderFillColor);
            }
            if (_showBorder)
            {
                _colliderBorderColor = EditorGUILayout.ColorField("枠線色", _colliderBorderColor);
            }
            _showTriggers = EditorGUILayout.Toggle("Trigger表示", _showTriggers);
            _showNonTriggers = EditorGUILayout.Toggle("非Trigger表示", _showNonTriggers);
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

            if (_collider2DEnabled) UpdateCachedColliders();
            else _cachedColliders.Clear();
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
                    {
                        _cachedGraphics.Add(graphic);
                    }
                }
            });
        }

        private void UpdateCachedColliders()
        {
            _cachedColliders.Clear();
            ForEachRoot(root =>
            {
                foreach (var col in root.GetComponentsInChildren<Collider2D>(true))
                {
                    if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                    if (col.isTrigger && !_showTriggers) continue;
                    if (!col.isTrigger && !_showNonTriggers) continue;
                    _cachedColliders.Add(col);
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
            foreach (var col in _cachedColliders)
            {
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;
                DrawCollider2DVisualization(col);
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
                };
            }
            return _labelStyle;
        }

        private void DrawGraphicVisualization(Graphic graphic)
        {
            var rt = graphic.rectTransform;
            if (rt == null || graphic.canvas == null) return;

            var worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            var guiCorners = new Vector3[4];
            for (var i = 0; i < 4; i++)
            {
                guiCorners[i] = HandleUtility.WorldToGUIPoint(worldCorners[i]);
            }

            if (_showFill)
            {
                Handles.DrawSolidRectangleWithOutline(guiCorners, _uguiFillColor, Color.clear);
            }
            if (_showBorder)
            {
                DrawBorderGUI(guiCorners, _uguiBorderColor);
            }

            var style = GetLabelStyle();
            var center = (guiCorners[0] + guiCorners[2]) / 2f;
            var labelWidth = _labelFontSize * 10f;
            var labelHeight = _labelFontSize + 4f;
            GUI.Label(
                new Rect(center.x - labelWidth / 2f, center.y - labelHeight / 2f, labelWidth, labelHeight),
                graphic.GetType().Name, style);
        }

        private void DrawBorderGUI(Vector3[] guiCorners, Color color)
        {
            var oldColor = Handles.color;
            Handles.color = color;
            for (var offset = -_borderWidth / 2; offset <= _borderWidth / 2; offset += 0.5f)
            {
                var oc = new Vector3[4];
                for (var i = 0; i < 4; i++)
                    oc[i] = guiCorners[i] + Vector3.one * offset;
                for (var i = 0; i < 4; i++)
                    Handles.DrawLine(oc[i], oc[(i + 1) % 4]);
            }
            Handles.color = oldColor;
        }

        private void DrawCollider2DVisualization(Collider2D col)
        {
            var fill = _colliderFillColor;
            var border = _colliderBorderColor;

            if (col.isTrigger)
            {
                fill.a *= 0.5f;
                border.a *= 0.7f;
            }

            switch (col)
            {
                case BoxCollider2D box:
                    DrawBoxCollider2D(box, fill, border);
                    break;
                case CircleCollider2D circle:
                    DrawCircleCollider2D(circle, fill, border);
                    break;
                case CapsuleCollider2D capsule:
                    DrawCapsuleCollider2D(capsule, fill, border);
                    break;
                case PolygonCollider2D polygon:
                    DrawPolygonCollider2D(polygon, fill, border);
                    break;
                case EdgeCollider2D edge:
                    DrawEdgeCollider2D(edge, border);
                    break;
            }

            var worldPos = col.transform.TransformPoint(col.offset);
            var labelText = col.isTrigger ? $"{col.GetType().Name} (T)" : col.GetType().Name;
            Handles.Label(worldPos, labelText, GetLabelStyle());
        }

        private void DrawBoxCollider2D(BoxCollider2D box, Color fill, Color border)
        {
            var t = box.transform;
            var size = box.size;
            var halfX = size.x / 2f;
            var halfY = size.y / 2f;

            var corners = new[]
            {
                t.TransformPoint(box.offset + new Vector2(-halfX, -halfY)),
                t.TransformPoint(box.offset + new Vector2(-halfX, halfY)),
                t.TransformPoint(box.offset + new Vector2(halfX, halfY)),
                t.TransformPoint(box.offset + new Vector2(halfX, -halfY)),
            };

            if (_showFill)
            {
                Handles.DrawSolidRectangleWithOutline(corners, fill, Color.clear);
            }
            if (_showBorder)
            {
                var oldColor = Handles.color;
                Handles.color = border;
                for (var i = 0; i < 4; i++)
                    Handles.DrawLine(corners[i], corners[(i + 1) % 4]);
                Handles.color = oldColor;
            }
        }

        private void DrawCircleCollider2D(CircleCollider2D circle, Color fill, Color border)
        {
            var t = circle.transform;
            var center = t.TransformPoint(circle.offset);
            var scale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
            var radius = circle.radius * scale;

            if (_showFill)
            {
                Handles.color = fill;
                Handles.DrawSolidDisc(center, Vector3.forward, radius);
            }
            if (_showBorder)
            {
                Handles.color = border;
                Handles.DrawWireDisc(center, Vector3.forward, radius);
            }
        }

        private void DrawCapsuleCollider2D(CapsuleCollider2D capsule, Color fill, Color border)
        {
            var t = capsule.transform;
            var center = t.TransformPoint(capsule.offset);
            var sx = Mathf.Abs(t.lossyScale.x);
            var sy = Mathf.Abs(t.lossyScale.y);
            var w = capsule.size.x * sx;
            var h = capsule.size.y * sy;

            if (_showBorder)
            {
                Handles.color = border;
                var halfW = w / 2f;
                var halfH = h / 2f;
                const int segments = 32;
                var prev = center + new Vector3(halfW * Mathf.Cos(0), halfH * Mathf.Sin(0), 0);
                for (var i = 1; i <= segments; i++)
                {
                    var angle = 2f * Mathf.PI * i / segments;
                    var next = center + new Vector3(halfW * Mathf.Cos(angle), halfH * Mathf.Sin(angle), 0);
                    Handles.DrawLine(prev, next);
                    prev = next;
                }
            }
        }

        private void DrawPolygonCollider2D(PolygonCollider2D polygon, Color fill, Color border)
        {
            var t = polygon.transform;

            for (var p = 0; p < polygon.pathCount; p++)
            {
                var path = polygon.GetPath(p);
                if (path.Length < 2) continue;

                var worldPath = new Vector3[path.Length];
                for (var i = 0; i < path.Length; i++)
                {
                    worldPath[i] = t.TransformPoint(path[i]);
                }

                if (_showFill && path.Length >= 3)
                {
                    Handles.color = fill;
                    Handles.DrawAAConvexPolygon(worldPath);
                }
                if (_showBorder)
                {
                    Handles.color = border;
                    for (var i = 0; i < worldPath.Length; i++)
                    {
                        Handles.DrawLine(worldPath[i], worldPath[(i + 1) % worldPath.Length]);
                    }
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
            {
                var a = t.TransformPoint(points[i]);
                var b = t.TransformPoint(points[i + 1]);
                Handles.DrawLine(a, b);
            }
        }
    }
}
