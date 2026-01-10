using UnityEditor;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// Animator/AnimationClipバインダーのコンテキストメニュー
    /// </summary>
    internal static class AnimatorAnimationClipBinderContextMenu
    {
        [MenuItem("CONTEXT/RuntimeAnimatorController/アニメーション結合ツールを開く")]
        private static void OpenClipBinderFromController(MenuCommand command)
        {
            var controller = command.context as RuntimeAnimatorController;
            if (controller == null) return;
            
            var window = EditorWindow.GetWindow<AnimatorAnimationClipBinder>("アニメーション結合ツール");
            window.SetTargetController(controller);
            window.Show();
        }
        
        [MenuItem("CONTEXT/Animator/アニメーション結合ツールを開く")]
        private static void OpenClipBinderFromAnimator(MenuCommand command)
        {
            var animator = command.context as Animator;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            
            var window = EditorWindow.GetWindow<AnimatorAnimationClipBinder>("アニメーション結合ツール");
            window.SetTargetAnimator(animator);
            window.Show();
        }
        
        [MenuItem("Assets/Animatorコントローラーに結合", true)]
        private static bool ValidateBindToAnimatorController()
        {
            return Selection.activeObject is AnimationClip;
        }
        
        [MenuItem("Assets/Animatorコントローラーに結合")]
        private static void BindToAnimatorController()
        {
            var clip = Selection.activeObject as AnimationClip;
            if (clip == null) return;
            
            var window = EditorWindow.GetWindow<AnimatorAnimationClipBinder>("アニメーション結合ツール");
            window.SetClipToBind(clip);
            window.Show();
        }
    }
}