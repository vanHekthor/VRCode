using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.IO;
using VRVis.Spawner;

public class CodeCityTutorialMain : MonoBehaviour {

    public Player player;
    public Hand leftHand;
    public Hand rightHand;
    public SteamVR_Action_Boolean laserAction;
    public SteamVR_Action_Boolean uiAction;
    public TutorialInfoScreen infoScreen;
    public TeleportPoint scenePortal;

    public enum TutorialState { grab_laser_pointer, hover_over_file, open_file, find_tallest_java_building, next_scene_portal };

    public TutorialState State { get; set; }

    private const string TallestJavaBuildingName = "CatenaTest.java";

    public bool InvokedTeleportRecently { get; set; }
    private bool successfullyTeleported = false;
    private bool spawnedFile = false;

    private bool grabbedLaserPointer = false;

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

        State = TutorialState.grab_laser_pointer;

        Teleport.Player.AddListener(delegate { Teleported(); });
    }

    void Start() {
        playerGameObject = player.gameObject;
        scenePortal.gameObject.SetActive(false);

        FileSpawner.GetInstance().onFileSpawned.AddListener(FileSpawned);
        // bodyCollider = player.gameObject.transform.Find("BodyCollider").gameObject;
    }


    void Update() {
        switch (State) {
            case TutorialState.grab_laser_pointer:
                GrabLaserPointerIntroUpdate();
                break;
            case TutorialState.hover_over_file:
                HoverIntroUpdate();
                break;
            case TutorialState.open_file:
                OpenFileIntroUpdate();
                break;
            case TutorialState.find_tallest_java_building:
                FindTallestJavaBuildingUpdate();
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

    public void LaserPointerGrabbed() {
        grabbedLaserPointer = true;
    }

    public void WalkIntroFinished() {
        State = TutorialState.next_scene_portal;
    }

    private bool infoScreenUpdated1 = false;
    private void GrabLaserPointerIntroUpdate() {
        if (!infoScreenUpdated1) {
            infoScreen.ChangeTitle("Code City 1");
            infoScreen.ChangeDescription("Press the B-Button on your right controller and select the laser pointer!");
            infoScreenUpdated1 = true;
        }

        bool isShowingHint = !string.IsNullOrEmpty(ControllerButtonHints.GetActiveHintText(rightHand, laserAction));

        if (!isShowingHint) {
            ControllerButtonHints.ShowTextHint(rightHand, laserAction, "Grab laser pointer");
        }

        if (grabbedLaserPointer) {
            ControllerButtonHints.HideTextHint(rightHand, laserAction);
            State = TutorialState.hover_over_file;
        }
    }

    private bool infoScreenUpdated2 = false;
    private void HoverIntroUpdate() {
        if (!infoScreenUpdated2) {
            infoScreen.ChangeTitle("Code City 2");
            infoScreen.ChangeDescription("Each building of the city represents a class and packages are districts.\n" +
                "Hover over a building to see further information.");
            infoScreenUpdated2 = true;
        }
    }


    private bool infoScreenUpdated3 = false;
    private bool isShowingInteractionHint = false;
    private void OpenFileIntroUpdate() {
        if (!infoScreenUpdated3) {
            infoScreen.ChangeTitle("Code City 3");
            infoScreen.ChangeDescription("Open a file by pointing at a building and pressing the A-Button." +
                "Then choose a point on the window grid and press 'A' again.");
            infoScreenUpdated3 = true;
        }

        if (!isShowingInteractionHint) {
            ControllerButtonHints.ShowTextHint(rightHand, uiAction, "UI Interaction");
        }

        if (spawnedFile) {
            ControllerButtonHints.HideTextHint(rightHand, uiAction);
            spawnedFile = false;
            State = TutorialState.find_tallest_java_building;
        }
    }

    private bool infoScreenUpdated4 = false;
    private bool spawnedHighestJavaBuilding = false;
    private void FindTallestJavaBuildingUpdate() {
        if (!infoScreenUpdated4) {
            infoScreen.ChangeTitle("Code City 4");
            infoScreen.ChangeDescription("Find the java file with the most lines and put it onto the window grid.");
            infoScreenUpdated4 = true;
        }

        if (spawnedHighestJavaBuilding) {
            State = TutorialState.next_scene_portal;
        }
    }

    private bool infoScreenUpdated5 = false;
    private bool nextScenePortalIsVisible = false;
    private void DisplayNextScenePortal() {
        if (!infoScreenUpdated5) {
            infoScreen.ChangeTitle("Code City 5");
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
                SceneManager.LoadScene(2);
            }
            successfullyTeleported = false;
        }
    }

    private void Teleported() {
        successfullyTeleported = true;
    }

    private void FileSpawned(CodeFile codeFile) {
        spawnedFile = true;
        
        if (codeFile.GetNode().GetName() == TallestJavaBuildingName) {
            spawnedHighestJavaBuilding = true;
        }
    }
}
