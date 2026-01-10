using System;
using UnityEngine;

namespace MornLib
{
    [Serializable]
    public class MornTips
    {
        [SerializeField] private string _message;

        public MornTips(string message)
        {
            _message = message;
        }

        public MornTips()
        {
            _message = "Tipsが未設定です。";
        }
    }
}