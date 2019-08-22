using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Spawner.ConfigModel;
using VRVis.IO.Features;
using VRVis.UI.Helper;
using UnityEngine.Events;

namespace VRVis.UI.VariabilityModel {

    /// <summary>
    /// Script attached to the UI shown if hovered over a model node.<para/>
    /// Holds information about the text elements to change...
    /// </summary>
    public class VariabilityNodeHoverUI : MonoBehaviour {

        public GameObject text_id;
        public GameObject text_name;
        public GameObject text_value;

        /// <summary>The node the UI is currently attached to.</summary>
        public GameObject CurrentlyShownNode { get; set; }

        [Tooltip("Update time in seconds")]
        public float infoUpdateTime = 0.2f;

        private AFeature curOption;
        private VariabilityModelNodeInfo curNodeInfo;
        //private Coroutine updateCoroutine;

        private UnityAction valueChangeCallback;


        private void Awake() {
            valueChangeCallback = OptionValueChanged;
        }


        void OnDestroy() {
            //StopAllCoroutines();
            if (valueChangeCallback != null && curOption != null) { curOption.valueChangedEvent.RemoveListener(valueChangeCallback); }
        }


        /// <summary>
        /// Update the shown information using this node.<para/>
        /// Returns false if the according option could not be found and thus, no correct information shown.
        /// </summary>
        public bool ShowNodeInformation(VariabilityModelNodeInfo nodeInfo) {
            
            if (nodeInfo == null) { return false; }

            AFeature option = nodeInfo.GetOption();
            if (option == null) { return false; }

            if (nodeInfo != curNodeInfo || option != curOption) {

                // remove old callback and add new one
                if (valueChangeCallback != null) {
                    if (curNodeInfo != null) { curOption.valueChangedEvent.RemoveListener(valueChangeCallback); }
                    option.valueChangedEvent.AddListener(valueChangeCallback);
                }
                
                curNodeInfo = nodeInfo;
                curOption = option;
            }

            //Debug.Log("Updating node information...");

            // change text content accordingly
            if (text_id) { text_id.GetComponent<ChangeTextHelper>().ChangeText(option.GetName()); }
            if (text_name) { text_name.GetComponent<ChangeTextHelper>().ChangeText(option.GetDisplayName()); }

            float val = option.GetInfluenceValue();
            string activeState = val.ToString();
            if (option is Feature_Boolean) {
                if (val == 1) { activeState = "true"; }
                else { activeState = "false"; }
            }

            if (text_value) { text_value.GetComponent<ChangeTextHelper>().ChangeText(activeState); }
            return true;
        }


        /// <summary>Called when the value of an option changed to update the shown information.</summary>
        private void OptionValueChanged() { ShowNodeInformation(curNodeInfo); }


        ///// <summary>
        ///// Update the shown information using this node.<para/>
        ///// Returns false if the according option could not be found and thus, no correct information shown.
        ///// </summary>
        //public bool ShowNodeInformation(VariabilityModelNodeInfo nodeInfo) {
            
        //    if (nodeInfo == null) { return false; }

        //    AFeature option = nodeInfo.GetOption();
        //    if (option == null) { return false; }

        //    if (nodeInfo != curNodeInfo) {

        //        // stop current update coroutine
        //        if (updateCoroutine != null) { StopCoroutine(updateCoroutine); }
        //        updateCoroutine = null;
        //        curNodeInfo = nodeInfo;
        //    }

        //    //Debug.Log("Updating node information...");

        //    // send change requests to the objects
        //    //if (text_id) { text_id.SendMessage("ChangeText", option.GetName(), SendMessageOptions.DontRequireReceiver); } // not working for some reason
        //    if (text_id) { text_id.GetComponent<ChangeTextHelper>().ChangeText(option.GetName()); }

        //    //if (text_name) { text_name.SendMessage("ChangeText", option.GetDisplayName(), SendMessageOptions.DontRequireReceiver); }
        //    if (text_name) { text_name.GetComponent<ChangeTextHelper>().ChangeText(option.GetDisplayName()); }
            
        //    float val = option.GetInfluenceValue();
        //    string activeState = val.ToString();
        //    if (option is Feature_Boolean) {
        //        if (val == 1) { activeState = "true"; }
        //        else { activeState = "false"; } 
        //    }
        //    //if (text_value) { text_value.SendMessage("ChangeText", option.GetInfluenceValue(), SendMessageOptions.DontRequireReceiver); }
        //    if (text_value) { text_value.GetComponent<ChangeTextHelper>().ChangeText(activeState); }

        //    // start update coroutine
        //    if (gameObject.activeInHierarchy) { updateCoroutine = StartCoroutine(UpdateInfoCoroutine()); }
        //    return true;
        //}


        ///// <summary>
        ///// To update the shown node information in a fixed interval and not every frame.
        ///// </summary>
        //private IEnumerator UpdateInfoCoroutine() {

        //    yield return new WaitForSecondsRealtime(infoUpdateTime);
        //    if (gameObject.activeInHierarchy) { ShowNodeInformation(curNodeInfo); }
        //}

    }
}
