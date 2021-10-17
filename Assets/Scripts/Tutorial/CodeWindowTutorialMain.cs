using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;

public class CodeWindowTutorialMain : MonoBehaviour {

    public Player player;
    public Hand leftHand;
    public Hand rightHand;
    public SteamVR_Action_Boolean laserAction;
    public SteamVR_Action_Boolean uiAction;
    public TutorialInfoScreen infoScreen;
    public TeleportPoint scenePortal;

    public enum TutorialState { open_new_file, open_file_with_regions, move_windows, zoom_windows, close_windows, next_scene_portal };

    public TutorialState State { get; set; }

    private const string TallestJavaBuildingName = "CatenaTest.java";
    private const string StripedBuilding = "Catena.java";
    private const string StripedBuildingAlternative = "Helper.java";

    public bool InvokedTeleportRecently { get; set; }
    private bool successfullyTeleported = false;
    private bool spawnedFile = false;

    private GameObject playerGameObject;
    private GameObject bodyCollider;

    void Awake() {
        if (player == null) {
            Debug.LogError("Player was not defined in tutorial main script!");
        }

        if (leftHand == null) {
            Debug.LogError("Left hand was not defined in tutorial main script!");
        }

        if (rightHand == null) {
            Debug.LogError("Right hand was not defined in tutorial main script!");
        }

        if (laserAction == null) {
            Debug.LogError("Teleport action was not defined in tutorial main script!");
        }

        InvokedTeleportRecently = false;

        State = TutorialState.open_new_file;

        Teleport.Player.AddListener(delegate { Teleported(); });
    }

    void Start() {
        playerGameObject = player.gameObject;
        scenePortal.gameObject.SetActive(false);

        FileSpawner.GetInstance().onFileSpawned.AddListener(FileSpawned);
        MoveCodeWindowButton.moveEvent.AddListener(MovedCodeWindow);
        ZoomCodeWindowButton.zoomInEvent.AddListener(ZoomedIn);
        ZoomCodeWindowButton.zoomOutEvent.AddListener(ZoomedOut);
        CloseCodeWindowButton.closeEvent.AddListener(ClosedWindow);
        // bodyCollider = player.gameObject.transform.Find("BodyCollider").gameObject;
    }


    void Update() {
        switch (State) {
            case TutorialState.open_new_file:
                OpenNewFileIntroUpdate();
                break;
            case TutorialState.open_file_with_regions:
                OpenFileWithRegionsIntroUpdate();
                break;
            case TutorialState.move_windows:
                MoveCodeWindowsAroundIntroUpdate();
                break;
            case TutorialState.zoom_windows:
                ZoomCodeWindows();
                break;
            case TutorialState.close_windows:
                CloseWindows();
                break;

            case TutorialState.next_scene_portal:
                DisplayNextScenePortal();
                break;
        }
    }

    public void NextIntroTask() {
        State++;
    }

    public void PreviousIntroTask() {
        // State--;
    }

    public void WalkIntroFinished() {
        State = TutorialState.next_scene_portal;
    }

    private bool infoScreenUpdated1 = false;
    private void OpenNewFileIntroUpdate() {
        if (!infoScreenUpdated1) {
            infoScreen.ChangeTitle("Code Window 1");
            infoScreen.ChangeDescription("Grab the laser pointer (press 'B'), open a file and place it onto the window screen!");
            infoScreenUpdated1 = true;
        }

        if (spawnedFile) {
            State = TutorialState.open_file_with_regions;
            spawnedFile = false;
        }
    }

    private bool infoScreenUpdated2 = false;
    private bool spawnedStripedBuilding = false;
    private void OpenFileWithRegionsIntroUpdate() {
        if (!infoScreenUpdated2) {
            infoScreen.ChangeTitle("Code Window 2");
            infoScreen.ChangeDescription("Find the buildings with stripes on their sides. Open 'Catena.java'" +
                " or if it is already spawned open 'Helper.java'. Both are located at the table corner.");
            infoScreenUpdated2 = true;
        }

        if (spawnedStripedBuilding) {
            State = TutorialState.move_windows;
            spawnedStripedBuilding = false;
        }
    }


