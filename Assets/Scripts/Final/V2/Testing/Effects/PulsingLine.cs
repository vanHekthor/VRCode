using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Testing.Effects {

    /**
     * This script should make the line
     * appear to pulse in a specified direction.
     * 
     * Tested: Yes
     * Result: Does not work that good
     * (Problem: would need to calculate color on begin and end)
     */
    [RequireComponent(typeof(LineRenderer))]
    public class PulsingLine : MonoBehaviour {

        public enum PulseDirection { RIGHT_LEFT, LEFT_RIGHT };
        public PulseDirection pulseDirection = PulseDirection.LEFT_RIGHT;

        public float pulseUpdateTime = 0.01f;
        public Color baseColor = new Color(0.5f, 0.5f, 1.0f);
        public Color pulseColor = new Color(0.9f, 0.9f, 1.0f);

        [Tooltip("Between 0 and 1")]
        public float pulse_pos_increment = 0.1f;
        public float pulse_width = 0.1f;

        private LineRenderer lineRenderer;
        private float lastPulse = -1;
        private float pulse_pos = 0; // between 0 and 1


        void Awake() {

            lineRenderer = GetComponent<LineRenderer>();
            if (!lineRenderer) { Debug.LogError("Missing required LineRenderer component!"); }

        }

        void Update() {
            
            PulseUpdate();

        }

        private void PulseUpdate() {

            if (!lineRenderer) { return; }
            if (Time.time > lastPulse + pulseUpdateTime) { lastPulse = Time.time; }
            else { return; }

            // ToDo: move the function representing the pulse in the specific direction

            // a = displacement on x-axis (increasing/decreasing over time)
            // b = fade (the higher the less fade)
            // one could use this function then: e^(-(bx - a)^2)
            // -> code: Mathf.Exp(-Mathf.Pow(b*x - a, 2));
            // http://www.mathematische-basteleien.de/glockenkurve.htm

            // https://docs.unity3d.com/ScriptReference/GradientColorKey.html
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            for (int k = 0; k < 5; k++) {
                float kOf5 = (k+1) / 5f;

                // get correct color
                Color color = baseColor;
                if (k == 2) { color = pulseColor; }
                else if (kOf5 == pulse_pos) { color = pulseColor; }
                else if (pulse_pos < 0 && k == 1) { color = pulseColor; }
                else if (pulse_pos > 1 && k == 3) { color = pulseColor; }

                // get correct time
                float time = kOf5;
                if (k == 1) { time = pulse_pos - pulse_width * 0.5f; }
                else if (k == 2) { time = pulse_pos; }
                else if (k == 3) { time = pulse_pos + pulse_width * 0.5f; }
                time = time < 0 ? 0 : time > 1 ? 1 : time;

                GradientColorKey key = new GradientColorKey(color, time);
                colorKeys[k] = key;
            }

            // apply color keys
            Gradient gradient = new Gradient();
            gradient.mode = GradientMode.Blend;
            gradient.colorKeys = colorKeys;
            lineRenderer.colorGradient = gradient;

            // increment pulse position
            pulse_pos += pulse_pos_increment;
            if (pulse_pos > 1 + pulse_width * 0.5f) { pulse_pos = -pulse_width * 0.5f; }

        }

    }

}
