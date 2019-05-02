using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Effects {

    // Creates a connection between this and another object.
    public class ConnectionLine : MonoBehaviour {

        public Transform lineStart;
        public Transform lineEnd;
        public LineRenderer lineRenderer;
        
        public bool updatePositions = false;

        [Space]
        [Header("If no line renderer attached:")]
        public Material lineRendererMaterial;
        public Vector2 lineWidth = new Vector2(0.01f, 0.01f);


	    void Start () {
	    
            if (!lineStart) { lineStart = transform; }

            if (!lineEnd) {
                Debug.LogWarning("Missing object to track!");
                return;
            }

            // get or add line renderer component
            if (!lineRenderer) { lineRenderer = GetComponent<LineRenderer>(); }
            if (!lineRenderer) {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                if (lineRendererMaterial) { lineRenderer.material = lineRendererMaterial; }
                lineRenderer.startWidth = lineWidth.x;
                lineRenderer.endWidth = lineWidth.y;
                applyPositions();
            }
	    }
	

	    void Update () {
		
            if (updatePositions && lineRenderer && lineEnd) {
                applyPositions();
            }
	    }


        void applyPositions() {

            if (lineRenderer.positionCount < 2) { lineRenderer.positionCount = 2; }
            lineRenderer.SetPosition(0, lineStart.position);
            lineRenderer.SetPosition(1, lineEnd.position);
        }

    }
}
