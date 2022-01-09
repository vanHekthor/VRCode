using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRVis.Spawner.Edges;

public class CodePopup : MonoBehaviour {

    public Transform classNameTransform;
    public Transform methodDeclarationTransform;

    void Awake() {
        classNameTransform = transform.Find("ClassName");
        if (classNameTransform == null) {
            Debug.LogError("Code popup is missing a class name object called 'ClassName' to display the class names!");
        }

        methodDeclarationTransform = transform.Find("MethodDeclaration");
        if (methodDeclarationTransform == null) {
            Debug.LogError("Code popup is missing a method declaration object called 'MethodDeclaration' to display the method declarations'!");
        }
    }

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {

    }

    public void UpdateContent(CodeWindowLink link) {
        var tmproClassName = classNameTransform.GetComponent<TextMeshProUGUI>();
        var tmproMethodDec = methodDeclarationTransform.GetComponent<TextMeshProUGUI>();

        tmproClassName.text = link.EdgeLink.GetTo().file;
        tmproMethodDec.text = "Displayling method declaration not supported yet!";
    }
}
