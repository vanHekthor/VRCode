using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace VRVis.Spawner.Regions {

    /// <summary>
    /// Attached to heightmap regions.<para/>
    /// Tells which is the actual object to scale and what just background.
    /// </summary>
    public class HeightmapRegionInfo : MonoBehaviour {

        public RectTransform foregroundPanel;
        public RectTransform backgroundPanel;
        public TMP_Text textValueOut;

    }
}
