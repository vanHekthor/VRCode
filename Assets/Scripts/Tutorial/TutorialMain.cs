using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class TutorialMain : MonoBehaviour {

    public Player player;
    public Hand leftHand;
    public Hand rightHand;
    public SteamVR_Action_Boolean teleportAction;
    public SteamVR_Action_Vector2 walkAction;
    public TutorialInfoScreen infoScreen;
    public WalkIntro walkIntro;
    public TeleportPoint scenePortal;

    public enum TutorialState { teleport_intro_1, teleport_intro_2, teleport_intro_3, teleport_intro_4, walk_intro, next_scene_portal };

    public TutorialState State { get; set; }

    public Transform teleportLeft;
    public Transform teleportRight;
    public Transform teleportScene;

    public bool InvokedTeleportRecently { get; set; }
    private bool successfullyTeleported = false;

    private Walk walkController;

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

        if (teleportAction == null) {
            Debug.LogError("Teleport action was not defined in tutorial main script!");
        }        
        
        if (teleportLeft == null) {
            Debug.LogError("Left teleport point was not found!");
        }
        
        if (teleportRight == null) {
            Debug.LogError("Right teleport point was not found!");
        }

        InvokedTeleportRecently = false;

        State = TutorialState.teleport_intro_1;

        Teleport.Player.AddListener(delegate { Teleported(); });

    }

    void Start() {
        playerGameObject = player.gameObject;
        walkIntro.gameObject.SetActive(false);
        scenePortal.gameObject.SetActive(false);

        walkController = Player.instance.GetComponent<Walk>();
        // bodyCollider = player.gameObject.transform.Find("BodyCollider").gameObject;
    }


    void Update() {
        switch (State) {
            case TutorialState.teleport_intro_1:
                TeleportIntro1Update();
                break;
            case TutorialState.teleport_intro_2:
                TeleportIntro2Update();
                break;
            case TutorialState.teleport_intro_3:
                TeleportIntro3Update();
                break;
            case TutorialState.teleport_intro_4:
                TeleportIntro4Update();
                break;
            case TutorialState.walk_intro:
                WalkIntroUpdate();
                break;
            case TutorialState.next_scene_portal:
                DisplayNextScenePortal();
                break;
        }
    }

    public void TeleportIntroFinished() {

    }

    public void WalkIntroFinished() {
        walkIntroFinished = true;
    }

    private bool infoScreenUpdated1 = false;
    private void TeleportIntro1Update() {
        if (!infoScreenUpdated1) {
            infoScreen.ChangeTitle("Teleport 1");
            infoScreen.ChangeDescription("Teleport to the left teleport point.");
            infoScreenUpdated1 = true;
        }

        bool isShowingHint = !string.IsNullOrEmpty(ControllerButtonHints.GetActiveHintText(leftHand, teleportAction));
        if (!isShowingHint) {
            ControllerButtonHints.ShowTextHint(leftHand, teleportAction, "Teleport");
        }

        Vector3 distToPlayer = Player.instance.hmdTransform.position - teleportLeft.position;
        distToPlayer.y = 0;
        
        if (successfullyTeleported) {
            if (teleportAction.activeDevice == SteamVR_Input_Sources.LeftHand) {
                if (distToPlayer.magnitude <= 0.1f) {
                    ControllerButtonHints.HideTextHint(leftHand, teleportAction);
                    State = TutorialState.teleport_intro_2;
                }
            }
            successfullyTeleported = false;
        }        
    }

    private bool infoScreenUpdated2 = false;
    private void TeleportIntro2Update() {
        if (!infoScreenUpdated2) {
            infoScreen.ChangeTitle("Teleport 2");
            infoScreen.ChangeDescription("Teleport to the other teleport point on the right.");
            infoScreenUpdated2 = true;
        }

        bool isShowingHint = !string.IsNullOrEmpty(ControllerButtonHints.GetActiveHintText(rightHand, teleportAction));
        if (!isShowingHint) {
            ControllerButtonHints.ShowTextHint(rightHand, teleportAction, "Teleport");
        }

        Vector3 distToPlayer = Player.instance.hmdTransform.position - teleportRight.position;
        distToPlayer.y = 0;

        if (successfullyTeleported) {
            if (teleportAction.activeDevice == SteamVR_Input_Sources.RightHand) {          
                if (distToPlayer.magnitude <= 0.1f) {
                    ControllerButtonHints.HideTextHint(rightHand, teleportAction);
                    State = TutorialState.teleport_intro_3;
                }
            }
            successfullyTeleported = false;
        }
    }

    private bool infoScreenUpdated3 = false;
    private bool teleportedToSomewhere = false;
    private void TeleportIntro3Update() {
        if (!infoScreenUpdated3) {
            infoScreen.ChangeTitle("Teleport 3");
            infoScreen.ChangeDescription("Teleport anywhere you want.");
            infoScreenUpdated3 = true;
        }

        if (!teleportedToSomewhere) {
            if (successfullyTeleported) { 
                teleportedToSomewhere = true;
                successfullyTeleported = false;
            }
            return;
        }

        State = TutorialState.teleport_intro_4;
    }

    private bool infoScreenUpdated4 = false;
    private bool teleportedBack = false;
    private void TeleportIntro4Update() {
        if (!infoScreenUpdated4) {
            infoScreen.ChangeTitle("Teleport 4");
            infoScreen.ChangeDescription("Teleport back to the center.");
            infoScreenUpdated4 = true;
        }

        if (!teleportedBack) {
            Vector3 distToCenter = Player.instance.hmdTransform.position - Vector3.zero;
            distToCenter.y = 0;

            if (successfullyTeleported) {
                if (distToCenter.magnitude < 2) {
                    teleportedBack = true;
                }
                successfullyTeleported = false;
            }

            return;
        }

        State = TutorialState.walk_intro;
    }

    private bool infoScreenUpdated5 = false;
    private bool hasWalked = false;
    private bool walkIntroFinished = false;
    private void WalkIntroUpdate() {
        if (!infoScreenUpdated5) {
            teleportLeft.gameObject.SetActive(false);
            teleportRight.gameObject.SetActive(false);

            infoScreen.ChangeTitle("Walk Intro");
            infoScreen.ChangeDescription("Walk through the walk triggers using the thumb stick on your left controller!");
            infoScreenUpdated5 = true;

            walkIntro.gameObject.SetActive(true);
        }

        if (!hasWalked) {
            bool isShowingHint = !string.IsNullOrEmpty(ControllerButtonHints.GetActiveHintText(leftHand, walkAction));
            if (!isShowingHint) {
                ControllerButtonHints.ShowTextHint(leftHand, walkAction, "Walk");
            }

            if (walkController.IsWalking) {
                ControllerButtonHints.HideTextHint(leftHand, walkAction);
                hasWalked = true;
            }
        }

        // check if all walk triggers have been activated happens in the WalkIntro script
        if (walkIntroFinished) {
            State = TutorialState.next_scene_portal;
        }
    }

    private bool nextScenePortalIsVisible = false;
    private void DisplayNextScenePortal() {
        if (!nextScenePortalIsVisible) {
            nextScenePortalIsVisible = true;
            scenePortal.gameObject.SetActive(true);
        }

        Vector3 distToPlayer = Player.instance.hmdTransform.position - teleportScene.position;
        distToPlayer.y = 0;

        if (successfullyTeleported) {
            if (distToPlayer.magnitude <= 0.1f) {
                SceneManager.LoadScene(1);
            }
            successfullyTeleported = false;
        }
    }

    private void Teleported() {
        successfullyTeleported = true;
    }
}
