using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.IO.Features;

public class PointClick : MonoBehaviour, IPointerClickHandler {

    public GameObject testObject;

    public void OnPointerClick(PointerEventData eventData) {
        var configManager = testObject.GetComponent<ConfigManager>();
        configManager.AddConfig(configManager.Config1);
        Debug.Log("PointClick Component was clicked!");
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }
}
