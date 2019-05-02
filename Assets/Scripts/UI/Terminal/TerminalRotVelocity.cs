using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.Terminal {

    /// <summary>
    /// Terminal rotation points can send messages to this component
    /// telling how much force to add or if the velocity should be set to zero.<para/>
    /// This script will then take care of rotating the terminal and decreasing the velocity over time.<para/>
    /// 
    /// Attach an instance of this component to the rotating part of the terminal.<para/>
    /// 
    /// Formula used as here: http://www.softschools.com/formulas/physics/air_resistance_formula/85/ <para/>
    /// and here: https://socratic.org/questions/how-can-momentum-be-decreased
    /// </summary>
    public class TerminalRotVelocity : MonoBehaviour {

        [Header("General")]
        public float terminalMass = 1;
        public float velocityMin = 0.05f;
        public float velocityMax = 6;
        public float velocityMultiplier = 1;
        public bool flipRotationDirection = true;
        public bool useEasyCalculation = false;

        [Header("For Advanced Calculation")]
        [Tooltip("Density of air the terminal moves through [kg/m^3]")]
        [Range(0, 500)]
        public float airDensity = 1.225f;

        [Tooltip("Drag coefficient [unitless]")]
        [Range(0, 1)]
        public float drag = 0.024f;

        [Tooltip("Area of terminal [m^2]")]
        [Range(0, 500)]
        public float area = 4;

        [Header("For Easy Calculation")]
        [Range(0, 500)]
        public float decreaseBy = 0.04f;


        private float momentum = 0;
        private float velocity = 0;
        private bool flipDir = true;


        /// <summary>
        /// Called through SendMessage commands of the rotation point instances.<para/>
        /// Sets the current rotation velocity of the terminal.<para/>
        /// The sign of the velocity tells about the rotation direction!
        /// </summary>
        public void SetVelocity(float velocity) {
            flipDir = velocity < 0 ? flipRotationDirection ? true : false : flipRotationDirection ? false : true;
            this.velocity = Mathf.Abs(velocity);
            if (this.velocity > velocityMax) { this.velocity = velocityMax; }
            //Debug.Log("Set velocity: " + this.velocity);
        }


	    // Update is called once per frame
	    void FixedUpdate() {
            UpdateRotation();
	    }


        /// <summary>
        /// Updates the rotation of the terminal.
        /// </summary>
        private void UpdateRotation() {

            // set zero when under threshold
            if (velocity < velocityMin) { velocity = 0; }
            if (velocity == 0) { return; } // fast exit

            // calculate the momentum taking the frame rate into account
            momentum = terminalMass * velocity;

            // use current momentum to rotate terminal
            transform.Rotate(transform.up, momentum * (flipDir ? -1 : 1));

            // decrease velocity accordingly
            float dt = 1; //Time.deltaTime * velocityMultiplier;
            if (useEasyCalculation) {
                velocity -= decreaseBy * dt;
            }
            else {
                float k = (airDensity * drag * area) / 2f;
                velocity -= k * velocity * velocity * dt;
            }
        }
    }
}
