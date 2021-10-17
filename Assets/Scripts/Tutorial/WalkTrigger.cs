using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkTrigger : MonoBehaviour {

    public WalkIntro walkIntroReference;

    public enum WalkPointState { unactivated, activated };

    public bool Activated { get; private set; }
    public WalkPointState State { get; private set; }

    private GameObject frame;
    private GameObject ground;

    void Start() {
        if (walkIntroReference == null) {
            Debug.LogError("Reference to Walk Intro is missing!");
        }

        walkIntroReference.Subscribe(this);

        State = WalkPointState.unactivated;

        frame = gameObject.transform.Find("Frame").gameObject;
        ground = gameObject.transform.Find("Ground").gameObject;        
    }

    public void Activate() {
        Activated = true;
        ChangeState(WalkPointState.activated);
        walkIntroReference.TriggerActivated();
    }

    public void ChangeState(WalkPointState state) {
        State = state;

        if (State == WalkPointState.activated) {
            frame.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0, 0.7932f, 0.828f, 0.5529f));
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.name == "BodyCollider") {
            Activated = true;
            ChangeState(WalkPointState.activated);
        }
    }    
}