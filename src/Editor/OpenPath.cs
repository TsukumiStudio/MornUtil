using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal static class OpenPath
    {
        [MenuItem("Tools/MornUtil/Open Persistent Data Path")]
        private static void OpenPersistentPath()
        {
            var path = Application.persistentDataPath;
            EditorUtility.RevealInFinder(path);
        }
        
        [MenuItem("Tools/MornUtil/Open Data Path")]
        private static void OpenDataPath()
        {
            var path = Application.dataPath;
            EditorUtility.RevealInFinder(path);
        }
        
        [MenuItem("Tools/MornUtil/Open Streaming Assets Path")]
        private static void OpenStreamingAssetsPath()
        {
            var path = Application.streamingAssetsPath;
            EditorUtility.RevealInFinder(path);
        }
        
        [MenuItem("Tools/MornUtil/Open Temporary Cache Path")]
        private static void OpenTemporaryCachePath()
        {
            var path = Application.temporaryCachePath;
            EditorUtility.RevealInFinder(path);
        }
        
        [MenuItem("Tools/MornUtil/Open Console Log Path")]
        private static void OpenConsoleLogPath()
        {
            var path = Application.consoleLogPath;
            EditorUtility.RevealInFinder(path);
        }
    }
}