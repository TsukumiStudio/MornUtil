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
        private const string PrefPrefix = "MornRaycastViz_";

        private static RaycastTargetVisualizerWindow _instance;
        private bool _isVisualizationEnabled;
        private Color _visualizationColor = new(1f, 0f, 0f, 0.3f);
        private Color _borderColor = new(1f, 0f, 0f, 0.8f);
        private float _borderWidth = 2f;
        private bool _showBorder = true;
        private bool _showFill = true;
        private bool _checkCanvasGroup = true;
        private int _labelFontSize = 10;
        private readonly List<Graphic> cachedGraphics = new();
        private float _updateInterval = 0.1f;
        private float _lastUpdateTime;
        private GUIStyle _labelStyle;

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
            _isVisualizationEnabled = EditorPrefs.GetBool(PrefPrefix + "Enabled", false);
            _showFill = EditorPrefs.GetBool(PrefPrefix + "ShowFill", true);
            _showBorder = EditorPrefs.GetBool(PrefPrefix + "ShowBorder", true);
            _checkCanvasGroup = EditorPrefs.GetBool(PrefPrefix + "CheckCanvasGroup", true);
            _borderWidth = EditorPrefs.GetFloat(PrefPrefix + "BorderWidth", 2f);
            _updateInterval = EditorPrefs.GetFloat(PrefPrefix + "UpdateInterval", 0.1f);
            _labelFontSize = EditorPrefs.GetInt(PrefPrefix + "LabelFontSize", 10);

            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "FillColor", ""), out var fc))
                _visualizationColor = fc;
            if (ColorUtility.TryParseHtmlString(EditorPrefs.GetString(PrefPrefix + "BorderColor", ""), out var bc))
                _borderColor = bc;
        }

        private void SavePrefs()
        {
            EditorPrefs.SetBool(PrefPrefix + "Enabled", _isVisualizationEnabled);
            EditorPrefs.SetBool(PrefPrefix + "ShowFill", _showFill);
            EditorPrefs.SetBool(PrefPrefix + "ShowBorder", _showBorder);
            EditorPrefs.SetBool(PrefPrefix + "CheckCanvasGroup", _checkCanvasGroup);
            EditorPrefs.SetFloat(PrefPrefix + "BorderWidth", _borderWidth);
            EditorPrefs.SetFloat(PrefPrefix + "UpdateInterval", _updateInterval);
            EditorPrefs.SetInt(PrefPrefix + "LabelFontSize", _labelFontSize);
            EditorPrefs.SetString(PrefPrefix + "FillColor", "#" + ColorUtility.ToHtmlStringRGBA(_visualizationColor));
            EditorPrefs.SetString(PrefPrefix + "BorderColor", "#" + ColorUtility.ToHtmlStringRGBA(_borderColor));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Raycastターゲット可視化", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            _isVisualizationEnabled = EditorGUILayout.Toggle("可視化を有効にする", _isVisualizationEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                if (_isVisualizationEnabled)
                {
                    _lastUpdateTime = 0f;
                    UpdateCachedGraphics();
                }

                SavePrefs();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledGroupScope(!_isVisualizationEnabled))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("表示設定", EditorStyles.boldLabel);
                _showFill = EditorGUILayout.Toggle("塗りつぶし表示", _showFill);
                if (_showFill)
                {
                    _visualizationColor = EditorGUILayout.ColorField("塗りつぶし色", _visualizationColor);
                }

                _showBorder = EditorGUILayout.Toggle("枠線表示", _showBorder);
                if (_showBorder)
                {
                    _borderColor = EditorGUILayout.ColorField("枠線色", _borderColor);
                    _borderWidth = EditorGUILayout.Slider("枠線の太さ", _borderWidth, 1f, 10f);
                }

                _labelFontSize = EditorGUILayout.IntSlider("文字サイズ", _labelFontSize, 6, 24);

                EditorGUILayout.Space();

                var prevCheckCanvasGroup = _checkCanvasGroup;
                _checkCanvasGroup = EditorGUILayout.Toggle("CanvasGroupを考慮", _checkCanvasGroup);
                if (_checkCanvasGroup != prevCheckCanvasGroup)
                {
                    _lastUpdateTime = 0f;
                    UpdateCachedGraphics();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.HelpBox(
                    "有効時、CanvasGroupの設定（blocksRaycasts=false）でブロックされているGraphicを除外します",
                    MessageType.Info);
                EditorGUILayout.Space();
                _updateInterval = EditorGUILayout.Slider("更新間隔", _updateInterval, 0.01f, 1f);

                if (EditorGUI.EndChangeCheck())
                {
                    _labelStyle = null;
                    SavePrefs();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"キャッシュ済みGraphic数: {cachedGraphics.Count}");

                if (_isVisualizationEnabled)
                {
                    EditorGUILayout.LabelField($"{_updateInterval:F2}秒ごとに自動更新中", EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"最終更新: {Time.realtimeSinceStartup - _lastUpdateTime:F2}秒前");
                    EditorGUILayout.LabelField(
                        $"モード: {(EditorApplication.isPlaying ? "再生中" : "編集中")}",
                        EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("自動更新無効（可視化OFF）", EditorStyles.helpBox);
                }

                if (GUILayout.Button("強制更新"))
                {
                    UpdateCachedGraphics();
                    SceneView.RepaintAll();
                }
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (_isVisualizationEnabled)
            {
                UpdateCachedGraphics();
                _lastUpdateTime = Time.realtimeSinceStartup;
                SceneView.RepaintAll();
                Repaint();
            }
        }

        private void OnEditorUpdate()
        {
            if (!_isVisualizationEnabled) return;

            if (Time.realtimeSinceStartup - _lastUpdateTime > _updateInterval)
            {
                UpdateCachedGraphics();
                _lastUpdateTime = Time.realtimeSinceStartup;
                SceneView.RepaintAll();
                Repaint();
            }
        }

        private bool IsGraphicRaycastable(Graphic graphic)
        {
            if (!graphic.raycastTarget) return false;
            if (!graphic.gameObject.activeInHierarchy) return false;
            if (!_checkCanvasGroup) return true;

            CanvasGroup[] canvasGroups = graphic.GetComponentsInParent<CanvasGroup>(true);
            foreach (var canvasGroup in canvasGroups)
            {
                if (!canvasGroup.blocksRaycasts) return false;
                if (canvasGroup.ignoreParentGroups) break;
            }

            return true;
        }

        private void UpdateCachedGraphics()
        {
            cachedGraphics.Clear();

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                CollectGraphics(prefabStage.prefabContentsRoot);
                return;
            }

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    CollectGraphics(root);
                }
            }
        }

        private void CollectGraphics(GameObject root)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != null && IsGraphicRaycastable(graphic))
                {
                    cachedGraphics.Add(graphic);
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isVisualizationEnabled) return;

            Handles.BeginGUI();
            foreach (var graphic in cachedGraphics)
            {
                if (graphic == null || !graphic.gameObject.activeInHierarchy) continue;
                DrawGraphicVisualization(graphic);
            }

            Handles.EndGUI();
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
            RectTransform rectTransform = graphic.rectTransform;
            Canvas canvas = graphic.canvas;
            if (rectTransform == null || canvas == null) return;

            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            Vector3[] guiCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                guiCorners[i] = HandleUtility.WorldToGUIPoint(worldCorners[i]);
            }

            if (_showFill)
            {
                Handles.DrawSolidRectangleWithOutline(guiCorners, _visualizationColor, Color.clear);
            }

            if (_showBorder)
            {
                Color oldColor = Handles.color;
                Handles.color = _borderColor;

                for (float offset = -_borderWidth / 2; offset <= _borderWidth / 2; offset += 0.5f)
                {
                    Vector3[] offsetCorners = new Vector3[4];
                    for (int i = 0; i < 4; i++)
                    {
                        offsetCorners[i] = guiCorners[i] + Vector3.one * offset;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        int nextIndex = (i + 1) % 4;
                        Handles.DrawLine(offsetCorners[i], offsetCorners[nextIndex]);
                    }
                }

                Handles.color = oldColor;
            }

            var style = GetLabelStyle();
            Vector2 center = (guiCorners[0] + guiCorners[2]) / 2f;
            var labelWidth = _labelFontSize * 10f;
            var labelHeight = _labelFontSize + 4f;
            GUI.Label(
                new Rect(center.x - labelWidth / 2f, center.y - labelHeight / 2f, labelWidth, labelHeight),
                graphic.GetType().Name,
                style);
        }
    }
}
