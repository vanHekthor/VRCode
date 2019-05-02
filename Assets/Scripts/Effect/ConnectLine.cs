using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Siro.Effects {

    // Creates a connection between this and another object.
    public class ConnectLine : MonoBehaviour {

        public Transform trackedObject;
        public LineRenderer lineRenderer;
        public Material lineRendererMaterial;
        public bool updatePositions = false;


	    // Use this for initialization
	    void Start () {
	    
            if (!trackedObject) {
                Debug.LogError("Missing object to track!");
                return;
            }

            // get or add line renderer component
            if (!lineRenderer) { lineRenderer = GetComponent<LineRenderer>(); }
            if (!lineRenderer) {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                if (lineRendererMaterial) { lineRenderer.material = lineRendererMaterial; }
                lineRenderer.startWidth = 0.01f;
                lineRenderer.endWidth = 0.01f;
                applyPositions();
            }

	    }
	
	    // Update is called once per frame
	    void Update () {
		
            if (updatePositions && lineRenderer && trackedObject) {
                applyPositions();
            }

	    }

        void applyPositions() {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, trackedObject.position);
        }

    }

}
