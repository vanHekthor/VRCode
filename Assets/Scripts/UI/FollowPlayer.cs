using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI {

    /// <summary>
    /// This script is mainly used for the terminal to follow the player.
    /// </summary>
    public class FollowPlayer : MonoBehaviour {

        [System.Serializable]
        public class FollowEntry {

            [Tooltip("Object to follow")]
            public GameObject follow;

            [Header("Position")]
            [Tooltip("Axis to follow the object on")]
            public Vector3 followAxis;

            public Vector3 globalOffset;

            [Tooltip("Local offset is x multiplied with local right, y with local up and z with local forward!")]
            public Vector3 localOffset;

            [Header("Rotation")]
            public bool copyRotation = false;
            public Vector3 rotationAxis;
            public Vector3 rotationOffset;

            public FollowEntry() {
                followAxis = Vector3.zero;
                rotationAxis = Vector3.zero;
            }
        }


        [Tooltip("The objects to follow (uses the first valid one of the list)")]
        public FollowEntry[] follows;

        [Tooltip("Seconds to wait before searching for valid object to follow")]
        public float waitSeconds = 2;

        
        private int curIndex = -1;
        private bool foundOne = true;


        void Start() {
            
            if (follows.Length == 0) {
                Debug.LogError("No objects to follow assigned!");
                enabled = false;
                return;
            }
        }

        void Update() {

            if (foundOne) { FollowObject(); }
        }


        /// <summary>
        /// Checks if the current selected object is still valid.
        /// </summary>
        private bool IsValid(int index) {

            if (index < 0 || index > follows.Length) { return false; }
            if (follows[index].follow == null) { return false; }
            if (!follows[index].follow.activeInHierarchy) { return false; }
            return true;
        }


        /// <summary>
        /// Returns the index of the valid object or -1 if none found.
        /// </summary>
        private int GetValidObject() {

            for (int i = 0; i < follows.Length; i++) {
                if (IsValid(i)) { return i; }
            }

            return -1;
        }


        /// <summary>
        /// Follow the currently selected object.
        /// </summary>
        private void FollowObject() {

            if (!IsValid(curIndex)) {
            
                // search for new one
                foundOne = false;
                StartCoroutine(SearchValid(waitSeconds));
                return;
            }

            FollowEntry fe = follows[curIndex];

            // offset scale in directions of the followed object
            Vector3 offsetAdd = Vector3.zero;
            offsetAdd += fe.follow.transform.right * fe.localOffset.x;
            offsetAdd += fe.follow.transform.up * fe.localOffset.y;
            offsetAdd += fe.follow.transform.forward * fe.localOffset.z;

            // apply position
            Vector3 pos = Vector3.Scale(fe.follow.transform.position, fe.followAxis);
            transform.position = pos + offsetAdd + fe.globalOffset;

            // copy and apply rotation accordingly
            if (fe.copyRotation) {
                Quaternion rot = fe.follow.transform.rotation;
                rot.x *= fe.rotationAxis.x;
                rot.y *= fe.rotationAxis.y;
                rot.z *= fe.rotationAxis.z;
                transform.rotation = Quaternion.Euler(fe.rotationOffset) * rot;
            }
        }


        /// <summary>
        /// Coroutine for waiting and searching for valid object.
        /// </summary>
        private IEnumerator SearchValid(float waitSeconds) {

            int index = GetValidObject();
            if (IsValid(index)) {
                curIndex = index;
                foundOne = true;
            }
            else {

                // wait before continuing search
                yield return new WaitForSecondsRealtime(waitSeconds);
                StartCoroutine(SearchValid(waitSeconds));
            }
        }

    }
}
