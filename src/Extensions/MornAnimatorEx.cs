using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MornLib
{
    public static class MornAnimatorEx
    {
        // AnimationClipを指定して再生
        public static void MornPlay(this Animator animator, AnimationClip clip, float transition = 0)
        {
            if (clip == null) return;
            MornPlay(animator, clip.name, transition);
        }

        public static UniTask MornPlayAsync(this Animator animator, AnimationClip clip, float transition = 0,
            CancellationToken ct = default)
        {
            if (clip == null) return UniTask.CompletedTask;
            return MornPlayAsync(animator, clip.name, transition, ct);
        }

        public static void MornApplyImmediate(this Animator animator, AnimationClip clip, float normalizedTime = 1f)
        {
            if (clip == null) return;
            MornApplyImmediate(animator, clip.name, normalizedTime);
        }

        // State名を指定して再生（共通実装）
        public static void MornPlay(this Animator animator, string stateName, float transition = 0)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            
            animator.CrossFadeInFixedTime(stateName, transition);
        }

        public async static UniTask MornPlayAsync(this Animator animator, string stateName, float transition = 0,
            CancellationToken ct = default)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            
            animator.CrossFadeInFixedTime(stateName, transition);

            // まずステートが再生されるまで待機
            await UniTask.WaitUntil(
                () =>
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    return stateInfo.IsName(stateName) && stateInfo.normalizedTime > 0f;
                },
                cancellationToken: ct);
                
            // その後、ステートの終了まで待機
            await UniTask.WaitUntil(
                () =>
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    return !stateInfo.IsName(stateName) || stateInfo.normalizedTime >= 1f;
                },
                cancellationToken: ct);
        }

        public static void MornApplyImmediate(this Animator animator, string stateName, float normalizedTime = 1f)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;

            animator.Play(stateName, -1, normalizedTime);
            animator.Update(0f);
            animator.StopPlayback();
        }

        // 互換性のためのエイリアス
        public static void MornPlayState(this Animator animator, string stateName, float transition = 0)
        {
            MornPlay(animator, stateName, transition);
        }

        public static UniTask MornPlayStateAsync(this Animator animator, string stateName, float transition = 0,
            CancellationToken ct = default)
        {
            return MornPlayAsync(animator, stateName, transition, ct);
        }

        public static void MornApplyStateImmediate(this Animator animator, string stateName, float normalizedTime = 1f)
        {
            MornApplyImmediate(animator, stateName, normalizedTime);
        }
    }
}