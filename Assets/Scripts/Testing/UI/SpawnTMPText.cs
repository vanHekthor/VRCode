using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VRVis.Testing {

    /**
     * Script that spawns TextMesh Pro text
     * and tries to instantly get information about it
     * without using the Update method in a tricky way.
     */
    public class SpawnTMPText : MonoBehaviour {

        public GameObject textPrefab;
        public RectTransform parentTo;

        [Header("Gathered Information")]
        public int lineCount;
        public float lineHeight;
        public float preferredHeight;
        public float TMP_fontScale;
        public float TMP_fontSize;

        public string fontName;
        public float fontLineHeight;
        public float fontPointSize;
        public float fontBaseline;
        public float fontScale;
        public float fontSubSize;

        public Vector3 textBounds;
        public Vector2 renderedValues;

        [Tooltip("This is calculated from: fontScale * fontLineHeight")]
        public float lineHeight_calculated;


        void Awake() {

            if (!textPrefab) {
                Debug.LogError("Missing required text prefab!");
            }

            if (!parentTo) {
                Debug.LogError("Missing required parent to object!");
            }

        }

        void Start() {
            
            if (textPrefab && parentTo) {
                GameObject text = Instantiate(textPrefab);
                text.name = "Test TMP Text";
                text.transform.SetParent(parentTo, false);

                // change default size according to parented object
                RectTransform textRT = text.GetComponent<RectTransform>();
                textRT.sizeDelta = parentTo.sizeDelta;

                TextMeshProUGUI tmpgui = text.GetComponent<TextMeshProUGUI>();
                if (!tmpgui) { Debug.LogError("Failed to get TextMeshProUGUI component!"); return; }

                // set some example text
                tmpgui.SetText("Hello!\nHere is some\nexample text\nfor us to explore!");
                tmpgui.ForceMeshUpdate();

                TMP_fontScale = tmpgui.fontScale;
                TMP_fontSize = tmpgui.fontSize;
                
                fontName = tmpgui.font.name;
                fontLineHeight = tmpgui.font.faceInfo.lineHeight;
                fontPointSize = tmpgui.font.faceInfo.pointSize;
                fontBaseline = tmpgui.font.faceInfo.baseline;
                fontScale = tmpgui.font.faceInfo.scale;
                fontSubSize = tmpgui.font.fontInfo.SubSize;

                lineHeight_calculated = TMP_fontScale * fontLineHeight;

                lineCount = tmpgui.textInfo.lineCount;
                lineHeight = tmpgui.textInfo.lineInfo[0].lineHeight;
                preferredHeight = tmpgui.preferredHeight;
                textBounds = tmpgui.textBounds.size;
                renderedValues = tmpgui.GetRenderedValues(true);

                int i = 0;
                TMP_LineInfo[] lineInfos = tmpgui.textInfo.lineInfo;
                foreach (TMP_LineInfo lineInfo in lineInfos) {
                    Debug.Log("Line info " + (i++) + ": height=" + lineInfo.lineHeight +
                    ", words=" + lineInfo.wordCount + ", chars=" + lineInfo.characterCount);
                }
            }

        }

    }
}
