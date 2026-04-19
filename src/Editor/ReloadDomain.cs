using UnityEditor;

namespace MornLib
{
    internal static class ReloadDomain
    {
        [MenuItem("Tools/Reload Domain %#r")]
        private static void Execute()
        {
            EditorUtility.RequestScriptReload();
        }
    }
}
