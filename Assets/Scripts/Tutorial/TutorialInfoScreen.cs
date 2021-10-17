using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialInfoScreen : MonoBehaviour {

    public TextMeshProUGUI title;
    public TextMeshProUGUI description;

    void Start() {
        if (title == null) {
            Debug.LogError("Reference to TextMeshPro UGUI component for info screen title is missing!" +
                "Probably needs to be assigned in Unity Editor.");
        }

        if (description == null) {
            Debug.LogError("Reference to TextMeshPro UGUI component for info screen description is missing!" +
                "Probably needs to be assigned in Unity Editor.");
        }
    }

    public void ChangeTitle(string text) {
        title.text = text;
    }

    public void ChangeDescription(string text) {
        description.text = text;
    }
}
