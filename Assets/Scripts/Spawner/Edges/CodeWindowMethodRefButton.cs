using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.Edges;
using VRVis.Spawner.File;
using VRVis.UI.Helper;

public class CodeWindowMethodRefButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerClickHandler {

    public GameObject SpawnPanel;
    public GameObject WindowPreview;

    public Sprite HighlightSprite;
    public Sprite DefaultSprite;

    public Transform ButtonBody;
    public TextMeshProUGUI RefButtonTextElement;

    public class RefClickedEvent : UnityEvent<List<CodeWindowMethodRef>> { };
    public static RefClickedEvent RefClicked = new RefClickedEvent();

    public string CallingFilePath { get; private set; }
    public CodeFile CallingFile { get; private set; }
    public GameObject DeclarationCodeWindowObject { get; set; }
    public CodeWindowMethodRef Ref { get; set; }

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

    private string sceneName;

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

        // Create a temporary reference to the current scene.
        Scene currentScene = SceneManager.GetActiveScene();

        // Retrieve the name of this scene.
        sceneName = currentScene.name;

    }

    void Start() {
        CallingFilePath = Ref.RefEdge.GetTo().file.ToLower();
        CallingFile = Ref.CallingFile;
    }

    public void ChangeRefButtonText(string text) {
        RefButtonTextElement.SetText(text);
    }

    public float GetButtonWidth() {
        // here x-scale is the width
        return ButtonBody.lossyScale.x;
    }

    /// <summary>
    /// Shows a spawn panel in front of the link button for indicating the side to which the user wants to spawn a linked code file.
    /// The side the user chooses is determined by a drag motion that is handled in the OnDrag method. 
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData) {
        //pressed = true;

        //gridElement = BaseCodeWindowObject.GetComponent<GridElement>();

        //if (!spawnPanelVisible) {
        //    spawnPanelVisible = true;
        //    instantiatedSpawnPanel = Instantiate(SpawnPanel, toggleCenter);
        //}

        //spawnLeftImage = instantiatedSpawnPanel.transform.Find("Container/LeftButton").GetComponent<Image>();
        //spawnRightImage = instantiatedSpawnPanel.transform.Find("Container/RightButton").GetComponent<Image>();
    }

    /// <summary>
    /// When releasing the button the spawn panel gets destroyed and the chosen spawn position is used to spawn the linked code file.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData) {
        //pressed = false;

        //if (spawnPanelVisible) {
        //    spawnPanelVisible = false;
        //    DestroyImmediate(instantiatedSpawnPanel);            
        //}        

        //StartCoroutine(SpawnFileCoroutine());
    }

    /// <summary>
    /// <para>Determines where the linked code file should be spawned based on the drag motion after pressing the link button.</para>
    /// <para>When making a small motions to the left a neighboring location in the window grid to the left side of
    /// the current base code file gets chosen. Vice versa when making a small motion to the right.</para>
    /// <para>When dragging even further the user can freely choose an unoccupied place in the window grid by just pointing to it.</para>
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData) {
        //if (pressed) {
        //    // Debug.Log("Currently dragging...");

        //    if (spawnWindowOntoSphere) {               

        //        Vector3 pointerPos = eventData.pointerCurrentRaycast.worldPosition;

        //        if (Vector3.Distance(pointerPos, Vector3.zero) >= 0.001f) {

        //            Vector3 spawnVector = pointerPos - toggleCenter.position;
        //            Vector3 baseFileToPointer = pointerPos
        //                - grid.GetGridPoint(gridElement.GridPositionLayer, gridElement.GridPositionColumn).Position;

        //            float distanceToLeft = (pointerPos - toggleLeft.position).magnitude;
        //            float distanceToRight = (pointerPos - toggleRight.position).magnitude;

        //            if (baseFileToPointer.magnitude <= 17.0) {

        //                if (spawnSide == SpawnFartherAway) {
        //                    if (instantiatedPreview) {
        //                        // DestroyImmediate(instantiatedPreview);
        //                        instantiatedPreview.SetActive(false);
        //                    }
        //                }

        //                if (distanceToLeft <= distanceToRight) {
        //                    spawnSide = SpawnLeft;
        //                    spawnLeftImage.sprite = HighlightSprite;
        //                    spawnRightImage.sprite = DefaultSprite;
        //                }
        //                else {
        //                    spawnSide = SpawnRight;
        //                    spawnLeftImage.sprite = DefaultSprite;
        //                    spawnRightImage.sprite = HighlightSprite;
        //                }
        //            }
        //            else {
        //                if (spawnSide != SpawnFartherAway) {
        //                    if (instantiatedPreview) {
        //                        instantiatedPreview.SetActive(true);
        //                    }
        //                }

        //                Vector3 previewPos = grid.GetClosestGridPoint(pointerPos).AttachmentPoint;
        //                Vector3 lookDirection = previewPos - fs.WindowScreenTransform.position;
        //                Quaternion previewRot = Quaternion.LookRotation(lookDirection);

        //                if (!instantiatedPreview) {
        //                    instantiatedPreview = Instantiate(WindowPreview, previewPos, Quaternion.LookRotation(lookDirection));
        //                }
        //                else {
        //                    instantiatedPreview.transform.position = previewPos;
        //                    instantiatedPreview.transform.rotation = previewRot;
        //                }

        //                spawnSide = SpawnFartherAway;
        //            }
        //        }
        //    }
        //}
    }

    public void OnPointerClick(PointerEventData eventData) {
        //if (sceneName == "ExampleScene_Catena_NoVR") {
        //    Debug.Log("LinkButton was clicked!");
        //    // OnPointerUp(eventData);
        //    var links = new List<CodeWindowLink>();
        //    links.Add(Link);
        //    LinkClicked.Invoke(links);
        //}
        Debug.Log("RefButton was clicked!");
        // OnPointerUp(eventData);

        WasClicked();
    }

    public void WasClicked() {
        var refs = new List<CodeWindowMethodRef>();
        refs.Add(Ref);
        RefClicked.Invoke(refs);
    }


    private IEnumerator SpawnFileCoroutine() {

        // get full file path from relative one
        Debug.Log("Spawning window: " + CallingFilePath);
        if (CallingFile == null) {
            Debug.LogError("Failed to spawn code window '" + CallingFile + " - file not found!");
        }

        // spawn window
        windowSpawning = true;

        if (fs) {

            if (spawnSide == SpawnLeft) {
                fs.SpawnFileNextTo(CallingFile, Ref.DeclarationFileInstance.Config.Name, DeclarationCodeWindowObject.GetComponent<CodeFileReferences>(), true, FileSpawnCallback);
            }
            else if (spawnSide == SpawnRight) {
                fs.SpawnFileNextTo(CallingFile, Ref.DeclarationFileInstance.Config.Name, DeclarationCodeWindowObject.GetComponent<CodeFileReferences>(), false, FileSpawnCallback);
            }
            else if (spawnSide == SpawnFartherAway) {
                fs.SpawnFile(
                    CallingFile.GetNode(),
                    instantiatedPreview.transform.position,
                    instantiatedPreview.transform.rotation,
                    FileSpawnCallback);

                if (instantiatedPreview) {
                    DestroyImmediate(instantiatedPreview);
                }
            }

        }
        else {
            FileSpawnCallback(false, null, "Missing FileSpawner!");
        }

        // wait until spawning is finished
        yield return new WaitUntil(() => windowSpawning == false);
    }

    /// <summary>
    /// Called after the window placement finished.
    /// </summary>
    private void FileSpawnCallback(bool success, CodeFileReferences fileInstance, string msg) {

        windowSpawned = success;
        windowSpawning = false;

        var edgeConnection = SpawnEdgeConnection(Ref.DeclarationFileInstance, fileInstance);

        edgeConnection.TargetMethodHighlight = HighlightCodeAreaInFileInstance(edgeConnection.GetStartCodeFileInstance(), Ref.RefEdge.GetTo().lines.from, Ref.RefEdge.GetTo().lines.to);

        if (!success) {
            string name = "";
            if (fileInstance != null && fileInstance.GetCodeFile().GetNode() != null) { name = "(" + fileInstance.GetCodeFile().GetNode().GetName() + ") "; }
            Debug.LogError("Failed to place window! " + name + msg);
            return;
        }
    }

    private CodeWindowEdgeConnection SpawnEdgeConnection(CodeFileReferences startFileInstance, CodeFileReferences endFileInstance) {
        var edgeConnection = fs.edgeSpawner.SpawnSingleEdgeConnection(Ref.DeclarationFileInstance, endFileInstance, Ref.RefEdge);

        return edgeConnection;
    }

    private LineHighlight HighlightCodeAreaInFileInstance(CodeFileReferences fileInstance, int startLine, int endLine) {
        var highlight = fileInstance.SpawnMethodHighlight(startLine, endLine);
        if (highlight == null) {
            Debug.LogError("Could not highlight the lines " + startLine + " to " +
                endLine + " inside code window for " + CallingFile.GetNode().GetName());
        }

        return highlight;
    }
}
