using UnityEditor;
using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// MornUtil Editor設定
    /// </summary>
    internal static class MornUtilEditorPreferences
    {
        private const string ENABLE_ANIMATOR_DRAWER_KEY = "MornUtil.EnableAnimatorDrawer";
        private const string SHOW_ANIMATOR_TOOLTIPS_KEY = "MornUtil.ShowAnimatorTooltips";
        private const string ANIMATOR_BUTTON_COLOR_R_KEY = "MornUtil.AnimatorButtonColor.R";
        private const string ANIMATOR_BUTTON_COLOR_G_KEY = "MornUtil.AnimatorButtonColor.G";
        private const string ANIMATOR_BUTTON_COLOR_B_KEY = "MornUtil.AnimatorButtonColor.B";
        
        public static bool EnableAnimatorDrawer
        {
            get => EditorPrefs.GetBool(ENABLE_ANIMATOR_DRAWER_KEY, true);
            set => EditorPrefs.SetBool(ENABLE_ANIMATOR_DRAWER_KEY, value);
        }
        
        public static bool ShowAnimatorTooltips
        {
            get => EditorPrefs.GetBool(SHOW_ANIMATOR_TOOLTIPS_KEY, true);
            set => EditorPrefs.SetBool(SHOW_ANIMATOR_TOOLTIPS_KEY, value);
        }
        
        public static Color AnimatorButtonColor
        {
            get => new Color(
                EditorPrefs.GetFloat(ANIMATOR_BUTTON_COLOR_R_KEY, 0.7f),
                EditorPrefs.GetFloat(ANIMATOR_BUTTON_COLOR_G_KEY, 0.9f),
                EditorPrefs.GetFloat(ANIMATOR_BUTTON_COLOR_B_KEY, 1f),
                1f
            );
            set
            {
                EditorPrefs.SetFloat(ANIMATOR_BUTTON_COLOR_R_KEY, value.r);
                EditorPrefs.SetFloat(ANIMATOR_BUTTON_COLOR_G_KEY, value.g);
                EditorPrefs.SetFloat(ANIMATOR_BUTTON_COLOR_B_KEY, value.b);
            }
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateMornUtilSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Morn Util", SettingsScope.User)
            {
                label = "Morn Util",
                guiHandler = (searchContext) =>
                {
                    EditorGUILayout.LabelField("Animator Property Drawer", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    
                    var newEnableDrawer = EditorGUILayout.Toggle(
                        new GUIContent("Enable Enhanced Animator Drawer", "Enable the enhanced Animator property drawer with Ping button and context menu"),
                        EnableAnimatorDrawer
                    );
                    if (newEnableDrawer != EnableAnimatorDrawer)
                    {
                        EnableAnimatorDrawer = newEnableDrawer;
                        EditorUtility.RequestScriptReload();
                    }
                    
                    EditorGUI.BeginDisabledGroup(!EnableAnimatorDrawer);
                    
                    ShowAnimatorTooltips = EditorGUILayout.Toggle(
                        new GUIContent("Show Tooltips", "Show detailed tooltips on Animator buttons"),
                        ShowAnimatorTooltips
                    );
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Button Color", EditorStyles.boldLabel);
                    AnimatorButtonColor = EditorGUILayout.ColorField(
                        new GUIContent("Ping Button Color", "Color of the Ping button when controller is available"),
                        AnimatorButtonColor
                    );
                    
                    if (GUILayout.Button("Reset to Defaults"))
                    {
                        EnableAnimatorDrawer = true;
                        ShowAnimatorTooltips = true;
                        AnimatorButtonColor = new Color(0.7f, 0.9f, 1f, 1f);
                    }
                    
                    EditorGUI.EndDisabledGroup();
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "Enhanced Animator Drawer adds a Ping button and context menu to Animator fields in the Inspector. " +
                        "Changes require script reload to take effect.",
                        MessageType.Info
                    );
                },
                keywords = new[] { "Morn", "Util", "Animator", "Drawer", "Ping" }
            };
            
            return provider;
        }
    }
}