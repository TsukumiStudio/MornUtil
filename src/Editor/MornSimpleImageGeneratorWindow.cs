using System.IO;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal sealed class MornSimpleImageGeneratorWindow : EditorWindow
    {
        private enum GenerateMode
        {
            SolidColor,
            Gradient
        }
        
        private GenerateMode _mode = GenerateMode.SolidColor;
        private Color _fillColor = Color.white;
        
        // グラデーション用
        private Gradient _gradient;
        private bool _isHorizontalGradient = true;
        
        private int _width = 512;
        private int _height = 512;
        private string _fileName = "GeneratedImage";
        private string _savePath = "";

        [MenuItem("Tools/MornUtil/Simple Image Generator")]
        private static void Open()
        {
            var window = GetWindow<MornSimpleImageGeneratorWindow>();
            window.titleContent = new GUIContent("Simple Image Generator");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnEnable()
        {
            // グラデーションの初期化
            if (_gradient == null)
            {
                _gradient = new Gradient();
                _gradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(Color.white, 0.0f), 
                        new GradientColorKey(Color.black, 1.0f) 
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(1.0f, 0.0f), 
                        new GradientAlphaKey(1.0f, 1.0f) 
                    }
                );
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("簡単な画像生成ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // モード選択
            _mode = (GenerateMode)EditorGUILayout.EnumPopup("生成モード", _mode);
            EditorGUILayout.Space();
            
            // モードによって表示を切り替え
            switch (_mode)
            {
                case GenerateMode.SolidColor:
                    // 単色の設定
                    _fillColor = EditorGUILayout.ColorField("塗りつぶす色", _fillColor);
                    break;
                    
                case GenerateMode.Gradient:
                    // グラデーションの設定
                    _gradient = EditorGUILayout.GradientField("グラデーション", _gradient);
                    _isHorizontalGradient = EditorGUILayout.Toggle("横方向グラデーション", _isHorizontalGradient);
                    break;
            }
            EditorGUILayout.Space();

            // サイズの設定
            EditorGUILayout.LabelField("画像サイズ", EditorStyles.boldLabel);
            _width = EditorGUILayout.IntField("幅 (Width)", _width);
            _height = EditorGUILayout.IntField("高さ (Height)", _height);

            // サイズの制限
            _width = Mathf.Clamp(_width, 1, 4096);
            _height = Mathf.Clamp(_height, 1, 4096);

            EditorGUILayout.Space();

            // ファイル名
            _fileName = EditorGUILayout.TextField("ファイル名", _fileName);

            EditorGUILayout.Space();

            // 保存パスの選択
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("保存先: " + (_savePath == "" ? "未選択" : _savePath));
            if (GUILayout.Button("選択", GUILayout.Width(60)))
            {
                var selectedPath = EditorUtility.SaveFilePanel(
                    "画像の保存先を選択",
                    Application.dataPath,
                    _fileName,
                    "png"
                );

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _savePath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 生成ボタン
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_savePath));
            if (GUILayout.Button("画像を生成", GUILayout.Height(30)))
            {
                GenerateImage();
            }
            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrEmpty(_savePath))
            {
                EditorGUILayout.HelpBox("保存先を選択してください", MessageType.Info);
            }
        }

        private void GenerateImage()
        {
            // テクスチャの作成
            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            
            // ピクセル配列の準備
            var pixels = new Color[_width * _height];
            
            switch (_mode)
            {
                case GenerateMode.SolidColor:
                    // 単色で塗りつぶす
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = _fillColor;
                    }
                    break;
                    
                case GenerateMode.Gradient:
                    // グラデーションで塗りつぶす
                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            float t;
                            if (_isHorizontalGradient)
                            {
                                // 横方向のグラデーション
                                t = (float)x / (_width - 1);
                            }
                            else
                            {
                                // 縦方向のグラデーション
                                t = (float)y / (_height - 1);
                            }
                            
                            // グラデーションから色を計算
                            Color pixelColor = _gradient.Evaluate(t);
                            pixels[y * _width + x] = pixelColor;
                        }
                    }
                    break;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();

            // PNGにエンコード
            byte[] pngData = texture.EncodeToPNG();

            // ファイルに保存
            try
            {
                File.WriteAllBytes(_savePath, pngData);
                
                // Unityプロジェクト内の場合、アセットをリフレッシュ
                if (_savePath.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }

                Debug.Log($"画像を生成しました: {_savePath}");
                EditorUtility.DisplayDialog("成功", $"画像を生成しました。\n{_savePath}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"画像の保存に失敗しました: {e.Message}");
                EditorUtility.DisplayDialog("エラー", $"画像の保存に失敗しました。\n{e.Message}", "OK");
            }
            finally
            {
                // テクスチャのクリーンアップ
                DestroyImmediate(texture);
            }
        }
    }
}