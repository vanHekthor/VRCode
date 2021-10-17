using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;
using VRVis.UI.UIElements;

public class HoloPadTutorialMain : MonoBehaviour {
    public Player player;
    public Hand leftHand;
    public Hand rightHand;
    public SteamVR_Action_Boolean laserAction;
    public SteamVR_Action_Boolean uiAction;
    public TutorialInfoScreen infoScreen;
    public TeleportPoint scenePortal;
    public GameObject holoPad;

    public enum TutorialState { open_holo_pad, holo_pad_info, switch_tabs, find_specific_tab, close_windows, next_scene_portal };

    public TutorialState State { get; set; }

    private const string TallestJavaBuildingName = "CatenaTest.java";
    private const string StripedBuilding = "Catena.java";
    private const string StripedBuildingAlternative = "Helper.java";

    public bool InvokedTeleportRecently { get; set; }
    private bool successfullyTeleported = false;
    private bool spawnedFile = false;

    private GameObject playerGameObject;
    private GameObject bodyCollider;

    private TabGroup holoPadTabGroup;

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

        State = TutorialState.open_holo_pad;

        Teleport.Player.AddListener(delegate { Teleported(); });
    }

    void Start() {
        playerGameObject = player.gameObject;
        scenePortal.gameObject.SetActive(false);
        
        holoPadTabGroup = holoPad.GetComponentInChildren<TabGroup>();
        if (holoPadTabGroup == null) {
            Debug.LogError("Holo pad tab group was not found!");
        }

        holoPadTabGroup.tabSelected.AddListener(TabSwitched);
        WristMenuController.onHoloPadOpened.AddListener(HoloPadWasOpened);
        //FileSpawner.GetInstance().onFileSpawned.AddListener(FileSpawned);
        //MoveCodeWindowButton.moveEvent.AddListener(MovedCodeWindow);
        //ZoomCodeWindowButton.zoomInEvent.AddListener(ZoomedIn);
        //ZoomCodeWindowButton.zoomOutEvent.AddListener(ZoomedOut);
        CloseCodeWindowButton.closeEvent.AddListener(ClosedWindow);
        // bodyCollider = player.gameObject.transform.Find("BodyCollider").gameObject;
    }


    void Update() {
        switch (State) {
            case TutorialState.open_holo_pad:
                OpenHoloPadIntroUpdate();
                break;
            case TutorialState.holo_pad_info:
                HoloPadInfoIntroUpdate();
                break;
            case TutorialState.switch_tabs:
                SwitchTabsIntroUpdate();
                break;
            case TutorialState.find_specific_tab:
                FindSpecificTabIntroUpdate();
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
    private bool holoPadWasOpened = false;
    private void OpenHoloPadIntroUpdate() {
        if (!infoScreenUpdated1) {
            infoScreen.ChangeTitle("Holo Pad 1");
            infoScreen.ChangeDescription("Press the red button on your left wrist like a real button!");
            infoScreenUpdated1 = true;
        }

        if (holoPadWasOpened) {
            State = TutorialState.holo_pad_info;
            holoPadWasOpened = false;
        }
    }

    private bool infoScreenUpdated2 = false;

    private void HoloPadInfoIntroUpdate() {
        if (!infoScreenUpdated2) {
            infoScreen.ChangeTitle("Holo Pad 2");
            infoScreen.ChangeDescription("The holo pad is the UI for triggering actions like config validation or performance prediction" +
                "and for defining app settings! <Press next>");
            infoScreenUpdated2 = true;
        }
    }


    private bool infoScreenUpdated3 = false;
    private bool switchedTab = false;
    private string currentTabTitle = "Configuration";
    private void SwitchTabsIntroUpdate() {
        if (!infoScreenUpdated3) {
            infoScreen.ChangeTitle("Holo Pad 3");
            infoScreen.ChangeDescription("Click a button on the right side of the holo pad to" +
                "switch to a different tab!");
            infoScreenUpdated3 = true;
        }

        if (switchedTab) {
            State = TutorialState.find_specific_tab;
            switchedTab = false;
        }
    }

    private bool infoScreenUpdated4 = false;
    string targetTab = "";
    private void FindSpecificTabIntroUpdate() {
        
        if (!infoScreenUpdated4) {
            infoScreen.ChangeTitle("Holo Pad 4");
            
            if (currentTabTitle != "Control Flow") {
                targetTab = "Control Flow";
            }
            else {
                targetTab = "Feature Regions";
            }
            infoScreen.ChangeDescription("Find the '" + targetTab + "' tab!");
            infoScreenUpdated4 = true;
        }

        if (currentTabTitle == targetTab) {
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

    private void HoloPadWasOpened() {
        holoPadWasOpened = true;
    }

    private void TabSwitched(string tabTitle) {
        if (State == TutorialState.switch_tabs) {
            switchedTab = true;
        }
        
        currentTabTitle = tabTitle;
    }

    private void ClosedWindow(CodeFileReferences codeFileRef) {
        if (State == TutorialState.close_windows) {
            closedWindowCount++;
        }
    }
}
