using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRVis.Spawner.File;

public class ZoomCodeWindowButton : MonoBehaviour, IPointerClickHandler {

    public float zoomDistance;
    public float speed;

    public Sprite ZoomIn;
    public Sprite ZoomOut;

    public static CodeWindowZoomEvent zoomInEvent = new CodeWindowZoomEvent();
    public static CodeWindowZoomEvent zoomOutEvent = new CodeWindowZoomEvent();
    public class CodeWindowZoomEvent : UnityEvent<CodeFileReferences> {};

    private bool zoomed = false;
    private bool zooming = false;
    private GameObject codeWindow;

    private Vector3 basePosition;
    private Vector3 zoomedPosition;

    private Image backgroundImage; 

    void Start() {
        codeWindow = gameObject.GetComponentInParent<CodeFileReferences>().gameObject;

        basePosition = codeWindow.transform.position;
        zoomedPosition = basePosition + zoomDistance * (-1) * codeWindow.transform.forward;

        if (!ZoomIn) {
            Debug.LogError("Zoom button is missing a zoom-in sprite!");
        }

        if (!ZoomOut) {
            Debug.LogError("Zoom button is missing a zoom-out sprite!");
        }

        backgroundImage = GetComponent<Image>();
        backgroundImage.sprite = ZoomIn;  
    }

    void Update() {
        float step = speed * Time.deltaTime;
        if (zooming) {
            if (!zoomed) {
                codeWindow.transform.position = Vector3.MoveTowards(codeWindow.transform.position, zoomedPosition, step);

                if (Vector3.Distance(codeWindow.transform.position, zoomedPosition) < 0.001f) {
                    zooming = false;
                    zoomed = true;
                    backgroundImage.sprite = ZoomOut;

                    zoomInEvent.Invoke(gameObject.GetComponent<CodeFileReferences>());
                }
            } 
            else {
                codeWindow.transform.position = Vector3.MoveTowards(codeWindow.transform.position, basePosition, step);

                if (Vector3.Distance(codeWindow.transform.position, basePosition) < 0.001f) {
                    zooming = false;
                    zoomed = false;
                    backgroundImage.sprite = ZoomIn;

                    zoomOutEvent.Invoke(gameObject.GetComponent<CodeFileReferences>());
                }
            }                    
        } else {
            Vector3 currentPosition = codeWindow.transform.position;

            if (Vector3.Distance(currentPosition, basePosition) > 0.001f
                && Vector3.Distance(currentPosition, zoomedPosition) > 0.001f) {

                zoomed = false;
                backgroundImage.sprite = ZoomIn;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData) {

        // reset code window location
        if (!zoomed) {
            basePosition = codeWindow.transform.position;
            zoomedPosition = basePosition + zoomDistance * (-1) * codeWindow.transform.forward;
        }

        zooming = true;
    }


}
