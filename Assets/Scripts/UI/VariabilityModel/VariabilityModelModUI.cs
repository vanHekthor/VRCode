using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRVis.IO.Features;

namespace VRVis.UI.VariabilityModel {

    /// <summary>
    /// Script that is attached to the variability model modification UI prefab.<para/>
    /// It takes care of preparing the UI for usage and deals with interaction events.
    /// Created: 12.09.2019 (Leon H.)<para/>
    /// Updated: 12.09.2019
    /// </summary>
    public class VariabilityModelModUI : MonoBehaviour {

        [Tooltip("Slider to change the value")]
        public Slider valueSlider;

        [Tooltip("GameObject that should receive a notification and has the ChangeTextHelper attached to it")]
        public Transform optionNameText;

        [Tooltip("GameObject that should receive a notification and has the ChangeTextHelper attached to it")]
        public Transform valueText;
        
        public int maxSliderTicks = 10;


        private Feature_Range feature;

        // stores values of the feature
        private List<float> sliderValues;


	    /// <summary>Prepare the UI with the numerical option.</summary>
        public void PrepareUI(Feature_Range range_feature) {

            feature = range_feature;
            optionNameText.SendMessage("ChangeText", feature.GetName(), SendMessageOptions.DontRequireReceiver);
            range_feature.valueChangedEvent.AddListener(UpdateFeatureValue);
            if (valueSlider) { valueSlider.interactable = false; }
            List<float> values = range_feature.GetAllValues();
            Debug.Log("Values " + feature.GetName());

            /*
            System.Text.StringBuilder strb = new System.Text.StringBuilder();
            foreach (float f in values) { strb.Append(f); strb.Append(','); }
            Debug.Log(strb.ToString());
            */

            // get only a few of these values if there are too many
            sliderValues = new List<float>();

            if (values.Count <= maxSliderTicks) { values.ForEach(v => sliderValues.Add(v)); }
            else {

                // we always want the first and the last included
                float frac = values.Count / (float) maxSliderTicks;
                for (int i = 0; i <= maxSliderTicks; i++) {
                    int index = Mathf.FloorToInt(frac * i);
                    if (index >= values.Count-1) { break; }
                    sliderValues.Add(values[index]);
                }

                if (sliderValues.Count < maxSliderTicks) {
                    sliderValues.Add(values[values.Count-1]);
                }
            }

            // ToDo: fix slider last step not shown!

            if (valueSlider) {
                valueSlider.interactable = !feature.IsReadOnly();
                valueSlider.minValue = 0;
                valueSlider.maxValue = sliderValues.Count > 0 ? sliderValues.Count-1 : 0;
                valueSlider.wholeNumbers = true;
                valueSlider.onValueChanged.AddListener(SliderValueChangedEvent);
            }

            // update shown value
            UpdateFeatureValue();
            PositionSlider();
        }

        /// <summary>Positions the slider according to the current feature value.</summary>
        private void PositionSlider() {

            if (!valueSlider) { return; }

            int closestIndex = 0;
            float closestDist = Mathf.Infinity;
            for (int i = 0; i < sliderValues.Count; i++) {
                float d = Mathf.Abs(feature.GetValue() - sliderValues[i]);
                if (d < closestDist) {
                    closestDist = d;
                    closestIndex = i;
                }
            }
            valueSlider.value = closestIndex;
        }


        // ----------------------------------------------------------------------------------------------------
        // EVENT CALLBACKS

        /// <summary>Called when the features value changed so that the text is updated properly.</summary>
        private void UpdateFeatureValue() {
            valueText.SendMessage("ChangeText", feature.GetValue().ToString(), SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>Called when the sliders value was modified.</summary>
        private void SliderValueChangedEvent(float value) {
            if (sliderValues.Count < 1) { return; }
            if (value >= sliderValues.Count) { return; }
            feature.SetValue(sliderValues[(int) value]);
        }

    }
}
