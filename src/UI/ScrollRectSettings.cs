using UnityEngine;
using UnityEngine.UI;

namespace MornLib
{
    [CreateAssetMenu(fileName = nameof(ScrollRectSettings), menuName = "Morn/" + nameof(ScrollRectSettings))]
    internal sealed class ScrollRectSettings : ScriptableObject
    {
        [SerializeField] private ScrollRect.MovementType _movementType = ScrollRect.MovementType.Elastic;
        [SerializeField] private float _elasticity = 0.1f;
        [SerializeField] private bool _inertia = true;
        [SerializeField] private float _decelerationRate = 0.135f;
        [SerializeField] private float _scrollSensitivity = 0.2f;
        [SerializeField] private float _scrollSensitivityWebGL = 0.2f;
        public ScrollRect.MovementType MovementType => _movementType;
        public float Elasticity => _elasticity;
        public bool Inertia => _inertia;
        public float DecelerationRate => _decelerationRate;
#if !UNITY_EDITOR && UNITY_WEBGL
        public float ScrollSensitivity => _scrollSensitivityWebGL;
#else
        public float ScrollSensitivity => _scrollSensitivity;
#endif
    }
}