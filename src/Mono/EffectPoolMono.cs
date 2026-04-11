using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace MornLib
{
    public class EffectPoolMono : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _effectPrefab;
        [SerializeField] private int _initialSize = 3;
        private MornObjectPool<ParticleSystem> _pool;

        private void Awake()
        {
            _pool = new MornObjectPool<ParticleSystem>(OnGenerate, OnRent, OnReturn, _initialSize);
        }

        public ParticleSystem Play(Vector3 pos)
        {
            var effect = _pool.Rent();
            effect.transform.position = pos;
            return effect;
        }

        private ParticleSystem OnGenerate()
        {
            var effect = Instantiate(_effectPrefab, transform);
            var main = effect.main;
            main.stopAction = ParticleSystemStopAction.Disable;
            effect.OnDisableAsObservable().Subscribe(_ => _pool.Return(effect)).AddTo(this);
            return effect;
        }

        private static void OnRent(ParticleSystem x)
        {
            x.gameObject.SetActive(true);
            x.Play();
        }

        private static void OnReturn(ParticleSystem x)
        {
            x.Stop();
        }
    }
}