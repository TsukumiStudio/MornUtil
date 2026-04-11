using UnityEditor;
using UnityEngine;

namespace MornSoundProcessor
{
    internal sealed class MornSoundProcessorWindow : EditorWindow
    {
        private static Editor s_editor;

        [MenuItem("MornLib/MornSoundProcessor")]
        private static void Open()
        {
            Init();
        }

        private static void Init()
        {
            var instance = MornSoundProcessorSettings.instance;
            instance.Init();
            instance.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
            Editor.CreateCachedEditor(instance, null, ref s_editor);
        }

        private void OnGUI()
        {
            if (s_editor == null)
            {
                Init();
            }

            EditorGUI.BeginChangeCheck();
            s_editor.OnInspectorGUI();

            var instance = MornSoundProcessorSettings.instance;
            if ((instance.UseCutBeginningSilence || instance.UseNormalizeAmplitude) && GUILayout.Button("Generate"))
            {
                ExecuteGenerate(instance);
            }
        }

        private static void ExecuteGenerate(MornSoundProcessorSettings instance)
        {
            var length = instance.ClipList.Count;
            instance.ClearResult();

            for (var i = 0; i < length; i++)
            {
                var clip = instance.ClipList[i];
                EditorUtility.DisplayProgressBar("変換中", clip.name, i * 1f / length);
                var convertedClip = MornSoundProcessorUtil.ConvertClip(clip);
                var savedClip = MornSoundProcessorUtil.SaveClip(convertedClip);
                instance.AddResult(savedClip);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"{length}件の変換が終わりました");
        }
    }
}
