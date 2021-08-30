using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;
using VRVis.UI.Helper;

public class CodeWindowLinkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {

    public GameObject SpawnPanel;
    public GameObject WindowPreview;

    public Sprite HighlightSprite;
    public Sprite DefaultSprite;

    public string TargetFilePath { get; set; }
    public CodeFile TargetFile { get; set; }
    public GameObject AnchorObject { get; set; }

    private const int SpawnLeft = 0;
    private const int SpawnRight = 1;
    private const int SpawnFartherAway = 2;

    private bool windowSpawning = false;
    private bool windowSpawned = false;

    private int spawnSide;
    private GameObject instantiatedSpawnPanel;
    private GameObject instantiatedPreview;
    private Vector3 spawnPosition;

    private bool pressed;
    private bool spawnPanelVisible = false;

    private FileSpawner fs;
    private SphereGrid grid;
    private GridElement gridElement;
    private bool spawnWindowOntoSphere;

    private Transform toggleCenter;
    private Transform toggleLeft;
    private Transform toggleRight;

    private Image spawnLeftImage;
    private Image spawnRightImage;


    void Awake() {
        spawnSide = SpawnLeft;        

        fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        grid = fs.WindowScreen.GetComponent<SphereGrid>();
        if (fs.WindowScreen != null) {
            spawnWindowOntoSphere = true;
        }

        toggleCenter = transform.Find("Center");
        toggleLeft = transform.Find("ToggleLeft");
        toggleRight = transform.Find("ToggleRight");
    }
    
    /// <summary>
    /// Shows a spawn panel in front of the link button for indicating the side to which the user wants to spawn a linked code file.
    /// The side the user chooses is determined by a drag motion that is handled in the OnDrag method. 
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData) {
        pressed = true;

        gridElement = AnchorObject.GetComponent<GridElement>();

        if (!spawnPanelVisible) {
            spawnPanelVisible = true;
            instantiatedSpawnPanel = Instantiate(SpawnPanel, toggleCenter);
        }

        spawnLeftImage = instantiatedSpawnPanel.transform.Find("Container/LeftButton").GetComponent<Image>();
        spawnRightImage = instantiatedSpawnPanel.transform.Find("Container/RightButton").GetComponent<Image>();
    }

    /// <summary>
    /// When releasing the button the spawn panel gets destroyed and the chosen spawn position is used to spawn the linked code file.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData) {
        pressed = false;

        if (spawnPanelVisible) {
            spawnPanelVisible = false;
            DestroyImmediate(instantiatedSpawnPanel);            
        }        
        
        StartCoroutine(SpawnFileCoroutine());
    }

    /// <summary>
    /// <para>Determines where the linked code file should be spawned based on the drag motion after pressing the link button.</para>
    /// <para>When making a small motions to the left a neighboring location in the window grid to the left side of
    /// the current base code file gets chosen. Vice versa when making a small motion to the right.</para>
    /// <para>When dragging even further the user can freely choose an unoccupied place in the window grid by just pointing to it.</para>
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData) {
        if (pressed) {
            // Debug.Log("Currently dragging...");

            if (spawnWindowOntoSphere) {               

                Vector3 pointerPos = eventData.pointerCurrentRaycast.worldPosition;

                if (Vector3.Distance(pointerPos, Vector3.zero) >= 0.001f) {

                    Vector3 spawnVector = pointerPos - toggleCenter.position;
                    Vector3 baseFileToPointer = pointerPos
                        - grid.GetGridPoint(gridElement.GridPositionLayer, gridElement.GridPositionColumn).Position;

                    float distanceToLeft = (pointerPos - toggleLeft.position).magnitude;
                    float distanceToRight = (pointerPos - toggleRight.position).magnitude;

                    if (baseFileToPointer.magnitude <= 17.0) {

                        if (spawnSide == SpawnFartherAway) {
                            if (instantiatedPreview) {
                                // DestroyImmediate(instantiatedPreview);
                                instantiatedPreview.SetActive(false);
                            }
                        }

                        if (distanceToLeft <= distanceToRight) {
                            spawnSide = SpawnLeft;
                            spawnLeftImage.sprite = HighlightSprite;
                            spawnRightImage.sprite = DefaultSprite;
                        }
                        else {
                            spawnSide = SpawnRight;
                            spawnLeftImage.sprite = DefaultSprite;
                            spawnRightImage.sprite = HighlightSprite;
                        }
                    }
                    else {
                        if (spawnSide != SpawnFartherAway) {
                            if (instantiatedPreview) {
                                instantiatedPreview.SetActive(true);
                            }
                        }

                        Vector3 previewPos = grid.GetClosestGridPoint(pointerPos).AttachmentPoint;
                        Vector3 lookDirection = previewPos - fs.WindowScreenTransform.position;
                        Quaternion previewRot = Quaternion.LookRotation(lookDirection);

                        if (!instantiatedPreview) {
                            instantiatedPreview = Instantiate(WindowPreview, previewPos, Quaternion.LookRotation(lookDirection));
                        }
                        else {
                            instantiatedPreview.transform.position = previewPos;
                            instantiatedPreview.transform.rotation = previewRot;
                        }

                        spawnSide = SpawnFartherAway;

                    }
                }
            }
        }
    }



    private IEnumerator SpawnFileCoroutine() {

        // get full file path from relative one
        Debug.Log("Spawning window: " + TargetFilePath);
        if (TargetFile == null) {
            Debug.LogError("Failed to spawn code window '" + TargetFile + " - file not found!");
        }

        // spawn window
        windowSpawning = true;

        if (fs) {

            if (spawnSide == SpawnLeft) {
                fs.SpawnFileNextTo(TargetFile, AnchorObject.GetComponent<CodeFileReferences>(), true, WindowSpawnedCallback);
            }
            else if (spawnSide == SpawnRight) {
                fs.SpawnFileNextTo(TargetFile, AnchorObject.GetComponent<CodeFileReferences>(), false, WindowSpawnedCallback);
            }
            else if (spawnSide == SpawnFartherAway) {
                fs.SpawnFile(
                    TargetFile.GetNode(), 
                    instantiatedPreview.transform.position, 
                    instantiatedPreview.transform.rotation, 
                    WindowSpawnedCallback);
                
                if (instantiatedPreview) {
                    DestroyImmediate(instantiatedPreview);
                }
            }

        
        }
        else {
            WindowSpawnedCallback(false, null, "Missing FileSpawner!");
        }

        // wait until spawning is finished
        yield return new WaitUntil(() => windowSpawning == false);
    }

    /// <summary>
    /// Called after the window placement finished.
    /// </summary>
    private void WindowSpawnedCallback(bool success, CodeFile file, string msg) {

        windowSpawned = success;
        windowSpawning = false;

        if (!success) {
            string name = "";
            if (file != null && file.GetNode() != null) { name = "(" + file.GetNode().GetName() + ") "; }
            Debug.LogError("Failed to place window! " + name + msg);
            return;
        }
    }
    
}
