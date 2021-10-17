using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WalkIntro : MonoBehaviour {

    public UnityEvent introFinished;

    private List<WalkTrigger> walkTriggers;

    public void Subscribe(WalkTrigger walkTrigger) {
        if (walkTriggers == null) {
            walkTriggers = new List<WalkTrigger>();
        }

        walkTriggers.Add(walkTrigger);
    }
    public void TriggerActivated() {
        bool introFinished = true;
        foreach (var trigger in walkTriggers) {
            if (!trigger.Activated) {
                introFinished = false;
            }
        }

        if (introFinished) {
            this.introFinished.Invoke();
        }
    }
}
