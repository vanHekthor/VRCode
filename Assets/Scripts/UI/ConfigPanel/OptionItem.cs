using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRVis.IO.Features;

namespace VRVis.UI.Config {
    public class OptionItem : MonoBehaviour {
        public GameObject label;

        public enum OptionType { binary, numeric };

        public OptionType optionType;

        public enum OptionColor { standard, turnedOff, turnedOn, modified };

        private static readonly Color StandardColor = new Color(1, 1, 1);
        private static readonly Color TurnedOffColor = new Color(1, 0, 0);
        private static readonly Color TurnedOnColor = new Color(0, 1, 0);
        private static readonly Color ModifiedColor = new Color(1, 0, 1);

        public AFeature Feature { get; set; }

        private string _optionLabel;

        public string OptionLabel
        {
            get => _optionLabel;
            set
            {
                _optionLabel = value;
                UpdateItem(_optionLabel);
            }
        }

        private float _optionValue;

        public float OptionValue
        {
            get => _optionValue;
            set
            {
                _optionValue = value;
                UpdateItem(_optionValue);
            }
        }
        
        void Start() {
            if (label == null) {
                Debug.LogError("Option Item is missing a label game object!");
            }
            if (label.GetComponent<TextMeshProUGUI>() == null) {
                Debug.LogError("Label is missing a TextMeshPro UGUI component!");
            }
        }

        public void ChangeColor(OptionColor optionColor) {
            switch (optionColor) {
                case OptionColor.turnedOff:
                    label.GetComponent<TextMeshProUGUI>().color = TurnedOffColor;
                    break;
                case OptionColor.turnedOn:
                    label.GetComponent<TextMeshProUGUI>().color = TurnedOnColor;
                    break;
                case OptionColor.standard:
                    label.GetComponent<TextMeshProUGUI>().color = StandardColor;
                    break;
                case OptionColor.modified:
                    label.GetComponent<TextMeshProUGUI>().color = ModifiedColor;
                    break;
            }
        }

        private void UpdateItem(string optionName) {
            if (optionType == OptionType.binary) {
                UpdateLabel(optionName);
            }
            else if (optionType == OptionType.numeric) {
                UpdateLabel(optionName + ":");
            }
        }

        private void UpdateItem(float value) {
            if (optionType == OptionType.binary) {
                UpdateCheckbox(value);
            }
            else if (optionType == OptionType.numeric) {
                UpdateValue(value);
            }
        }        

        private void UpdateLabel(string optionName) {
            var tmpUGUI = label.GetComponent<TextMeshProUGUI>();

            if (tmpUGUI == null) {
                Debug.LogError("Necessary TextMeshPro UGUI component is missing! Can not display option label!");
            }
            else {
                tmpUGUI.text = optionName;
            }
        }

        private void UpdateCheckbox(float value) {
            var toggle = GetComponent<Toggle>();

            if (toggle == null) {
                Debug.LogError("Necessary toggle component is missing! Can not display binary option value!");
            }
            else {
                if (value == 1) {
                    GetComponent<Toggle>().isOn = true;
                }
                else {
                    GetComponent<Toggle>().isOn = false;
                }
            }            
        }

        private void UpdateValue(float value) {
            var valueObj = transform.Find("Value");
            if (valueObj == null) {
                Debug.LogError("GameObject for displaying the numeric option value not found!");
            }
            else {
                var tmpUGUI = valueObj.GetComponent<TextMeshProUGUI>();
                if (tmpUGUI == null) {
                    Debug.LogError("GameObject Value is missing a TextMeshPro UGUI component for displaying the numeric option value!");
                }
                else {
                    tmpUGUI.text = value.ToString();
                }
            }
        }
    }
}
