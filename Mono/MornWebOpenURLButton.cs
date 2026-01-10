using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace MornLib
{
    [RequireComponent(typeof(Button))]
    public sealed class MornWebOpenURLButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _url;

        private void Awake()
        {
            _button.OnSubmitAsObservable().Subscribe(_ => MornWebUtil.Open(_url)).AddTo(this);
        }

        private void Reset()
        {
            _button = GetComponent<Button>();
        }
    }
}