using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Walk : MonoBehaviour {
    [SerializeField]
    private SteamVR_Action_Vector2 input;

    [SerializeField]
    private float speed = 1f;

    public UnityEvent onWalking;

    public bool IsWalking { get; private set; }

    private CharacterController characterController;
    private Rigidbody rigidBody;
       
    private Vector3 previousPosition;

    void Start() {
        characterController = GetComponent<CharacterController>();
        rigidBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update() {
        Vector3 walkVector = Player.instance.hmdTransform.TransformDirection(new Vector3(input.axis.x, 0, input.axis.y));

        previousPosition = transform.position;
        transform.position += speed * Time.deltaTime * Vector3.ProjectOnPlane(walkVector, Vector3.up);

        if (walkVector.magnitude >= 0.001f) {
            onWalking.Invoke();
            IsWalking = true;
        } else {
            IsWalking = false;
        }
        //rigidBody.AddForce(speed * Time.deltaTime * Vector3.ProjectOnPlane(walkVector, Vector3.up));
        //characterController.Move(speed * Time.deltaTime * Vector3.ProjectOnPlane(walkVector, Vector3.up));
    }
}
