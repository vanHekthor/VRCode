using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRVis.Interaction.Controller.Mover {

    /// <summary>
    /// Will be attached to an object with a collider that uses the layer "Moveable".<para/>
    /// It provides information about how this object can be moved by the CodeWindowMover controller.
    /// </summary>
    public class Movable : MonoBehaviour {

        [Tooltip("The object that should be moved")]
        public Transform movableObject;

        [Tooltip("Use default rotation settings of code window mover instead")]
        public bool defaultRotationSettings = false;

        [Tooltip("Use default laser distance settings of code window mover instead")]
        public bool defaultDistanceSettings = false;

        [Header("Rotation Settings")]
        public float rotationSpeed = 100;
        public bool invertRotation = true;

        [Header("Laser Distance Settings")]
        public float distanceChangeSpeed = 10;
        public bool invertDirection = false; 
    }
}
