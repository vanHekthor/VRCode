using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Spawner.Edges {

    [RequireComponent(typeof(SphereCollider))]
    public class HoverPoint : MonoBehaviour {

        public Transform circleTransform;

        private SphereCollider sphereCollider;

        private void Awake() {
            sphereCollider = GetComponent<SphereCollider>();
        }

        // Start is called before the first frame update
        void Start() {
        }

        // Update is called once per frame
        void Update() {

        }

        public void ChangeSize(float radius) {
            circleTransform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
            sphereCollider.radius = radius;
        }
    }
}