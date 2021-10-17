using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class ControllerVisibility : MonoBehaviour {
        
    public enum VisibilityMode { permanently, close_to_face, never }

    public VisibilityMode visibilityMode; 
    public float visibilityDistance = 0.5f;

    // Update is called once per frame
    void Update() {
        switch (visibilityMode) {
            case VisibilityMode.permanently:
                ShowPermanentlyUpdate();
                break;
            case VisibilityMode.close_to_face:
                ShowWhenCloseToFaceUpdate();
                break;
            case VisibilityMode.never:
                ShowNeverUpdate();
                break;
        }
    }

    private void ShowPermanentlyUpdate() {
        foreach (var hand in Player.instance.hands) {
            hand.ShowController();
            hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithController);
        }
    }

    private void ShowWhenCloseToFaceUpdate() {
        foreach (var hand in Player.instance.hands) {
            var headPosition = Player.instance.hmdTransform.position;
            var distVec = headPosition - hand.transform.position;
            if (distVec.magnitude < visibilityDistance) {
                hand.ShowController();
                hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithController);
            }
            else {
                hand.HideController();
                hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithoutController);
            }
        }
    }

    private void ShowNeverUpdate() {
        foreach (var hand in Player.instance.hands) {
            hand.HideController();
            hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithoutController);
        }
    }
}
