using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Fallback {

    /**
     * Allows basic "spectator like" camera movement.
     */
    public class TestCamMovement : MonoBehaviour {

        public float speed = 5.0f;
        public float speedRunAdditional = 5.0f;
        public float rotateSpeed = 100.0f;
        public float maxCamUp = 70;
        public float maxCamDown = 70;

        //private Vector3 lastMousePos;
        //private bool lastMousePosSet = false;

        private void Start() {
        
            if (maxCamUp > 90) {
                Debug.LogWarning("Max Cam Up can not be greater than 90!");
                maxCamUp = 90;
            }

            if (maxCamDown > 90) {
                Debug.LogWarning("Max Cam Down can not be greater than 90!");
                maxCamDown = 90;
            }
        }

        void Update () {
            applyMovement();

            if (Input.GetMouseButton(1)) {

                /*
                Vector3 mousePos = Input.mousePosition;

                // the change of the previous pos to the current pos
                Vector3 deltaMousePos = mousePos - lastMousePos;
                lastMousePos = mousePos;

                if (lastMousePosSet) {
                    deltaMousePos.y = deltaMousePos.x;
                    deltaMousePos.x = 0;
                    transform.Rotate(deltaMousePos * rotateSpeed * Time.deltaTime);
                }
                else {
                    lastMousePosSet = true;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                */

                // lock the cursor to the center as soon at the rmb is pressed
                Cursor.lockState = CursorLockMode.Locked;

                Vector3 mouseMove = Vector3.zero;
                mouseMove += Input.GetAxis("Mouse Y") * -transform.right;
                mouseMove += Input.GetAxis("Mouse X") * transform.up;

                if (mouseMove.magnitude > 0) {
                    transform.Rotate(mouseMove * rotateSpeed * Time.deltaTime, Space.World);
                
                    // get the rotation quaternion to set the z-axis to zero
                    Quaternion q = transform.rotation;
                    float x_rot = q.eulerAngles.x;
                
                    // lock up/down rotation of camera
                    if (x_rot > maxCamDown && x_rot < 90) { x_rot = maxCamDown; } // down
                    else if (x_rot > 270 && x_rot < 360-maxCamUp) {  x_rot = 360-maxCamUp;  } // up

                    // set z-axis to zero (this locks it)
                    q.eulerAngles = new Vector3(x_rot, q.eulerAngles.y, 0);
                    transform.rotation = q;
                }
            }
            else {
                //lastMousePosSet = false;
            
                // release cursor lock
                Cursor.lockState = CursorLockMode.None;
            }
	    }

        void applyMovement() {
        
            Vector3 moveDir = Vector3.zero;

            // forward and backward
            moveDir += transform.forward * Input.GetAxis("Vertical");

            // left and right
            moveDir += transform.right * Input.GetAxis("Horizontal");

            // apply directional movement
            if (moveDir.magnitude > 0) {
                float applySpeed = speed + Input.GetAxis("Run") * speedRunAdditional;
                moveDir = moveDir.normalized * applySpeed;
                transform.Translate(moveDir * Time.deltaTime, Space.World);
            }
        }

    }
}
