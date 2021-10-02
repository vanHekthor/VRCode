using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.Spawner.File;
using VRVis.UI.Helper;

public class MoveCodeWindowButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {

    private GameObject codeWindow;

    private SphereGrid grid;
    private GridElement gridElement;

    private SphereGridPoint selectedGridPoint;

    private bool spawnWindowOntoSphere;

    private bool pressed = false;

    void Start() {
        codeWindow = GetComponentInParent<CodeFileReferences>().gameObject;
        gridElement = codeWindow.GetComponent<GridElement>();
        grid = gridElement.Grid;

        if (grid != null) {
            spawnWindowOntoSphere = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        pressed = true;
        grid.DetachGridElement(ref gridElement);
    }

    public void OnPointerUp(PointerEventData eventData) {
        pressed = false;
        grid.AttachGridElement(ref gridElement, selectedGridPoint.LayerIdx, selectedGridPoint.ColumnIdx);
    }

    /// <summary>
    /// Positions the code window according to the location the user points to.<para/>
    /// Depending on the existence on a grid and/or a window sphere different positioning logics (TODO) are used. 
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData) {

        if (pressed) {
            if (spawnWindowOntoSphere) {                
                Vector3 pointerPos = eventData.pointerCurrentRaycast.worldPosition;

                if (Vector3.Distance(new Vector3(0, 0, 0), pointerPos) > 0.001f) {
                    selectedGridPoint = grid.GetClosestGridPoint(pointerPos);
                    Vector3 previewPos = selectedGridPoint.AttachmentPoint;
                    Vector3 lookDirection = previewPos - grid.screenSphere.transform.position;
                    Quaternion previewRot = Quaternion.LookRotation(lookDirection);

                    codeWindow.transform.position = previewPos;
                    codeWindow.transform.rotation = previewRot;
                }
            }
        }

    }

    

}
