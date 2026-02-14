using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MornLib
{
    public sealed class RaycastTargetVisualizerWindow : EditorWindow
    {
        private static RaycastTargetVisualizerWindow _instance;
        private bool _isVisualizationEnabled;
        private Color _visualizationColor = new(1f, 0f, 0f, 0.3f);
        private Color _borderColor = new(1f, 0f, 0f, 0.8f);
        private float _borderWidth = 2f;
        private bool _showBorder = true;
        private bool _showFill = true;
        private bool _checkCanvasGroup = true;
        private readonly List<Graphic> cachedGraphics = new();
        private float _updateInterval = 0.1f;
        private float _lastUpdateTime;

        [MenuItem("Tools/Raycastターゲット可視化")]
        public static void ShowWindow()
        {
            _instance = GetWindow<RaycastTargetVisualizerWindow>("Raycastターゲット可視化");
            _instance.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            _instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _instance = null;
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
                    // 有効化時は即座にキャッシュを更新
                    _lastUpdateTime = 0f;
                    UpdateCachedGraphics();
                }

                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledGroupScope(!_isVisualizationEnabled))
            {
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

                EditorGUILayout.Space();

                // CanvasGroupチェックオプション
                EditorGUI.BeginChangeCheck();
                _checkCanvasGroup = EditorGUILayout.Toggle("CanvasGroupを考慮", _checkCanvasGroup);
                if (EditorGUI.EndChangeCheck())
                {
                    // 即座に更新を強制
                    _lastUpdateTime = 0f; // 次のOnEditorUpdateで確実に更新される
                    UpdateCachedGraphics();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.HelpBox(
                    "有効時、CanvasGroupの設定（blocksRaycasts=false）でブロックされているGraphicを除外します",
                    MessageType.Info);
                EditorGUILayout.Space();
                _updateInterval = EditorGUILayout.Slider("更新間隔", _updateInterval, 0.01f, 1f);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"キャッシュ済みGraphic数: {cachedGraphics.Count}");

                // 自動更新の状態を表示
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
            // PlayModeが変更されたときにForceUpdateを実行
            if (_isVisualizationEnabled)
            {
                // PlayMode開始、終了、一時停止のいずれの場合も更新
                UpdateCachedGraphics();
                _lastUpdateTime = Time.realtimeSinceStartup;
                SceneView.RepaintAll();
                Repaint();

                // デバッグログ（必要に応じてコメントアウト可能）
                Debug.Log($"RaycastTargetVisualizer: PlayModeが {state} に変更されました。グラフィックスキャッシュを強制更新しました。");
            }
        }

        private void OnEditorUpdate()
        {
            if (!_isVisualizationEnabled) return;

            // Interval毎に自動更新（Playモード中もEditモード中も）
            if (Time.realtimeSinceStartup - _lastUpdateTime > _updateInterval)
            {
                UpdateCachedGraphics();
                _lastUpdateTime = Time.realtimeSinceStartup;
                SceneView.RepaintAll();
                Repaint(); // ウィンドウ自体も更新
            }
        }

        private bool IsGraphicRaycastable(Graphic graphic)
        {
            // Graphic自体のraycastTargetチェック
            if (!graphic.raycastTarget) return false;

            // GameObjectのアクティブチェック
            if (!graphic.gameObject.activeInHierarchy) return false;

            // CanvasGroupチェックがOFFの場合はここで終了
            if (!_checkCanvasGroup) return true;

            // 親階層のCanvasGroup全てをチェック（自分自身も含む）
            CanvasGroup[] canvasGroups = graphic.GetComponentsInParent<CanvasGroup>(true);
            foreach (var canvasGroup in canvasGroups)
            {
                // blocksRaycastsがfalseなら除外（レイキャストを透過する）
                if (!canvasGroup.blocksRaycasts) return false;

                // ignoreParentGroupsがtrueなら、それより上の階層は無視
                if (canvasGroup.ignoreParentGroups)
                {
                    break;
                }
            }

            return true;
        }

        private void UpdateCachedGraphics()
        {
            cachedGraphics.Clear();

            // 全ロード済みシーンのルートGameObjectから再帰的にGraphicを探す
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                    {
                        if (graphic != null && IsGraphicRaycastable(graphic))
                        {
                            cachedGraphics.Add(graphic);
                        }
                    }
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isVisualizationEnabled) return;

            // Cache update is now handled by OnEditorUpdate at regular intervals
            Handles.BeginGUI();
            foreach (var graphic in cachedGraphics)
            {
                if (graphic == null || !graphic.gameObject.activeInHierarchy) continue;
                DrawGraphicVisualization(graphic);
            }

            Handles.EndGUI();
        }

        private void DrawGraphicVisualization(Graphic graphic)
        {
            RectTransform rectTransform = graphic.rectTransform;
            Canvas canvas = graphic.canvas;
            if (rectTransform == null || canvas == null) return;

            // Get world corners of the RectTransform
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            // Convert world positions to GUI positions
            Vector3[] guiCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                guiCorners[i] = HandleUtility.WorldToGUIPoint(worldCorners[i]);
            }

            // Draw fill
            if (_showFill)
            {
                Handles.DrawSolidRectangleWithOutline(guiCorners, _visualizationColor, Color.clear);
            }

            // Draw border
            if (_showBorder)
            {
                Color oldColor = Handles.color;
                Handles.color = _borderColor;

                // Draw thicker lines by drawing multiple times with slight offset
                for (float offset = -_borderWidth / 2; offset <= _borderWidth / 2; offset += 0.5f)
                {
                    Vector3[] offsetCorners = new Vector3[4];
                    for (int i = 0; i < 4; i++)
                    {
                        offsetCorners[i] = guiCorners[i] + Vector3.one * offset;
                    }

                    // Draw rectangle outline
                    for (int i = 0; i < 4; i++)
                    {
                        int nextIndex = (i + 1) % 4;
                        Handles.DrawLine(offsetCorners[i], offsetCorners[nextIndex]);
                    }
                }

                Handles.color = oldColor;
            }

            // Draw label with component name
            Vector2 center = (guiCorners[0] + guiCorners[2]) / 2f;
            GUI.Label(
                new Rect(center.x - 50, center.y - 10, 100, 20),
                graphic.GetType().Name,
                EditorStyles.whiteMiniLabel);
        }
    }
}