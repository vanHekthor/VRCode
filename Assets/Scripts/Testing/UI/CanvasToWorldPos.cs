using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Testing {

    /// <summary>
    /// Simple script that tells about positions of a rect transform.<para/>
    /// It shows that we can simply use the position as world coordinates.
    /// </summary>
    public class CanvasToWorldPos : MonoBehaviour {

        public RectTransform canvasTransform;
	    public Vector3 position;
        public Vector3 position_local;
        public Vector3 anchoredPosition;
        public Vector2 rectPosition;
        public Vector2 rectSize;
        public Vector2 rectMaxCornerPos;

        
	    void Update () {
		
            if (canvasTransform) {
                position = canvasTransform.position;
                position_local = canvasTransform.localPosition;
                anchoredPosition = canvasTransform.anchoredPosition;
                rectPosition = canvasTransform.rect.position;
                rectSize = canvasTransform.rect.size;
                rectMaxCornerPos = canvasTransform.rect.max;
            }

	    }


        private void OnDrawGizmos() {
        
            if (canvasTransform) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(position, 0.05f);
            }
        }
    }
}
