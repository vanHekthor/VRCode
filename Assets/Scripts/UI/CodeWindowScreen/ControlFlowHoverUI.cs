using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Spawner.CodeCity;
using VRVis.Spawner.Edges;
using VRVis.UI.CodeCity;

namespace VRVis.UI {

    [RequireComponent(typeof(HoverPoint))]
    public class ControlFlowHoverUI : HoverUI {

        private HoverPoint hoverPoint;
        private string className;
        private string signature;

        void Awake() {
            hoverPoint = GetComponent<HoverPoint>();    
        }

        private bool infoWasUpdated = false;
        protected override void UpdateUIInfo() {
            if (!infoWasUpdated) {
                var edgeConnection = hoverPoint.EdgeConnection;

                className = edgeConnection.GetEndCodeFileInstance().fileName.text;
                signature =
                    CodeWindowTextHelper.LoadLineFromFile(
                        edgeConnection.GetEndCodeFileInstance().GetCodeFile().GetNode(),
                        edgeConnection.GetEdge().GetTo().lines.from);                
            }

            CCUITextAdder textAdder = uiInstance.GetComponent<CCUITextAdder>();
            if (!textAdder) { return; }
            textAdder.AddText("", className, true);
            textAdder.AddText("", signature, true);

            infoWasUpdated = true;
        }
    }
}