using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.Helper {

    /// <summary>
    /// Component that allows a GameObject to be attached to a grid point of a SphereGrid.
    /// </summary>
    public class GridElement : MonoBehaviour {

        public SphereGridPoint AttachedTo { get; set; }

        public int GridPositionLayer { get; set; }
        public int GridPositionColumn { get; set; }

    }

}