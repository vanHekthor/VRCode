using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.PsychicHand;
using VRVis.Spawner.File;
using VRVis.UI.Helper;
using VRVis.Utilities;

public class PsychicGrabElement : MonoBehaviour, ITelekinesable {

    public Transform grabbedTransform;
    public ParticleSystem focusEffect;
    public ParticleSystem grabEffect;
    public float snapTime = 1;

    public AnimationCurve distanceIntensityCurve = AnimationCurve.Linear(0.0f, 800.0f, 1.0f, 800.0f);
    public AnimationCurve pulseIntervalCurve = AnimationCurve.Linear(0.0f, 0.01f, 1.0f, 0.0f);

    public float pulseIntensity = 1000.0f;
    public float pulseInterval = FocusPulseInterval;

    public bool IsVisible { get; private set; }

    private const float FocusPulseInterval = 0.5f;
    private const float GrabPulseInterval = 0.33f;

    private Camera vrCamera;
    private Collider elementCollider;

    private bool grabbed = false;
    private bool focused = false;

    private IEnumerator pulseCoroutine;

    private GameObject codeWindow;
    private SphereGrid grid;
    private GridElement gridElement;
    private SphereGridPoint selectedGridPoint;
    private ZoomCodeWindowButton zoomButton;
    private bool moveWindowOnSphere;

    private Transform targetPoint;
    private float dropTimer;
    private bool moveToTarget = false;
    private bool moveTowardsHand = false;
    private bool attachedToHand = false;
    private Vector3 originalScale;
    private Transform telekineticAttachmentPoint;

    void Start() {
        codeWindow = GetComponentInParent<CodeFileReferences>().gameObject;
        gridElement = codeWindow.GetComponent<GridElement>();
        grid = gridElement.Grid;
        zoomButton = codeWindow.GetComponentInChildren<ZoomCodeWindowButton>();

        targetPoint = grid.GetGridPoint(gridElement.GridPositionLayer, gridElement.GridPositionColumn).AttachmentPointObject.transform;

        if (grid != null) {
            moveWindowOnSphere = true;
        }

        originalScale = grabbedTransform.localScale;

        vrCamera = Player.instance.gameObject.GetComponentInChildren<Camera>();
        elementCollider = gameObject.GetComponent<Collider>();
    }
    
    void Update() {       

        if (!moveToTarget || attachedToHand) {
            dropTimer = -1;
        }
        else {
            dropTimer += Time.deltaTime / (snapTime / 2);

            if (dropTimer > 1) {
                //transform.parent = snapTo;
                grabbedTransform.position = targetPoint.position;
                grabbedTransform.rotation = targetPoint.rotation;
                moveToTarget = false;

                if (moveTowardsHand) {
                    attachedToHand = true;
                    moveTowardsHand = false;
                }
            }
            else {
                float t = Mathf.Pow(35, dropTimer);
                grabbedTransform.position = Vector3.Lerp(grabbedTransform.position, targetPoint.position, Time.fixedDeltaTime * t * 3);
                grabbedTransform.rotation = Quaternion.Slerp(grabbedTransform.rotation, targetPoint.rotation, Time.fixedDeltaTime * t * 2);

                if (moveTowardsHand) {
                    grabbedTransform.localScale = Vector3.Lerp(grabbedTransform.localScale, originalScale * 0.67f, Time.fixedDeltaTime * t * 3);
                }
                else {
                    grabbedTransform.localScale = Vector3.Lerp(grabbedTransform.localScale, originalScale, Time.fixedDeltaTime * t * 3);
                }
            }
        }
    }

    public void OnFocus(Hand hand) {
        Debug.Log("Telekinetic power entered object!");

        if (!focused) {
            focused = true;
            focusEffect.Play();

            pulseCoroutine = HapticFeedback(hand);
            StartCoroutine(pulseCoroutine);            
        }

        telekineticAttachmentPoint = hand.GetComponentInChildren<PsychicHand>().TelekinesisAttachmentPoint.transform;
    }

