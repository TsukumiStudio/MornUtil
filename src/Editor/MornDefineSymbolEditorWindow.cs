using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MornLib
{
    /// <summary>Define Symbolを管理するEditorWindow</summary>
    internal sealed class MornDefineSymbolEditorWindow : EditorWindow
    {
        private static readonly Dictionary<BuildTargetGroup, BuildTarget> GroupToTarget = new()
        {
            { BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64 },
            { BuildTargetGroup.iOS, BuildTarget.iOS },
            { BuildTargetGroup.Android, BuildTarget.Android },
            { BuildTargetGroup.WebGL, BuildTarget.WebGL },
            { BuildTargetGroup.WSA, BuildTarget.WSAPlayer },
            { BuildTargetGroup.PS4, BuildTarget.PS4 },
            { BuildTargetGroup.XboxOne, BuildTarget.XboxOne },
            { BuildTargetGroup.tvOS, BuildTarget.tvOS },
            { BuildTargetGroup.Switch, BuildTarget.Switch },
            { BuildTargetGroup.VisionOS, BuildTarget.VisionOS },
        };
        private Vector2 _scrollPosition;
        private string _newSymbolName = "";
        private List<BuildTargetGroup> _validGroups;
        private List<string> _allSymbols;
        private Dictionary<BuildTargetGroup, HashSet<string>> _originalSymbols;
        private Dictionary<BuildTargetGroup, HashSet<string>> _currentSymbols;
        private bool _isDirty;

        [MenuItem("Tools/MornUtil/Define Symbol Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<MornDefineSymbolEditorWindow>();
            window.titleContent = new GUIContent("Define Symbol Editor");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSymbols();
        }

        private void LoadSymbols()
        {
            _validGroups = new List<BuildTargetGroup>();
            _allSymbols = new List<string>();
            _originalSymbols = new Dictionary<BuildTargetGroup, HashSet<string>>();
            _currentSymbols = new Dictionary<BuildTargetGroup, HashSet<string>>();
            foreach (var (group, target) in GroupToTarget)
            {
                if (!BuildPipeline.IsBuildTargetSupported(group, target))
                {
                    continue;
                }

                try
                {
                    var named = NamedBuildTarget.FromBuildTargetGroup(group);
                    var defines = PlayerSettings.GetScriptingDefineSymbols(named).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    _validGroups.Add(group);
                    _originalSymbols[group] = new HashSet<string>(defines);
                    _currentSymbols[group] = new HashSet<string>(defines);
                    foreach (var symbol in defines)
                    {
                        if (!_allSymbols.Contains(symbol))
                        {
                            _allSymbols.Add(symbol);
                        }
                    }
                }
                catch
                {
                    // 無効なBuildTargetGroupは無視
                }
            }

            _allSymbols.Sort();
            _isDirty = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawToolbar();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawSymbolMatrix();
            EditorGUILayout.EndScrollView();
            DrawAddSymbolSection();
            DrawActionButtons();
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadSymbols();
            }

            GUILayout.FlexibleSpace();
            if (_isDirty)
            {
                EditorGUILayout.LabelField("変更あり", EditorStyles.boldLabel, GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSymbolMatrix()
        {
            if (_allSymbols.Count == 0)
            {
                EditorGUILayout.HelpBox("Define Symbolが登録されていません。", MessageType.Info);
                return;
            }

            var platformWidth = 80;

            // ヘッダー行（Platform名）
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Symbol", EditorStyles.boldLabel, GUILayout.Width(200));
            foreach (var group in _validGroups)
            {
                GUILayout.Label(group.ToString(), EditorStyles.miniLabel, GUILayout.Width(platformWidth));
            }

            GUILayout.Label("", GUILayout.Width(30)); // 削除ボタン用スペース
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // 各Symbolの行
            foreach (var symbol in _allSymbols.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(symbol, GUILayout.Width(200));
                foreach (var group in _validGroups)
                {
                    var isEnabled = _currentSymbols[group].Contains(symbol);
                    var newValue = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(platformWidth));
                    if (newValue != isEnabled)
                    {
                        if (newValue)
                        {
                            _currentSymbols[group].Add(symbol);
                        }
                        else
                        {
                            _currentSymbols[group].Remove(symbol);
                        }

                        UpdateDirtyState();
                    }
                }

                // 削除ボタン
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    RemoveSymbol(symbol);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAddSymbolSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("新規シンボル:", GUILayout.Width(80));
            _newSymbolName = EditorGUILayout.TextField(_newSymbolName, GUILayout.Width(200));
            GUI.enabled = !string.IsNullOrWhiteSpace(_newSymbolName) && !_allSymbols.Contains(_newSymbolName.Trim());
            if (GUILayout.Button("追加", GUILayout.Width(60)))
            {
                AddSymbol(_newSymbolName.Trim());
                _newSymbolName = "";
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = _isDirty;
            if (GUILayout.Button("Revert", GUILayout.Height(30), GUILayout.Width(100)))
            {
                LoadSymbols();
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Apply", GUILayout.Height(30), GUILayout.Width(100)))
            {
                ApplyChanges();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void AddSymbol(string symbol)
        {
            if (_allSymbols.Contains(symbol))
            {
                return;
            }

            _allSymbols.Add(symbol);
            _allSymbols.Sort();

            // すべてのPlatformに追加
            foreach (var group in _validGroups)
            {
                _currentSymbols[group].Add(symbol);
            }

            UpdateDirtyState();
        }

        private void RemoveSymbol(string symbol)
        {
            if (!_allSymbols.Contains(symbol))
            {
                return;
            }

            _allSymbols.Remove(symbol);

            // すべてのPlatformから削除
            foreach (var group in _validGroups)
            {
                _currentSymbols[group].Remove(symbol);
            }

            UpdateDirtyState();
        }

        private void UpdateDirtyState()
        {
            _isDirty = false;
            foreach (var group in _validGroups)
            {
                if (!_originalSymbols[group].SetEquals(_currentSymbols[group]))
                {
                    _isDirty = true;
                    break;
                }
            }
        }

        private void ApplyChanges()
        {
            foreach (var group in _validGroups)
            {
                try
                {
                    var named = NamedBuildTarget.FromBuildTargetGroup(group);
                    var sortedSymbols = _currentSymbols[group].OrderBy(s => s).ToList();
                    PlayerSettings.SetScriptingDefineSymbols(named, string.Join(";", sortedSymbols));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to apply define symbols for {group}: {e.Message}");
                }
            }

            Debug.Log("Define Symbolsを適用しました。");
            LoadSymbols();
            EditorUtility.RequestScriptReload();
        }
    }
}