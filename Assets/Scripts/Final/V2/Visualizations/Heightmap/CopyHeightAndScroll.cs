using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Visualization.Heightmap {

    /// <summary>
    /// Will take care of copying the [height of the main windows content]
    /// as well as the [scroll of the main window] so that the
    /// heightmap on the side moves with the actual code.<para/>
    /// 
    /// Update:
    /// We may only set the height when we spawned the content of the code window,
    /// then we only update the y-Position of this rect transform.<para/>
    /// 
    /// Needs to be attached to the rect transform that should be affected
    /// (content object of the heightmap).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CopyHeightAndScroll : MonoBehaviour {

        public bool copyHeight = true;

        [Tooltip("Copy position results in copy of scroll")]
        public bool copyPosition = true;

        public RectTransform fromRect;
        private RectTransform thisRect;


	    void Start () {
		    thisRect = GetComponent<RectTransform>();
            if (!thisRect) { Debug.LogError("Missing required RectTransform component!"); }
	    }
	

	    void Update () {
            if (fromRect && thisRect) { UpdateHeightAndScroll(fromRect); }
	    }


        /// <summary>
        /// Get the height from the rect transform
        /// and the scroll from the scrollbar.
        /// </summary>
        void UpdateHeightAndScroll(RectTransform rect) {

            // copy the exact position of the content rect transform
            if (copyPosition) {
                Vector2 anchPos = thisRect.anchoredPosition;
                anchPos.y = rect.anchoredPosition.y;
                thisRect.anchoredPosition = anchPos;
            }
            
            if (copyHeight) {
                Vector2 sizeDelta = thisRect.sizeDelta;
                sizeDelta.y = rect.sizeDelta.y;
                thisRect.sizeDelta = sizeDelta;
            }
        }

    }
}
