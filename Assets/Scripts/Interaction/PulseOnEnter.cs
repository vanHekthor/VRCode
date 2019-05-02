using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Interaction {

    /// <summary>
    /// Add this component to objects that should pulse if the controller enters them.
    /// </summary>
    public class PulseOnEnter : MonoBehaviour {

        [Tooltip("Pulse duration in seconds")]
        public float duration = 0.01f;

        [Tooltip("Pulse amplitude")]
        public float amplitude = 1;

    }
}
