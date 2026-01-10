using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// Animator と AnimationClip を結合・分離するEditorWindow
    /// </summary>
    internal sealed class AnimatorAnimationClipBinder : EditorWindow
    {
        private RuntimeAnimatorController _targetController;
        private AnimationClip _clipToBind;
        private Vector2 _scrollPosition;
        private Vector2 _scrollPositionReferenced;
        private bool _showBoundClips = true;
        private bool _showReferencedClips = true;
        
        [MenuItem("Tools/MornUtil/アニメーション結合ツール")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimatorAnimationClipBinder>("アニメーション結合ツール");
            window.Show();
        }
        
        /// <summary>
        /// コンテキストメニューから呼び出される際にターゲットのAnimatorを設定
        /// </summary>
        public void SetTargetAnimator(Animator animator)
        {
            _targetController = animator?.runtimeAnimatorController;
        }
        
        /// <summary>
        /// コンテキストメニューから呼び出される際にターゲットのコントローラーを設定
        /// </summary>
        public void SetTargetController(RuntimeAnimatorController controller)
        {
            _targetController = controller;
        }
        
        /// <summary>
        /// コンテキストメニューから呼び出される際にバインドするクリップを設定
        /// </summary>
        public void SetClipToBind(AnimationClip clip)
        {
            _clipToBind = clip;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("アニメーション結合ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawAnimatorSection();
            EditorGUILayout.Space();
            
            DrawBindingSection();
            EditorGUILayout.Space();
            
            DrawClipListSection();
        }
        
        private void DrawAnimatorSection()
        {
            EditorGUILayout.LabelField("対象コントローラー", EditorStyles.boldLabel);
            
            var newController = EditorGUILayout.ObjectField("Animator Controller", _targetController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;
            if (newController != _targetController)
            {
                _targetController = newController;
            }
            
            if (_targetController == null)
            {
                EditorGUILayout.HelpBox("AnimatorControllerを選択してください。", MessageType.Info);
                return;
            }
        }
        
        private void DrawBindingSection()
        {
            EditorGUILayout.LabelField("アニメーションクリップの結合", EditorStyles.boldLabel);
            
            _clipToBind = EditorGUILayout.ObjectField("結合するアニメーションクリップ", _clipToBind, typeof(AnimationClip), false) as AnimationClip;
            
            EditorGUI.BeginDisabledGroup(_clipToBind == null || _targetController == null);
            if (GUILayout.Button("コントローラーに結合"))
            {
                BindClipToController();
            }
            EditorGUI.EndDisabledGroup();
            
            if (_clipToBind != null && _targetController != null)
            {
                if (IsClipBoundToController(_clipToBind))
                {
                    EditorGUILayout.HelpBox("このクリップは既にコントローラーに結合されています。", MessageType.Warning);
                }
            }
        }
        
        private void DrawClipListSection()
        {
            if (_targetController == null) return;
            
            EditorGUILayout.LabelField("アニメーションクリップ一覧", EditorStyles.boldLabel);
            
            _showBoundClips = EditorGUILayout.Foldout(_showBoundClips, "結合済みクリップ (サブアセット)");
            if (_showBoundClips)
            {
                DrawBoundClips();
            }
            
            EditorGUILayout.Space();
            
            _showReferencedClips = EditorGUILayout.Foldout(_showReferencedClips, "コントローラー参照クリップ (Bindボタン付き)");
            if (_showReferencedClips)
            {
                DrawReferencedClips();
            }
        }
        
        private void DrawBoundClips()
        {
            var boundClips = GetBoundClips();
            
            if (boundClips.Count == 0)
            {
                EditorGUILayout.HelpBox("サブアセットとして結合されているクリップはありません。", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical("box");
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(200));
            
            foreach (var clip in boundClips)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                EditorGUI.EndDisabledGroup();
                
                if (GUILayout.Button("分離", GUILayout.Width(60)))
                {
                    UnbindClipFromController(clip);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawReferencedClips()
        {
            var referencedClips = GetReferencedClips();
            var boundClips = GetBoundClips();
            var unboundClips = referencedClips.Where(clip => !boundClips.Contains(clip)).ToList();
            
            if (unboundClips.Count == 0)
            {
                EditorGUILayout.HelpBox("コントローラーが参照している未結合のクリップはありません。", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical("box");
            _scrollPositionReferenced = EditorGUILayout.BeginScrollView(_scrollPositionReferenced, GUILayout.MaxHeight(200));
            
            foreach (var clip in unboundClips)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                EditorGUI.EndDisabledGroup();
                
                if (GUILayout.Button("結合", GUILayout.Width(60)))
                {
                    BindClipToController(clip);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        
        private void BindClipToController(AnimationClip clip = null)
        {
            if (_targetController == null) return;
            
            var clipToUse = clip ?? _clipToBind;
            if (clipToUse == null) return;
            
            if (IsClipBoundToController(clipToUse))
            {
                Debug.LogWarning($"クリップ '{clipToUse.name}' は既にコントローラーに結合されています。");
                return;
            }
            
            // 元のクリップが既にSubAssetかどうかチェック
            var originalPath = AssetDatabase.GetAssetPath(clipToUse);
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(originalPath);
            bool isAlreadySubAsset = mainAsset != clipToUse;
            
            // 確認ダイアログを表示
            var message = isAlreadySubAsset 
                ? $"'{clipToUse.name}' を '{mainAsset.name}' から '{_targetController.name}' に移動しますか？"
                : $"'{clipToUse.name}' を '{_targetController.name}' に結合しますか？";
                
            bool shouldProceed = EditorUtility.DisplayDialog(
                "アニメーションクリップの結合", 
                message,
                "結合", 
                "キャンセル");
                
            if (!shouldProceed) return;
            
            try
            {
                // アンドゥ操作を記録
                Undo.RegisterCompleteObjectUndo(_targetController, "Bind Animation Clip");
                if (isAlreadySubAsset && mainAsset != null)
                {
                    Undo.RegisterCompleteObjectUndo(mainAsset, "Remove Animation Clip");
                }
                
                AnimationClip resultClip = null;
                
                if (!isAlreadySubAsset && !string.IsNullOrEmpty(originalPath))
                {
                    // 独立したアセットファイルの場合、コピーして結合
                    var newClip = Object.Instantiate(clipToUse);
                    newClip.name = clipToUse.name;
                    
                    // SubAssetとして追加
                    AssetDatabase.AddObjectToAsset(newClip, _targetController);
                    
                    // 元のアセットファイルを削除
                    AssetDatabase.DeleteAsset(originalPath);
                    
                    resultClip = newClip;
                }
                else if (isAlreadySubAsset)
                {
                    // 既にSubAssetの場合は先に削除してから追加
                    AssetDatabase.RemoveObjectFromAsset(clipToUse);
                    AssetDatabase.AddObjectToAsset(clipToUse, _targetController);
                    resultClip = clipToUse;
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                var clipName = resultClip != null ? resultClip.name : "クリップ";
                Debug.Log($"'{clipName}' を '{_targetController.name}' に正常に結合しました");
                
                // バインド後にクリップ選択をクリア
                if (clip == null)
                {
                    _clipToBind = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"クリップの結合に失敗しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", $"クリップの結合に失敗しました: {ex.Message}", "OK");
            }
        }
        
        private void UnbindClipFromController(AnimationClip clip)
        {
            if (_targetController == null || clip == null) return;
            
            if (!IsClipBoundToController(clip))
            {
                Debug.LogWarning($"クリップ '{clip.name}' はコントローラーに結合されていません。");
                return;
            }
            
            bool shouldProceed = EditorUtility.DisplayDialog(
                "アニメーションクリップの分離", 
                $"'{clip.name}' を '{_targetController.name}' から分離しますか？\\n\\nクリップ用の独立した .anim ファイルが作成されます。", 
                "分離", 
                "キャンセル");
                
            if (!shouldProceed) return;
            
            try
            {
                // アンドゥ操作を記録
                Undo.RegisterCompleteObjectUndo(_targetController, "Unbind Animation Clip");
                
                // コントローラーと同じディレクトリに新しいクリップファイルを作成
                var controllerPath = AssetDatabase.GetAssetPath(_targetController);
                var directory = System.IO.Path.GetDirectoryName(controllerPath);
                var newClipPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{clip.name}.anim");
                
                // SubAssetから削除
                AssetDatabase.RemoveObjectFromAsset(clip);
                
                // 独立したアセットファイルとして作成
                AssetDatabase.CreateAsset(clip, newClipPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // 新しく作成されたアセットをハイライト
                var newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newClipPath);
                EditorGUIUtility.PingObject(newClip);
                
                Debug.Log($"'{clip.name}' を '{_targetController.name}' から正常に分離し、'{newClipPath}' を作成しました");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"クリップの分離に失敗しました: {ex.Message}");
                EditorUtility.DisplayDialog("エラー", $"クリップの分離に失敗しました: {ex.Message}", "OK");
            }
        }
        
        private List<AnimationClip> GetBoundClips()
        {
            if (_targetController == null) return new List<AnimationClip>();
            
            var controllerPath = AssetDatabase.GetAssetPath(_targetController);
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(controllerPath);
            
            return subAssets.OfType<AnimationClip>().ToList();
        }
        
        private List<AnimationClip> GetReferencedClips()
        {
            if (_targetController == null) return new List<AnimationClip>();
            
            var clips = new List<AnimationClip>();
            
            // AnimatorControllerからすべての参照されているAnimationClipを取得
            if (_targetController is AnimatorController controller)
            {
                for (int i = 0; i < controller.layers.Length; i++)
                {
                    var stateMachine = controller.layers[i].stateMachine;
                    CollectClipsFromStateMachine(stateMachine, clips);
                }
            }
            else if (_targetController is AnimatorOverrideController overrideController)
            {
                foreach (var clipPair in overrideController.clips)
                {
                    if (clipPair.overrideClip != null && !clips.Contains(clipPair.overrideClip))
                    {
                        clips.Add(clipPair.overrideClip);
                    }
                }
            }
            
            return clips.Distinct().ToList();
        }
        
        private void CollectClipsFromStateMachine(AnimatorStateMachine stateMachine, List<AnimationClip> clips)
        {
            // ステートからクリップを収集
            foreach (var state in stateMachine.states)
            {
                if (state.state.motion is AnimationClip clip && !clips.Contains(clip))
                {
                    clips.Add(clip);
                }
                else if (state.state.motion is BlendTree blendTree)
                {
                    CollectClipsFromBlendTree(blendTree, clips);
                }
            }
            
            // サブステートマシンを再帰的に処理
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                CollectClipsFromStateMachine(childStateMachine.stateMachine, clips);
            }
        }
        
        private void CollectClipsFromBlendTree(BlendTree blendTree, List<AnimationClip> clips)
        {
            for (int i = 0; i < blendTree.children.Length; i++)
            {
                var child = blendTree.children[i];
                if (child.motion is AnimationClip clip && !clips.Contains(clip))
                {
                    clips.Add(clip);
                }
                else if (child.motion is BlendTree childBlendTree)
                {
                    CollectClipsFromBlendTree(childBlendTree, clips);
                }
            }
        }
        
        private bool IsClipBoundToController(AnimationClip clip)
        {
            return GetBoundClips().Contains(clip);
        }
        
    }
}