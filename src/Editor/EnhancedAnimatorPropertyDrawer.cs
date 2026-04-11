using UnityEditor;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// 拡張版Animator用CustomPropertyDrawer
    /// Pingボタンを追加
    /// </summary>
    [CustomPropertyDrawer(typeof(Animator))]
    internal class EnhancedAnimatorPropertyDrawer : PropertyDrawer
    {
        private const float PING_BUTTON_WIDTH = 50f;
        private const float SPACING = 2f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 設定でEnhanced Drawerが無効の場合は通常の描画
            if (!MornUtilEditorPreferences.EnableAnimatorDrawer)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            
            var animator = property.objectReferenceValue as Animator;
            var controller = animator?.runtimeAnimatorController;
            
            // Pingボタン幅を考慮してメインフィールドの幅を調整
            var totalButtonWidth = PING_BUTTON_WIDTH + SPACING;
            var mainFieldRect = new Rect(
                position.x, 
                position.y, 
                position.width - totalButtonWidth, 
                position.height
            );
            
            var pingButtonRect = new Rect(
                position.x + position.width - PING_BUTTON_WIDTH, 
                position.y, 
                PING_BUTTON_WIDTH, 
                position.height
            );
            
            // メインのAnimatorフィールドを描画
            EditorGUI.PropertyField(mainFieldRect, property, label);
            
            // Pingボタンを描画
            DrawPingButton(pingButtonRect, controller);
            
            EditorGUI.EndProperty();
        }
        
        private void DrawPingButton(Rect rect, RuntimeAnimatorController controller)
        {
            EditorGUI.BeginDisabledGroup(controller == null);
            
            if (GUI.Button(rect, "Ping"))
            {
                if (controller != null)
                {
                    EditorGUIUtility.PingObject(controller);
                }
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}