using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// BindAnimatorState用のCustomPropertyDrawer
    /// </summary>
    [CustomPropertyDrawer(typeof(BindAnimatorState))]
    internal class BindAnimatorStatePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var animatorProp = property.FindPropertyRelative("_animator");
            var stateNameProp = property.FindPropertyRelative("_stateName");
            
            // ラベルを描画
            var rect = EditorGUI.PrefixLabel(position, label);
            
            // インデントをリセット
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            // AnimatorとStateを1行で表示
            var animatorWidth = rect.width * 0.5f;
            var stateWidth = rect.width * 0.5f;
            
            // Animatorフィールド
            var animatorRect = new Rect(rect.x, rect.y, animatorWidth, rect.height);
            var newAnimator = EditorGUI.ObjectField(animatorRect, animatorProp.objectReferenceValue, typeof(Animator), true) as Animator;
            
            if (newAnimator != animatorProp.objectReferenceValue)
            {
                animatorProp.objectReferenceValue = newAnimator;
                stateNameProp.stringValue = string.Empty; // Animatorが変更されたらState名をリセット
            }
            
            // State選択フィールド
            var stateRect = new Rect(rect.x + animatorWidth, rect.y, stateWidth, rect.height);
            DrawStateSelector(stateRect, animatorProp.objectReferenceValue as Animator, stateNameProp);
            
            // インデントを復元
            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }
        
        private void DrawStateSelector(Rect rect, Animator animator, SerializedProperty stateNameProp)
        {
            if (animator?.runtimeAnimatorController == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(rect, 0, new string[] { "Animatorを設定" });
                EditorGUI.EndDisabledGroup();
                return;
            }
            
            var stateNames = GetStateNames(animator);
            if (stateNames.Count == 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(rect, 0, new string[] { "Stateなし" });
                EditorGUI.EndDisabledGroup();
                return;
            }
            
            var currentStateName = stateNameProp.stringValue;
            var currentIndex = stateNames.IndexOf(currentStateName);
            
            // 現在の選択が無効な場合は0にリセット
            if (currentIndex < 0)
            {
                currentIndex = 0;
                stateNameProp.stringValue = stateNames[0];
            }
            
            var newIndex = EditorGUI.Popup(rect, currentIndex, stateNames.ToArray());
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < stateNames.Count)
            {
                stateNameProp.stringValue = stateNames[newIndex];
            }
        }
        
        private List<string> GetStateNames(Animator animator)
        {
            var stateNames = new List<string>();
            
            if (animator?.runtimeAnimatorController == null)
                return stateNames;

            // エディタではAnimatorControllerからState名を取得
            if (animator.runtimeAnimatorController is AnimatorController controller)
            {
                for (var i = 0; i < controller.layers.Length; i++)
                {
                    CollectStateNames(controller.layers[i].stateMachine, stateNames);
                }
            }
            else
            {
                // ランタイムまたはAnimatorOverrideControllerの場合はクリップ名をState名として使用
                var allClips = animator.runtimeAnimatorController.animationClips;
                if (allClips != null)
                {
                    var uniqueNames = allClips.Where(c => c != null).Select(c => c.name).Distinct();
                    stateNames.AddRange(uniqueNames);
                }
            }
            
            return stateNames.OrderBy(n => n).ToList();
        }

        private void CollectStateNames(AnimatorStateMachine stateMachine, List<string> stateNames)
        {
            // ステート名を収集
            foreach (var state in stateMachine.states)
            {
                if (!stateNames.Contains(state.state.name))
                {
                    stateNames.Add(state.state.name);
                }
            }
            
            // サブステートマシンを再帰的に処理
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                CollectStateNames(childStateMachine.stateMachine, stateNames);
            }
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}