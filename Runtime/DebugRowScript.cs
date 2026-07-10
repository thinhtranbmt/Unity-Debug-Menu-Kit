using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMenuKit
{
    /// <summary>One button row of the debug menu (spawned from the DebugRow prefab).</summary>
    public class DebugRowScript : MonoBehaviour
    {
        [SerializeField] Button btnClick;
        [SerializeField] TextMeshProUGUI textLabel;

        private string rowName;
        private Action callback;

        public string RowName => rowName;

        public void Init(string name, Action callback)
        {
            this.rowName = name;
            this.callback = callback;
            textLabel.text = name;
            btnClick.onClick.AddListener(EventButtonClick);
        }

        private void EventButtonClick()
        {
            callback?.Invoke();
        }
    }
}
