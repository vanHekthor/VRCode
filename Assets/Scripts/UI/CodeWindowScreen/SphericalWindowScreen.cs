using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.CodeWindowScreen {

    /// <summary>
    /// Screen where code windows can be placed onto.
    /// </summary>
    public class SphericalWindowScreen : MonoBehaviour {

        [Tooltip("Max. elevation (higher means closer to 'north pole') for code windows")]
        public float maxElevationAngle;

        [Tooltip("Min. elevation (higher means closer to 'south pole') for code windows")]
        public float minElevationAngle;

        [Tooltip("Max. polar (higher means farther east)")]
        public float maxPolarAngle;

        [Tooltip("Min. polar (lower means farther west)")]
        public float minPolarAngle;

        // GETTER

        public float MaxElevationAngleInDegrees => maxElevationAngle;

        public float MaxElevationAngleInRadians { get => Mathf.PI / 180 * maxElevationAngle; }

        public float MinElevationAngleInDegrees => minElevationAngle;

        public float MinElevationAngleInRadians { get => Mathf.PI / 180 * minElevationAngle; }

        public float MaxPolarAngleInDegrees => maxPolarAngle;

        public float MaxPolarAngleInRadians { get => Mathf.PI / 180 * maxPolarAngle; }

        public float MinPolarAngleInDegrees => minPolarAngle;

        public float MinPolarAngleInRadians { get => Mathf.PI / 180 * minPolarAngle; }

    }

}