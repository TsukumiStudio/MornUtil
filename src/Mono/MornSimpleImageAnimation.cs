using UnityEngine;
using UnityEngine.UI;

namespace MornLib
{
    internal sealed class MornSimpleImageAnimation : MonoBehaviour
    {
        [SerializeField] private Image _renderer;
        [SerializeField] private Sprite[] _sprites;
        [SerializeField] private float _duration = 0.2f;
        [SerializeField] private bool _isLoop = true;
        [SerializeField] private bool _isPingPong;
        private int _nextIndex;
        private float _nextChangeTime;

        private void Reset()
        {
            _renderer = GetComponent<Image>();
        }

        private void OnEnable()
        {
            UpdateIndex(0);
        }

        private void Update()
        {
            if (_sprites == null || _sprites.Length == 0 || _renderer == null)
            {
                return;
            }

            if (_isLoop == false && _nextIndex >= _sprites.Length - 1)
            {
                return;
            }

            if (Time.time >= _nextChangeTime)
            {
                UpdateIndex(_nextIndex + 1);
                _nextChangeTime = Time.time + _duration;
            }
        }

        private void UpdateIndex(int index)
        {
            if (_isPingPong)
            {
                var length = _sprites.Length;
                var pingPongIndex = index % (2 * length - 2);
                if (pingPongIndex >= length)
                {
                    pingPongIndex = 2 * length - 2 - pingPongIndex;
                }

                _renderer.sprite = _sprites[pingPongIndex];
            }
            else
            {
                _renderer.sprite = _sprites[index % _sprites.Length];
            }

            _nextIndex = index;
        }
    }
}