    private bool infoScreenUpdated3 = false;
    private bool movedWindow1 = false;
    private bool movedWindow2 = false;
    private void MoveCodeWindowsAroundIntroUpdate() {
        if (!infoScreenUpdated3) {
            infoScreen.ChangeTitle("Code Window 3");
            infoScreen.ChangeDescription("Move 'Catena.java' and the other file to different locations by dragging them using the title bars.");
            infoScreenUpdated3 = true;
        }

        if (movedWindow1 && movedWindow2) {
            State = TutorialState.zoom_windows;
            movedWindow1 = false;
            movedWindow2 = false;
        }
    }

    private bool infoScreenUpdated4 = false;
    private bool zoomedIn = false;
    private bool zoomedOut = false;
    private void ZoomCodeWindows() {
        if (!infoScreenUpdated4) {
            infoScreen.ChangeTitle("Code Window 4");
            infoScreen.ChangeDescription("Move a code window closer to you by using the zoom button each code window has." +
                " After that zoom out.");
            infoScreenUpdated4 = true;
        }

        if (zoomedIn && zoomedOut) {
            State = TutorialState.close_windows;
        }
    }

    private bool infoScreenUpdated5 = false;
    private bool closedAllWindows = false;
    private int closedWindowCount = 0;
    private void CloseWindows() {
        if (!infoScreenUpdated5) {
            infoScreen.ChangeTitle("Code Window 5");
            infoScreen.ChangeDescription("Close 2 code windows by pressing the close button on each window!");
            infoScreenUpdated5 = true;
        }

        if (closedWindowCount >= 2) {
            State = TutorialState.next_scene_portal;
        }
    }

    private bool infoScreenUpdated6 = false;
    private bool nextScenePortalIsVisible = false;
    private void DisplayNextScenePortal() {
        if (!infoScreenUpdated6) {
            infoScreen.ChangeTitle("Code Window 6");
            infoScreen.ChangeDescription("Teleport to the next task!");
            infoScreenUpdated5 = true;
        }

        if (!nextScenePortalIsVisible) {
            ASpawner s = ApplicationLoader.GetInstance().GetSpawner("CodeCityV1");
            if (s != null) {
                s.ShowVisualization(false);
            }

            nextScenePortalIsVisible = true;
            scenePortal.gameObject.SetActive(true);

        }

        Vector3 distToPlayer = Player.instance.hmdTransform.position - scenePortal.transform.position;
        distToPlayer.y = 0;

        if (successfullyTeleported) {
            if (distToPlayer.magnitude <= 0.1f) {
                SceneManager.LoadScene(3);
            }
            successfullyTeleported = false;
        }
    }

    private void Teleported() {
        successfullyTeleported = true;
    }

    private void FileSpawned(CodeFile codeFile) {
        spawnedFile = true;

        if (State == TutorialState.open_file_with_regions) {
            if (codeFile.GetNode().GetName() == StripedBuilding
                || codeFile.GetNode().GetName() == StripedBuildingAlternative) {
                spawnedStripedBuilding = true;
            }
        }
    }

    private void MovedCodeWindow(CodeFileReferences codeFileRef) {
        if (codeFileRef.GetCodeFile().GetNode().GetName() == StripedBuilding) {
            movedWindow1 = true;
        }
        if (codeFileRef.GetCodeFile().GetNode().GetName() != StripedBuilding) {
            movedWindow2 = true;
        }

    }

    private void ZoomedIn(CodeFileReferences codeFileRef) {
        zoomedIn = true;
    }

    private void ZoomedOut(CodeFileReferences codeFileRef) {
        zoomedOut = true;
    }

    private void ClosedWindow (CodeFileReferences codeFileRef) {
        if (State == TutorialState.close_windows) {
            closedWindowCount++;
        }
    }
}
