#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    internal static class DefineSymbolRegisterMenuItem
    {
        private const string MornFolderPath = "Assets/_Morn";
        private const string SymbolPrefix = "USE_MORN_";

        [MenuItem("Tools/MornUtil/Define Symbolの整理")]
        public static void RegisterDefineSymbols()
        {
            if (!Directory.Exists(MornFolderPath))
            {
                Debug.LogWarning($"{MornFolderPath} フォルダが見つかりません。");
                return;
            }

            // 現在のフォルダから有効なシンボル名のセットを作成
            var directories = Directory.GetDirectories(MornFolderPath);
            var validSymbols = new HashSet<string>();
            foreach (var directory in directories)
            {
                var folderName = Path.GetFileName(directory);
                var symbolName = ConvertToSymbolName(folderName);
                validSymbols.Add(symbolName);
                DefineSymbolRegisterer.Register(symbolName);
            }

            // 存在しないフォルダのシンボルを削除
            DefineSymbolRegisterer.RemoveObsoleteSymbols(SymbolPrefix, validSymbols);

            // 登録後にソートを実行
            EditorApplication.delayCall += () => EditorApplication.delayCall += DefineSymbolRegisterer.SortAllDefineSymbols;
        }

        private static string ConvertToSymbolName(string folderName)
        {
            // MornBeat → USE_MORN_BEAT
            // MornUGUI → USE_MORN_UGUI (連続した大文字はそのまま)
            var converted = string.Concat(folderName.Select((c, i) => i > 0 && char.IsUpper(c) && char.IsLower(folderName[i - 1]) ? "_" + c : c.ToString()));
            return "USE_" + converted.ToUpper();
        }
    }
}
#endif