    public void OnUnfocus(Hand hand) {
        Debug.Log("Telekinetic power exited object!");

        if (focused) {
            focused = false;
            focusEffect.Stop();

            if (!grabbed) {
                StopCoroutine(pulseCoroutine);
            }
        }
    }

    public void OnGrab() {
        Debug.Log("Grabbed object via telekinesis!");
        pulseInterval = GrabPulseInterval;
        grid.DetachGridElement(ref gridElement);
        if (!grabbed) {
            grabEffect.Play();
        }

        grabbed = true;
    }

    public void OnPull() {
        //zoomButton.OnPointerClick(null);
        moveTowardsHand = true;
        moveWindowOnSphere = false;
        ChangeTargetPoint(telekineticAttachmentPoint);        
    }

    public void OnDrag(Transform pointer) {        
        if (moveWindowOnSphere) {
            SetTargetToClosestGridPoint(out selectedGridPoint, pointer.position);
        }

        if (moveTowardsHand) {
            ChangeTargetPoint(telekineticAttachmentPoint);
        }

        if (attachedToHand) {
            codeWindow.transform.position = telekineticAttachmentPoint.position;
            codeWindow.transform.rotation = telekineticAttachmentPoint.rotation;
        }
    }    

    public void OnRelease(Ray ray) {
        Debug.Log("Released telekinetically grabbed object!");
        pulseInterval = FocusPulseInterval;
        
        if (grabbed) {
            grabEffect.Stop();
            if (!focused) {
                StopCoroutine(pulseCoroutine);
            }
        }
        
        grabbed = false;
        moveTowardsHand = false;
        attachedToHand = false;
        moveWindowOnSphere = true;

        float radius = grid.screenSphere.GetComponent<SphereCollider>().radius * grid.screenSphere.transform.lossyScale.x;
        double t = PositionOnSphere.SphereIntersect(radius, grid.screenSphere.transform.position, ray.origin, ray.direction);
        Vector3 pointPosOnSphere = ray.origin + (float)t * ray.direction;

        GameObject pointObject = new GameObject("PointObject");
        pointObject.transform.position = pointPosOnSphere;

        SetTargetToClosestGridPoint(out selectedGridPoint, pointPosOnSphere);
        grid.AttachGridElement(ref gridElement, selectedGridPoint.LayerIdx, selectedGridPoint.ColumnIdx);
    }
    
    private IEnumerator HapticFeedback(Hand hand) {
        while (true) {
            if (hand != null) {
                float pulse = pulseIntensity;
                hand.TriggerHapticPulse((ushort)pulse);

                //SteamVR_Controller.Input( (int)trackedObject.index ).TriggerHapticPulse( (ushort)pulse );
            }

            float nextPulse = pulseInterval;

            yield return new WaitForSeconds(nextPulse);
        }
    }

    private void ChangeTargetPoint(Transform target) {
        // dropTimer = -1;
        moveToTarget = true;
        targetPoint = target;
    }
    private void ChangeTargetPoint(Vector3 position, Quaternion rotation) {
        // dropTimer = -1;
        moveToTarget = true;
        targetPoint.position = position;
        targetPoint.rotation = rotation;
    }

    private void SetTargetToClosestGridPoint(out SphereGridPoint sphereGridPoint, Vector3 pointOnWindowSphere) {
        Vector3 pointerPos = pointOnWindowSphere;

        sphereGridPoint = grid.GetClosestGridPoint(pointerPos);
        Vector3 previewPos = selectedGridPoint.AttachmentPoint;
        Vector3 lookDirection = previewPos - grid.screenSphere.transform.position;
        Quaternion previewRot = Quaternion.LookRotation(lookDirection);

        if ((codeWindow.transform.position - previewPos).magnitude > 0.1f) {
            ChangeTargetPoint(selectedGridPoint.AttachmentPointObject.transform);
        }
    }

}
