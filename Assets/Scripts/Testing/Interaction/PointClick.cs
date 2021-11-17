using DiffMatchPatch;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.IO.Features;

public class PointClick : MonoBehaviour, IPointerClickHandler {

    public GameObject testObject;

    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log("PointClick Component was clicked!");

        StuffDoneWhenClicked();
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    private void StuffDoneWhenClicked() {
        diff_match_patch dmp = new diff_match_patch();

        List<Diff> diff = dmp.diff_lineMode(
            "I am the very model of a modern Major-General,"+ Environment.NewLine +
            "I've information vegetable, animal, and mineral," + Environment.NewLine +
            "I know the kings of England, and I quote the fights historical," + Environment.NewLine +
            "From Marathon to Waterloo, in order categorical.",

            "I am the very model of a modern Major-General," + Environment.NewLine +
            "I know the kings of England, and I quote the fights historical," + Environment.NewLine +
            "Lari faur krum ipsum asfk fhe apeghfg dbisf  aufg" + Environment.NewLine +
            "From Marathon to Waterloo, in order categorical."
            );
        // Result: [(-1, "Hell"), (1, "G"), (0, "o"), (1, "odbye"), (0, " World.")]
        dmp.diff_cleanupSemantic(diff);
        // Result: [(-1, "Hello"), (1, "Goodbye"), (0, " World.")]
        for (int i = 0; i < diff.Count; i++) {
            Debug.Log(diff[i]);
        }
    }
}
