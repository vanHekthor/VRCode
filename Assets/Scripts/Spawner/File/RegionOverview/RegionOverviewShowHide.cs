using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;

namespace VRVis.Spawner.File.Overview {

    /// <summary>
    /// Registers a listener in the ApplicationSettings
    /// to show/hide the overview visualization accordingly.<para/>
    /// NOTE: We disable the canvas instead of the whole GameObject because
    /// otherwise the overview wont be generated right after spawning the window.<para/>
    /// Created: 05.09.2019 (Leon H.)<para/>
    /// Updated: 05.09.2019
    /// </summary>
    public class RegionOverviewShowHide : MonoBehaviour {

        [Tooltip("Canvas component to enable/disable accordingly")]
        public Canvas element;

        [Tooltip("Name of visualization to react to")]
        public string visualization = "overview";


        private void Awake() {

            // register listener for the event that the visibility is changed in the settings
            ApplicationLoader.GetApplicationSettings().visibilityChangedEvent.AddListener(VisibilityChangedEvent);
        }

        private void Start() {
            
            // executed right after the code window is spawned to check if the canvas should be shown or hidden
            bool visState;
            if (ApplicationLoader.GetApplicationSettings().GetVisualizationVisibility(visualization, out visState)) {
                SetVisibility(visState);
            }
        }


        /// <summary>Callback method, called when a visualization visibility changes.</summary>
        private void VisibilityChangedEvent(string visualization, bool visible) {
            if (visualization.ToLower() == this.visualization) { SetVisibility(visible); }
        }

        /// <summary>Change canvas enabled state to show/hide panel.</summary>
        private void SetVisibility(bool visible) {
            if (!element) { return; }
            element.enabled = visible;
        }

    }
}
