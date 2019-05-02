using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LaserScript : MonoBehaviour {

    public SteamVR_Input_Sources inputSource;
    public GameObject laserPrefab;
    public GameObject laser;

    private Transform laserTransform;
    private Vector3 hitPoint;

    // moveable gameobject hit by laser
    private GameObject moveAbleHit;
    private GameObject moveAbleHit_last;
    private Color defaultColor;

    // moveable gameobject currently hold with laser
    private GameObject moveAbleSelected;
    private Vector3 moveAblePreviousPosition;
    /**private float lastScroll = 0;*/

	// Use this for initialization
	void Start () {
        
        // create a new instance of the laser prefab
		laser = Instantiate(laserPrefab);
        laserTransform = laser.transform;
	}
	
	// Update is called once per frame
	void Update () {
		
        // get controller action
        if (getControllerTouchpadPress()) {
            RaycastHit hit;

            //Debug.DrawRay(transform.position, transform.forward, Color.green);

            // perform ray casting and if we hit something, show the laser
            if (Physics.Raycast(transform.position, transform.forward, out hit, 100)) {
                hitPoint = hit.point;
                showLaser(hit.distance);

                if (hit.collider.tag.ToLower() == "moveable") {
                    moveAbleHit = hit.collider.gameObject;
                }
                else {
                    moveAbleHit = null;
                }

                if (moveAbleHit_last && !moveAbleHit_last.Equals(moveAbleHit)) {
                    moveAbleHit_last.GetComponent<Renderer>().material.color = defaultColor;
                    moveAbleHit_last = null;
                }

                if (moveAbleHit && !moveAbleHit.Equals(moveAbleHit_last)) {
                    moveAbleHit_last = moveAbleHit;
                    Renderer mhR = moveAbleHit.GetComponent<Renderer>();
                    defaultColor = mhR.material.color;
                    mhR.material.color = Color.green;
                }
            }
            else {
                // simulate ray hit
                hitPoint = transform.position + transform.forward * 100;
                showLaser(100);

                // reset color of last selected
                moveAbleHit = null;
                if (moveAbleHit_last) {
                    moveAbleHit_last.GetComponent<Renderer>().material.color = defaultColor;
                    moveAbleHit_last = null;
                }
            }
        }
        else {
            // hide the laser if we do not press the button
            laser.SetActive(false);

            // reset color of last selected
            moveAbleHit = null;
            if (moveAbleHit_last) {
                moveAbleHit_last.GetComponent<Renderer>().material.color = defaultColor;
                moveAbleHit_last = null;
            }
        }

        // player is pressing (pinch)
        if (getControllerPress()) {
            grabObject(moveAbleHit);

            /*
            if (moveAbleSelected && isScrolling() && (laser && !laser.activeSelf)) {

                // because there is always some noise
                float scrollDiff = getScroll() - lastScroll;
                //Debug.Log(scrollDiff);

                if (scrollDiff < 0.1 && scrollDiff > -0.1) {
                    float maxScroll = 2;

                    Rigidbody moveAbleRB = moveAbleSelected.GetComponent<Rigidbody>();
                    Vector3 moveToCam = transform.position - moveAbleSelected.transform.position;
                    float vecLength = Vector3.Magnitude(moveToCam);
                    float scrollSpeed = Mathf.Lerp(getScroll(), lastScroll, 0.5f);
                    float perc = (maxScroll / vecLength) * scrollSpeed * -1;

                    // disconnect joint, move the object and reconnect joint
                    Joint joint = GetComponent<FixedJoint>();
                    moveAbleRB.detectCollisions = false;
                    if (joint) { joint.connectedBody = null; }
                    moveAbleSelected.transform.position += perc * moveToCam;
                    if (joint && moveAbleRB) { joint.connectedBody = moveAbleRB; }
                    moveAbleRB.detectCollisions = true;
                }
                else {
                    lastScroll = 0;
                }
            }
            else {
                lastScroll = 0;
            }
            */
        }
        else {
            releaseObject();
        }

        // save current position as previous position of the moved object
        if (moveAbleSelected) {
            moveAblePreviousPosition = moveAbleSelected.transform.position;
        }
	}

    private Vector3 getControllerPosition() {
        return Vector3.zero;
        //return SteamVR_Input._default.inActions.Pose.GetLocalPosition(inputSource); // no longer supported!
    }

    private bool getControllerTouchpadPress() {
        return false;
        //return SteamVR_Input._default.inActions.Teleport.GetState(inputSource);  // no longer supported!
    }

    private bool getControllerPress() {
        return false;
        //return SteamVR_Input._default.inActions.GrabPinch.GetState(inputSource);  // no longer supported!
    }

    private Vector3 getControllerVelocity() {
        return Vector3.zero;
        //return SteamVR_Input._default.inActions.Pose.GetVelocity(inputSource);  // no longer supported!
    }
    private Vector3 getControllerAngularVelocity() {
        return Vector3.zero;
        //return SteamVR_Input._default.inActions.Pose.GetAngularVelocity(inputSource);  // no longer supported!
    }

    // DISABLED FOR NOW BECAUSE IT CAUSES UNWANTED HAPTIC FEEDBACK
    /*
    private bool isScrolling() {
        return SteamVR_Input._default.inActions.ScrollWheel.GetChanged(inputSource);
    }

    private float getScroll() {
        return SteamVR_Input._default.inActions.ScrollWheel.GetAxis(inputSource).y;
    }
    */

    private void showLaser(float hitDistance) {

        // show the laser prefab
        laser.SetActive(true);

        // get the middle of the vector that connects the controller and the hit point
        laserTransform.position = Vector3.Lerp(transform.position, hitPoint, 0.5f);

        // make laser look at the hit point
        laserTransform.LookAt(hitPoint);

        // scale laser prefab to match the vector
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, hitDistance);
    }

    private void grabObject(GameObject selectedGO) {

        if (moveAbleSelected) { return; }
        if (!selectedGO) { return; }

        moveAbleSelected = selectedGO;
        Rigidbody moveAbleRB = moveAbleSelected.GetComponent<Rigidbody>();
        if (!moveAbleRB) { return; }

        // create joint
        Joint joint = GetComponent<FixedJoint>();
        if (!joint) {
            joint = gameObject.AddComponent<FixedJoint>();
        }
        joint.connectedBody = moveAbleRB;
    }

    private void releaseObject() {
        
        if (!moveAbleSelected) { return; }

        Joint joint = GetComponent<FixedJoint>();
        if (!joint) { return; }

        // release joint
        joint.connectedBody = null;

        // give object the velocity and rotation
        Rigidbody moveAbleRB = moveAbleSelected.GetComponent<Rigidbody>();
        if (!moveAbleRB) { return; }

        // calculate velocity of the object
        Vector3 moveAbleVelocity = getControllerVelocity();
        float dt = Time.deltaTime;
        if (dt != 0) {
            Vector3 curPos = moveAbleSelected.transform.position;
            moveAbleVelocity = new Vector3(
                curPos.x - moveAblePreviousPosition.x,
                curPos.y - moveAblePreviousPosition.y,
                curPos.z - moveAblePreviousPosition.z) / dt;
        }

        // apply velocity
        moveAbleRB.velocity = moveAbleVelocity;
        moveAbleRB.angularVelocity = getControllerAngularVelocity();
        moveAbleRB = null;
        moveAbleSelected = null;
    }

}
