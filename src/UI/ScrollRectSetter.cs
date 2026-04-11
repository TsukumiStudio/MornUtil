using UnityEngine;
using UnityEngine.UI;

namespace MornLib
{
    [RequireComponent(typeof(ScrollRect))]
    internal sealed class ScrollRectSetter : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private ScrollRectSettings _settings;

        private void Awake()
        {
            if (_scrollRect == null || _settings == null)
            {
                return;
            }

            _scrollRect.movementType = _settings.MovementType;
            _scrollRect.elasticity = _settings.Elasticity;
            _scrollRect.inertia = _settings.Inertia;
            _scrollRect.decelerationRate = _settings.DecelerationRate;
            _scrollRect.scrollSensitivity = _settings.ScrollSensitivity;
        }

        private void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }
    }
}