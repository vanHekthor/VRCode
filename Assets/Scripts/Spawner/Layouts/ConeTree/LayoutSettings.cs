using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Spawner.Layouts.ConeTree {
    
    /// <summary>Class for layout settings to be usable from within the editor.</summary>
    [System.Serializable]
    public class LayoutSettings {

        [Tooltip("Minimum radius of a single option node")]
        public float minRadius = 0.05f;

        [Tooltip("Maximum radius of a single option node (set 0 for unlimited)")]
        public float maxRadius = 0;

        [Tooltip("Gap between nodes on the same level")]
        public float nodeSpacing = 0.05f;

        [Tooltip("If previous radius is used, higher level nodes are positioned according to it. Turning this off can lead to overlapping!")]
        public bool useRadiusOfPreviousLevel = true;

    }
}
