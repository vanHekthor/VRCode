using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddForce : MonoBehaviour {

    public float forceMultiplier = 1;
    public Vector3 forceDirection = Vector3.down;

    Rigidbody rb;

	// Use this for initialization
	void Start () {
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
		
        if (rb) {
            rb.AddForce(forceDirection * forceMultiplier, ForceMode.Force);
        }
	}
}
