using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Testing {

    /// <summary>
    /// Simply show information about this object in inspector.
    /// </summary>
    public class ShowWorldPosition : MonoBehaviour {

        public GameObject obj;

        [Header("Local")]
        public Vector3 localPos;
        public Quaternion localRot;

        [Header("Global")]
        public Vector3 worldPos;
        public Quaternion worldRot;

        [Header("Global LateUpdate")]
        public Vector3 worldPosLU;
        public Quaternion worldRotLU;

        [Header("Global FixedUpdate")]
        public Vector3 worldPosFU;
        public Quaternion worldRotFU;


	    // Use this for initialization
	    void Start () {
		    if (!obj) { obj = gameObject; }
	    }
	

	    // Update is called once per frame
	    void Update () {
		
            if (!obj) { return;}

            localPos = obj.transform.localPosition;
            localRot = obj.transform.localRotation;

            worldPos = obj.transform.position;
            worldRot = obj.transform.rotation;
	    }


        void FixedUpdate() {

            if (!obj) { return; }

            worldPosFU = obj.transform.position;
            worldRotFU = obj.transform.rotation;
        }

    }
}
