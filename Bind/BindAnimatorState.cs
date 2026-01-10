using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor.Animations;
using UnityEngine;

namespace MornLib
{
    [Serializable]
    public class BindAnimatorState
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _stateName;
        public Animator Animator => _animator;
        public string StateName => _stateName;
        /// <summary>
        /// 指定されたStateに関連付けられたAnimationClipを取得する
        /// </summary>
        public AnimationClip Clip
        {
            get
            {
                if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(_stateName))
                {
                    return null;
                }

#if UNITY_EDITOR
                // エディタではAnimatorControllerからState名で逆引き
                if (_animator.runtimeAnimatorController is AnimatorController controller)
                {
                    for (var i = 0; i < controller.layers.Length; i++)
                    {
                        var clip = FindClipInStateMachine(controller.layers[i].stateMachine, _stateName);
                        if (clip != null)
                        {
                            return clip;
                        }
                    }
                }
#endif

                // ランタイムでは現在のStateInfoを使用してClipを特定する
                // （ただし、実行時にStateとClipの関連を直接取得する方法は限定的）
                // AnimatorOverrideControllerの場合は、オーバーライド情報から取得を試みる
                if (_animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                {
                    // オーバーライドされたクリップから、State名に対応するものを探す
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideController.GetOverrides(overrides);

                    // State名と一致するキー（元のクリップ名）を持つオーバーライドを探す
                    foreach (var pair in overrides)
                    {
                        if (pair.Key != null && pair.Key.name == _stateName && pair.Value != null)
                        {
                            return pair.Value;
                        }
                    }
                }

                // フォールバック: State名と同じ名前のClipを探す
                var clips = _animator.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    if (clip != null && clip.name == _stateName)
                    {
                        return clip;
                    }
                }

                return null;
            }
        }
#if UNITY_EDITOR
        private AnimationClip FindClipInStateMachine(AnimatorStateMachine stateMachine, string stateName)
        {
            // ステートを検索
            foreach (var state in stateMachine.states)
            {
                if (state.state.name == stateName && state.state.motion is AnimationClip clip)
                {
                    return clip;
                }
            }

            // サブステートマシンを再帰的に検索
            foreach (var childStateMachine in stateMachine.stateMachines)
            {
                var clip = FindClipInStateMachine(childStateMachine.stateMachine, stateName);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }
#endif
        public bool IsValid => !string.IsNullOrEmpty(_stateName) && _animator != null && _animator.runtimeAnimatorController != null;

        public BindAnimatorState()
        {
        }

        public BindAnimatorState(Animator animator, string stateName = "")
        {
            _animator = animator;
            _stateName = stateName;
        }

        /// <summary>
        /// State名を指定してアニメーションを再生する
        /// </summary>
        public void Play(float transition = 0f)
        {
            if (_animator == null || string.IsNullOrEmpty(_stateName))
            {
                return;
            }

            _animator.enabled = true;
            _animator.MornPlayState(_stateName, transition);
        }

        /// <summary>
        /// State名を指定してアニメーションを非同期で再生する
        /// </summary>
        public UniTask PlayAsync(float transition = 0f, CancellationToken ct = default)
        {
            if (_animator == null || string.IsNullOrEmpty(_stateName))
            {
                return UniTask.CompletedTask;
            }

            _animator.enabled = true;
            return _animator.MornPlayStateAsync(_stateName, transition, ct);
        }

        /// <summary>
        /// State名を指定してアニメーションを即座に適用する
        /// </summary>
        public void ApplyImmediate(float normalizedTime = 1f)
        {
            if (_animator == null || string.IsNullOrEmpty(_stateName))
            {
                return;
            }

            _animator.enabled = true;
            _animator.MornApplyStateImmediate(_stateName, normalizedTime);
        }
    }
}