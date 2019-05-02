using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Effects {

    /// <summary>
    /// Class that allows calculating a cubic bezier curve.<para/>
    /// This can be converted to an interface later which n control points.
    /// </summary>
    public class BezierCurve {

        private uint steps = 2;


        // CONSTRUCTOR

        public BezierCurve() {}


        // GETTER AND SETTER

        public uint GetSteps() { return steps; }

        /// <summary>Set the steps (Minimum is 2)</summary>
        /// <param name="steps"></param>
        public void SetSteps(uint steps) {
            if (steps < 2) { return; }
            this.steps = steps;
        }


        // FUNCTIONALITY

        /// <summary>
        /// Calculate the points of the curve using the current settings.<para/>
        /// Returns as many points as the current step size + 2 (additional at start and end).
        /// </summary>
        /// <param name="start">Start of the curve</param>
        /// <param name="startSide">In which direction the line goes from the start</param>
        /// <param name="end">End of the curve</param>
        /// <param name="endSide">From which direction the line comes to the end</param>
        public Vector3[] CalculatePoints(Vector3 start, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 end) {

            Vector3[] points = new Vector3[steps];

            // bit of optimization for many bezier curves
            // to not calculate the start and endpoint again
            points[0] = start;
            //points[1] = start + startSide;
            //points[points.Length-2] = end + endSide;
            points[points.Length-1] = end;

            for (uint i = 1; i < steps-1; i++) {
                float t = (i) / (float) (steps-1);
                points[i] = CubicBezier(t,
                    start,
                    controlPoint1,
                    controlPoint2,
                    end
                );
            }

            /*
            for (uint i = 2; i < steps-2; i++) {
                float t = (i-1) / (float) (steps-2);
                points[i] = CubicBezier(t,
                    points[1],
                    controlPoint1,
                    controlPoint2,
                    points[steps-2]
                );
            }
            */
            
            return points;
        }

        /// <summary>
        /// Using the De-Casteljau-Algorithm.<para/>
        /// Calculates a point on cubit bezier curve (n = 3) for the given parameter t
        /// and returns the calculated point (Vector3).<para/>
        /// https://de.wikipedia.org/wiki/B%C3%A9zierkurve#B%C3%A9zierkurven_bis_zum_dritten_Grad
        /// </summary>
        private Vector3 CubicBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {

            Vector3 b01 = LinearBezier(t, p0, p1);
            Vector3 b11 = LinearBezier(t, p1, p2);
            Vector3 b21 = LinearBezier(t, p2, p3);

            Vector3 b02 = LinearBezier(t, b01, b11);
            Vector3 b12 = LinearBezier(t, b11, b21);

            return LinearBezier(t, b02, b12);
        }

        /// <summary>Calculates a point on linear bezier curve (n=1).</summary>
        private Vector3 LinearBezier(float t, Vector3 p0, Vector3 p1) {
            return (1-t) * p0 + t * p1;
        }

    }
}
