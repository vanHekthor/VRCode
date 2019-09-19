using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.UI {

    /// <summary>
    /// Attach this script to a scroll view.<para/>
    /// After the object was initialized, the first frame will disable the horizontal scrollbar.<para/>
    /// As next, it will re-enable it, which often fixes the layout issues.<para/>
    /// Created: 19.09.2019 (Leon H.)<para/>
    /// Updated: 19.09.2019
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class FixHorScrollbar : MonoBehaviour {

        [Tooltip("Before disabling horizontal scrollbar")]
        public float waitBefore = 0.5f; // in seconds

        [Tooltip("Before re-enabling horizontal scrollbar")]
        public float waitAfter = 0.1f; // in seconds


	    private ScrollRect sr;
        private bool horizontalDefault = false;


	    void Start () {

		    sr = GetComponent<ScrollRect>();
            horizontalDefault = sr.horizontal;
            if (horizontalDefault) { StartCoroutine(ChangeState()); }
	    }

        IEnumerator ChangeState() {
            yield return new WaitForSecondsRealtime(waitBefore);
            sr.enabled = false;
            yield return new WaitForSecondsRealtime(waitAfter);
            sr.enabled = true;
        }

    }
}
