using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Testing.Effects {

    /// <summary>BezierCurve for testing.</summary>
    public class BezierCurve : MonoBehaviour {

        public Transform startPoint;
        public Transform endPoint;
        public Transform[] controlPoints;
        public LineRenderer lineRenderer;
        public uint steps = 20;
        public bool updateCurve = true;


	    void Update () {
		
            if (lineRenderer && updateCurve && controlPoints.Length > 1) {
                Vector3[] points = new Vector3[steps];

                // bit of optimization for many bezier curves
                // to not calculate the start and endpoint again
                points[0] = startPoint.position;
                points[steps-1] = endPoint.position;

                for (uint i = 1; i < steps-1; i++) {
                    float t = (i) / (float) (steps-1);
                    points[i] = CubicBezier(t,
                        startPoint.position,
                        controlPoints[0].position,
                        controlPoints[1].position,
                        endPoint.position
                    );
                }
            
                // add points to line renderer
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);
            }

	    }


        /// <summary>
        /// Using the De-Casteljau-Algorithm.<para/>
        /// Calculates a point on cubit bezier curve (n = 3) for the given parameter t
        /// and returns the calculated point (Vector3).<para/>
        /// https://de.wikipedia.org/wiki/B%C3%A9zierkurve#B%C3%A9zierkurven_bis_zum_dritten_Grad
        /// </summary>
        Vector3 CubicBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {

            Vector3 b01 = LinearBezier(t, p0, p1);
            Vector3 b11 = LinearBezier(t, p1, p2);
            Vector3 b21 = LinearBezier(t, p2, p3);

            Vector3 b02 = LinearBezier(t, b01, b11);
            Vector3 b12 = LinearBezier(t, b11, b21);

            return LinearBezier(t, b02, b12);
        }

        /// <summary>Calculates a point on linear bezier curve (n=1).</summary>
        Vector3 LinearBezier(float t, Vector3 p0, Vector3 p1) {
            return (1-t) * p0 + t * p1;
        }

    }
}
