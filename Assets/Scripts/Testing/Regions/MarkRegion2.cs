using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


// This script will get the general line height and then
// use it and multiply by the given from and to values
// this will not check if the text exists!
// The created region object will be attached to the first text element always.
public class MarkRegion2 : MonoBehaviour {

    public TMP_Text text;

    public GameObject regionPrefab;
    public Transform attachTo;

    public uint from = 1;
    public uint to = 10;

    public bool firstRun = true;


    void Update() {

        if (firstRun) {
            if (drawRegion()) {
                if (to < from) { to = from; }
                firstRun = false;
            }
        }

    }


    bool drawRegion() {

        // we have to wait because the array doesn't match the line count on start!
        TMP_TextInfo textInfo = text.textInfo;
        if (textInfo.lineInfo.Length < textInfo.lineCount) {
            return false;
        }

        float lineWidth = 0;
        float lineHeight = 0;

        // nothing to do
        if (textInfo.lineCount < 1) {
            return true;
        }

        TMP_LineInfo lineInfo = textInfo.lineInfo[0];
        lineWidth = lineInfo.width;
        lineHeight = lineInfo.lineHeight;
        
        Debug.Log("Line width: " + lineWidth + ", line height: " + lineHeight);
        
        // "draw" the region - spawn the prefab accordingly
        GameObject region = Instantiate(regionPrefab);
        region.transform.SetParent(attachTo, false);
        RectTransform rt = region.GetComponent<RectTransform>();
        if (rt) {
            float x = 0;
            float y = from * -lineHeight;
            float width = lineWidth;
            float height = (to - from + 1) * lineHeight;
            Debug.Log("Height = (" + from + " - " + to + " + 1) * " + lineHeight + " = " + height);

            // see https://answers.unity.com/questions/1335356/instantiate-rect-transform-objects.html
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }
        
        return true;
    }

}
