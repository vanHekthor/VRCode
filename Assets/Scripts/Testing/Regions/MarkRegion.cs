using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MarkRegion : MonoBehaviour {

    public TMP_Text text;
    public RectTransform textRectTransform;

    public GameObject regionPrefab;
    public Transform attachTo;

    public uint from = 1;
    public uint to = 10;

    public bool firstRun = true;


    void Update() {

        if (firstRun) {
            if (drawRegion()) {
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

        // the text goes in negative Y
        // thats why minY is the closest to zero and maxY the most negative value
        float minY = Mathf.NegativeInfinity;
        float maxY = 0;
        float maxWidth = 0;

        // finish, we don't have to color because the region doesnt exist
        if (from < 0 || from > textInfo.lineCount) {
            return true;
        }

        // iterate over lines and get min and max values
        for (uint i = from; i < textInfo.lineCount; i++) {

            // out of region area, so stop here
            if (i > to) { break; }

            // we are inside the region area
            TMP_LineInfo lineInfo = textInfo.lineInfo[i];

            if (lineInfo.descender < maxY) {
                maxY = lineInfo.descender;
            }

            if (lineInfo.ascender > minY) {
                minY = lineInfo.ascender;
            }

            if (lineInfo.width > maxWidth) {
                maxWidth = lineInfo.width;
            }
        }

        Debug.Log("Min Y: " + minY + ", Max Y: " + maxY + ", Max Width: " +  maxWidth);
        
        // "draw" the region - spawn the prefab accordingly
        GameObject region = Instantiate(regionPrefab);
        region.transform.SetParent(attachTo, false);
        RectTransform rt = region.GetComponent<RectTransform>();
        if (rt) {
            float x = 0;
            float y = minY + textRectTransform.anchoredPosition.y;
            float width = maxWidth;
            float height = Mathf.Abs(maxY) - Mathf.Abs(minY);

            // see https://answers.unity.com/questions/1335356/instantiate-rect-transform-objects.html
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }
        
        return true;
    }

}
