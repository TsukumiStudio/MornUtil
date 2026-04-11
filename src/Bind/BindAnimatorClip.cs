using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MornLib
{
    [Serializable]
    public class BindAnimatorClip
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _clipName;
        public Animator Animator => _animator;
        public AnimationClip Clip
        {
            get
            {
                if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrEmpty(_clipName))
                {
                    return null;
                }

                var clips = _animator.runtimeAnimatorController.animationClips;
                foreach (var clip in clips)
                {
                    if (clip != null && clip.name == _clipName)
                    {
                        return clip;
                    }
                }

                return null;
            }
        }
        public bool IsValid => Clip != null;

        public BindAnimatorClip()
        {
        }

        public BindAnimatorClip(Animator animator, string clipName = "")
        {
            _animator = animator;
            _clipName = clipName;
        }

        public void Play(float transition = 0f)
        {
            if (_animator == null || string.IsNullOrEmpty(_clipName))
            {
                return;
            }

            var clip = Clip;
            if (clip == null)
            {
                return;
            }

            _animator.enabled = true;
            _animator.MornPlay(clip, transition);
        }

        public UniTask PlayAsync(float transition = 0f, CancellationToken ct = default)
        {
            if (_animator == null || string.IsNullOrEmpty(_clipName))
            {
                return UniTask.CompletedTask;
            }

            var clip = Clip;
            if (clip == null)
            {
                return UniTask.CompletedTask;
            }

            _animator.enabled = true;
            return _animator.MornPlayAsync(clip, transition, ct);
        }

        public void ApplyImmediate(float normalizedTime = 1f)
        {
            if (_animator == null || string.IsNullOrEmpty(_clipName))
            {
                return;
            }

            var clip = Clip;
            if (clip != null)
            {
                _animator.enabled = true;
                _animator.MornApplyImmediate(clip, normalizedTime);
            }
        }
    }
}