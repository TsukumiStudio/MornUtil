using System.Linq;
using UnityEditor;

namespace MornLib
{
    internal static class MornPreloadAssetsMenuItem
    {
        [MenuItem("Tools/PreloadAssetsの最適化")]
        private static void OptimizePreloadAsset()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.RemoveAll(x => x == null);
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }
    }
}