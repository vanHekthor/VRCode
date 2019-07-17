using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.Spawner.File.Overview {

    /// <summary>
    /// Component for the overview windows.<para/>
    /// Allows to link two scrollbars, whereby the second receives the size of the first initially in LINK mode.
    /// </summary>
    public class LinkScrollHandles : MonoBehaviour {

        [Tooltip("The main scrollbar that provides size and so on")]
        public Scrollbar scrollBar1;
        public Scrollbar scrollBar2;

        public bool firstHasAffect = true;
        public bool secondHasAffect = true;

        private float sb1_last_val = 0;
        private float sb2_last_val = 0;


	    void Update () {
	
            if (scrollBar1 && scrollBar2) {

                scrollBar2.size = scrollBar1.size;
            
                float sb1v = scrollBar1.value;
                float sb2v = scrollBar2.value;
                bool sb1_changed = firstHasAffect && Mathf.Abs(sb1v - sb1_last_val) > 0.0001f;
                bool sb2_changed = secondHasAffect && Mathf.Abs(sb2v - sb2_last_val) > 0.0001f;

                if (sb1_changed) { scrollBar2.value = sb1v; }
                else if (sb2_changed) { scrollBar1.value = sb2v; }

                sb1_last_val = scrollBar1.value;
                sb2_last_val = scrollBar2.value;
            }
	    }


        /// <summary>
        /// Set the scale y-axis of the second scrollbar.<para/>
        /// (This is used for the code overview window.)
        /// </summary>
        /// <param name="maxHeightPerc">Max height in percentage between 0 and 1</param>
        public void SetScrollbar2ScaleY(float maxHeightPerc) {

            if (maxHeightPerc > 1) { maxHeightPerc = 1; }
            else if (maxHeightPerc < 0) { maxHeightPerc = 0; }

            Vector3 s = scrollBar2.gameObject.transform.localScale;
            s.y = maxHeightPerc;
            scrollBar2.gameObject.transform.localScale = s;
        }

    }
}
