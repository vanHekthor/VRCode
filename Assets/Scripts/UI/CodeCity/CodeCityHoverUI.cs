using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.LaserHand;
using VRVis.Interaction.LaserPointer;
using VRVis.Spawner.CodeCity;

namespace VRVis.UI.CodeCity {

    /// <summary>
    /// User interface to show on hover.<para/>
    /// This script is attached to the code city elements.<para/>
    /// Last Updated: 22.08.2019
    /// </summary>
    public class CodeCityHoverUI : HoverUI {        
        /// <summary>
        /// Update the shown information using code city element components.
        /// </summary>
        protected override void UpdateUIInfo() {

            // we can get the node information from this object
            // because this component is attached to it
            CodeCityElement e = GetComponent<CodeCityElement>();
            if (e) { SetUIInfo(e); return; }
            Debug.LogWarning("Failed to set UI info! - No CodeCityElement component found.", this);
        }


        /// <summary>
        /// Set UI information (text contents) using the text adder.
        /// </summary>
        private void SetUIInfo(CodeCityElement e) {            
            CCUITextAdder tadder = uiInstance.GetComponent<CCUITextAdder>();
            if (!tadder) { return; }
            e.GetInfo().ForEach((info) => { tadder.AddText(info.Key, info.Value); });
        }

    }
}
