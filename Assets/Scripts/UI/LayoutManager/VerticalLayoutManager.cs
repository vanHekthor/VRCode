using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalLayoutManager : MonoBehaviour {
    [Header("Padding")]
    public float leftP;
    public float rightP;
    public float topP;
    public float bottomP;

    public List<GameObject> Elements { get; private set; }
    public Vector2 Dimensions { get; private set; }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void AddElements(List<GameObject> elements) {
        Elements = elements;
        float totalHeight = 0f;
        float totalWidth = 0f;

        for (int i = 0; i < elements.Count; i++) {
            var element = elements[i];
            element.transform.SetParent(transform, false);

            var rt = element.GetComponent<RectTransform>();

            // set new element position
            rt.localPosition = new Vector2(leftP, -totalHeight);

            // calculate container dimensions            
            float elementWidth = rt.sizeDelta.x;
            float elementHeight = rt.sizeDelta.y;

            totalWidth = Mathf.Max(totalWidth, elementWidth + leftP);
            totalHeight += elementHeight;            
        }

        var containerRT = GetComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(totalWidth, totalHeight);
    }

    public void AddElement(GameObject element) {
        Elements.Add(element);
    }


    
}
