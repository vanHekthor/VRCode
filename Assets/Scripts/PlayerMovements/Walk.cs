using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Walk : MonoBehaviour
{
    [SerializeField]
    private SteamVR_Action_Vector2 input;

    [SerializeField]
    private float speed = 1f;
    
    // Update is called once per frame
    void Update()
    {
        Vector3 walkVector = Player.instance.hmdTransform.TransformDirection(new Vector3(input.axis.x, 0, input.axis.y));
        transform.position += speed * Time.deltaTime * Vector3.ProjectOnPlane(walkVector, Vector3.up);
    }
}
