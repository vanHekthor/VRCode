using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.IO.Features;

public class PointClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler{

    public GameObject testObject;

    public void OnPointerClick(PointerEventData eventData) {
        var configManager = testObject.GetComponent<ConfigManager>();
        configManager.AddConfig(configManager.Config1);
        Debug.Log("PointClick Component was clicked!");
    }

    private bool entered = false;
    public void OnPointerEnter(PointerEventData eventData) {
        if (!entered) {
            Debug.Log("PointClick Component was entered!");
            transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        }
        entered = true;
        exited = false;
    }

    private bool exited= false;
    public void OnPointerExit(PointerEventData eventData) {
        if (!exited) {
            Debug.Log("PointClick Component was exited!");
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        exited = true;
        entered = false;
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }


}
