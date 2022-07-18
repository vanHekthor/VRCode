using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRVis.Elements;
using VRVis.IO;
using VRVis.IO.Features;

namespace VRVis.Spawner.File {

    /// <summary>
    /// Attached to each code window.<para/>
    /// A code window represents a file and we need to store information about it somewhere.
    /// This is the location for such information.<para/>
    /// This script is e.g. used by the FileSpawner script, to create edges and to delete windows.<para/>
    /// Accessing the "gameObject variable" (where it is attached to) of this script
    /// will also allow access to the main game object holding all the file related contents.<para/>
    /// Private variables are set during the spawning process by the "FileSpawner" script.
    /// </summary>
    public class CodeFileReferences : MonoBehaviour {

        [Tooltip("Text object to show filename")]
        public TMP_Text fileName;

        [Tooltip("Holds the line numbers")]
        public TMP_Text lineNumbers;

        [Tooltip("A feature not supported by previous version of the code window prefab")]
        public bool showLineNumbers = false;

        [Tooltip("Background image that defines the background color of the code window")]
        public Image codeBackground;

        [Tooltip("Scroll rect for scroll view main window")]
        public ScrollRect scrollRect;

        [Tooltip("Holds text prefab instances")]
        public Transform textContainer;

        [Tooltip("Holds region prefab instances")]
        public Transform regionContainer;

        [Tooltip("Holds highlighted line instances")]
        public Transform highlighedLinesContainer;

        [Tooltip("Holds regions of features")]
        public Transform featureContainer;

        [Tooltip("Holds regions of height map")]
        public Transform heightmapRegionContainer;

        [Tooltip("The heightmap element of this code window")]
        public GameObject heightMap;

        [Tooltip("The active feature visualization of this code window")]
        public GameObject activeFeatureVis;

        [Tooltip("The prefab that is used for highlighting code lines")]
        public GameObject lineHighlightPrefab;

        [Tooltip("Points required for successful edge creation")]
        public EdgeAnchors edgePoints;
        public bool drawEdgeAnchorGizmos = true;

        public Configuration Config { get; set; }

        // reference to the "main" element managing the according code file
        private CodeFile codeFile;

        // stores a reference to all TMP text elements info
	    private List<TMP_TextInfo> textElements = new List<TMP_TextInfo>();
        private int linesTotal = 0;

        public Dictionary<Tuple<string, int>, LineHighlight> HighlightedMethods { get; private set; }

        public class MethodHighlightEvent : UnityEvent<string, int> { }
        public MethodHighlightEvent onMethodHighlightSpawned = new MethodHighlightEvent();
        public MethodHighlightEvent onMethodHighlightRemoved = new MethodHighlightEvent();

        // Class for edge connection information
        [System.Serializable]
        public class EdgeAnchors {

            [Tooltip("Container to attach edge connection instances to")]
            public Transform connectionContainer;

            [Tooltip("Holds edge anchor points spawned by edge connections")]
            public Transform anchorContainer;

            [Header("Attachment Points Above/Below:")]
            [Tooltip("Position to attach points above the window")]
            public Transform topLeft;
            public Transform topRight;

            [Tooltip("Position to attach points below the window")]
            public Transform bottomLeft;
            public Transform bottomRight;
            
            [Header("Attachment Points Left/Right:")]
            [Tooltip("Anchor position where control flow edges can be connectet to")]
            public float left;
            public float right;
            [Tooltip("Anchor position relative to the anchor container for control flow links")]
            public float linkOffset;

            public float GetLeftRightDistance() { return right-left; }

            // we are able to get content top and bottom from the scrollrect viewport
            // so we do not need another variable for it
        }


        // GETTER AND SETTER

        public GameObject GetCodeWindow() { return gameObject; }

        public ScrollRect GetScrollRect() { return scrollRect; }

        public RectTransform GetVerticalScrollbarRect() {
            if (!GetScrollRect().verticalScrollbar) { return null; }
            if (!GetScrollRect().verticalScrollbar.gameObject) { return null; }
            return GetScrollRect().verticalScrollbar.gameObject.GetComponent<RectTransform>();
        }

        public CodeFile GetCodeFile() { return codeFile; }
        public void SetCodeFile(CodeFile codeFile) {
            
            this.codeFile = codeFile;
            if (codeFile == null) { return; }

            // set file name
            if (!fileName) { return; }
            fileName.SetText(codeFile.GetNode().GetName());
            fileName.ForceMeshUpdate();
        }

        /// <summary>Get the GameObject of the according heightmap. Returns null if not set!</summary>
        public GameObject GetHeightmap() { return heightMap; }

        /// <summary>Get GameObject of the according active feature visualization. Returns null if missing!</summary>
        public GameObject GetActiveFeatureVis() { return activeFeatureVis; }

        public List<TMP_TextInfo> GetTextElements() { return textElements; }
        public void SetTextElements(List<TMP_TextInfo> elements) { textElements = elements; }
        public void AddTextElement(TMP_TextInfo text) { textElements.Add(text); }

        public int GetLinesTotal() { return linesTotal; }
        public void SetLinesTotal(int linesTotal) { this.linesTotal = linesTotal; }

        public EdgeAnchors GetEdgePoints() { return edgePoints; }

        public bool IsMethodHighlighted(string relativeFilePath, int startLine) {
            return HighlightedMethods.ContainsKey(Tuple.Create(relativeFilePath, startLine));
        }

        private void Start() {
            HighlightedMethods = new Dictionary<Tuple<string, int>, LineHighlight>();
        }

        // FUNCTIONALITY

        /// <summary>Get scroll rect viewport top position.</summary>
        public Vector3 GetViewportTop() {
            return GetScrollRect().viewport.position;
        }

        /// <summary>Get scroll rect viewport bottom position.</summary>
        public Vector3 GetViewportBottom() {
            float rectHeightWorld = GetScrollRect().viewport.rect.height * scrollRect.transform.lossyScale.y;
            return GetViewportTop() - new Vector3(0, rectHeightWorld, 0);
        }


        /// <summary>Get position of the scroll rect. Returns zero vector if not set.</summary>
        /// <param name="xOffset">Offset using the "right" vector of the scroll rect</param>
        /// <param name="xScale">Scaling factor of the right movement (required to convert from canvas to world coords)</param>
        public Vector3 GetScrollRectPos(float xOffset = 0, float xScale = 1) {
            if (!GetScrollRect()) { return Vector3.zero; }
            Vector3 rectPos = GetScrollRect().transform.position;
            Vector3 dirRight = GetScrollRect().transform.right;
            return rectPos + xOffset * xScale * dirRight;
        }

        /// <summary>
        /// Get position where edges should be connected left to the window.<para/>
        /// (Requires that anchorContainer and ScrollRect are set - otherwise returns zero vector!)
        /// </summary>
        public Vector3 GetLeftEdgeConnection() {
            if (!edgePoints.anchorContainer) { return Vector3.zero; }
            return GetScrollRectPos(edgePoints.left, edgePoints.anchorContainer.lossyScale.x);
        }

        /// <summary>
        /// Get position where edges should be connected right to the window.<para/>
        /// (Requires that anchorContainer and ScrollRect are set - otherwise returns zero vector!)
        /// </summary>
        public Vector3 GetRightEdgeConnection() {
            if (!edgePoints.anchorContainer) { return Vector3.zero; }
            return GetScrollRectPos(edgePoints.right, edgePoints.anchorContainer.lossyScale.x);
        }

        /// <summary>
        /// Adds line numbers if the feature is activated
        /// and the according text element assigned.
        /// </summary>
        public void AddLineNumbers(uint numbers) {
            
            if (!showLineNumbers || lineNumbers == null) { return; }

            // ToDo: handle possible overflow (currently 9999 lines possible)

            System.Text.StringBuilder strb = new System.Text.StringBuilder();
            for (uint i = 0; i < numbers; i++) {
                strb.AppendLine((i+1).ToString());
            }
            
            lineNumbers.SetText(strb);
        }

        public LineHighlight SpawnMethodHighlight(int startLine, int endLine) {
            string relativeFilePath = codeFile.GetNode().GetRelativePath();

            var key = Tuple.Create(relativeFilePath, startLine);

            if (HighlightedMethods.ContainsKey(key)) return HighlightedMethods[key];

            var highlight = SpawnLineHighlight(startLine, endLine);
            if (highlight == null) return null;

            AddMethodHighlight(highlight, relativeFilePath, startLine);

            return highlight;
        }

        /// <summary>
        /// Spawns a highlighted area for the passed paramaters.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="width"></param>
        /// <param name="lineHeight">height of a single line</param>
        /// <returns>line highlight component of the instantiated highlight object</returns>
        public LineHighlight SpawnLineHighlight(int start, int end) {
            float lineHeight = codeFile.GetLineInfo().lineHeight;
            float totalWidth_codeMarking = codeFile.GetLineInfo().lineWidth;

            // check height and width values
            if (lineHeight == 0) {
                Debug.LogWarning("Failed to spawn regions! Line height is zero!");
                return null;
            }

            if (totalWidth_codeMarking == 0) {
                Debug.LogWarning("Failed to spawn regions! Code marking width is zero!");
                return null;
            }

            //// get region width for code marking visualization (might change in future)
            //RectTransform scrollRectRT = GetScrollRect().GetComponent<RectTransform>();
            //RectTransform textContainerRT = textContainer.GetComponent<RectTransform>();
            //RectTransform vertScrollbarRT = GetVerticalScrollbarRect();
            //if (scrollRectRT && textContainerRT && vertScrollbarRT) {
            //    totalWidth_codeMarking = scrollRectRT.sizeDelta.x - textContainerRT.anchoredPosition.x - Mathf.Abs(vertScrollbarRT.sizeDelta.x) - 5;
            //}

            GameObject lineHighlightObject = Instantiate(lineHighlightPrefab);
            var lineHighlight = lineHighlightObject.GetComponent<LineHighlight>();
            if (lineHighlight == null) {
                Debug.LogError("Failed to highlight line in code - Prefab is missing LineHighlight component!");
                return null;
            }

            if (highlighedLinesContainer == null) {
                Debug.LogError("Failed to highlight line because highlighted line container is missing!" +
                    " Probably needs to be set in Unity Editor.");
                return null;
            }

            lineHighlightObject.transform.SetParent(highlighedLinesContainer, false);

            // pixel error calculation (caused by multiple text-elements)
            float curLinePos = start / (float)linesTotal;
            float pxErr = curLinePos * ((float)textElements.Count - 1) 
                * FileSpawner.GetInstance().regionSpawner.ERROR_PER_ELEMENT;

            // scale and position highlight
            float x = 0f;
            float y = (start - 1) * - lineHeight + pxErr; // lineHeight needs to be a negative value!
            float height = (end - start + 1) * lineHeight;

            RectTransform rt = lineHighlightObject.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(totalWidth_codeMarking, height);

            return lineHighlight;
        }

        private void AddMethodHighlight(LineHighlight lineHighlight, string relativeFilePath, int startLine) {
            var key = Tuple.Create(relativeFilePath, startLine);
            HighlightedMethods.Add(key, lineHighlight);
            onMethodHighlightSpawned.Invoke(relativeFilePath, startLine);
        }

        public void RemoveMethodHighlight(string relativeFilePath, int startLine) {
            var key = Tuple.Create(relativeFilePath, startLine);
            HighlightedMethods.Remove(key);
            onMethodHighlightRemoved.Invoke(relativeFilePath, startLine);
        }

        public void ScrollTo(RectTransform target) {
            Canvas.ForceUpdateCanvases();

            scrollRect.content.anchoredPosition =
                    (Vector2)scrollRect.transform.InverseTransformPoint(scrollRect.content.position) * (Vector2)target.up
                    - (Vector2)scrollRect.transform.InverseTransformPoint(target.position) * (Vector2)target.up
                    - new Vector2(0, codeFile.GetLineInfo().lineHeight);
        }

        private bool visible;
        public void ToggleVisibility() {
            visible = !visible;
            gameObject.SetActive(visible);
        }

        public void SetVisibility(bool visible) {
            gameObject.SetActive(visible);
        }

        void OnDrawGizmos() {
            
            // draw edge point gizmos
            if (drawEdgeAnchorGizmos) {

                // draw top/bottom left/right anchors
                float radius = 0.05f;
                Gizmos.color = Color.red;
                if (edgePoints.topLeft) { Gizmos.DrawSphere(edgePoints.topLeft.position, radius); }
                if (edgePoints.topRight) { Gizmos.DrawSphere(edgePoints.topRight.position, radius); }
                if (edgePoints.bottomLeft) { Gizmos.DrawSphere(edgePoints.bottomLeft.position, radius); }
                if (edgePoints.bottomRight) { Gizmos.DrawSphere(edgePoints.bottomRight.position, radius); }

                // draw left/right values
                Gizmos.color = Color.yellow;
                if (GetScrollRect()) {
                    Vector3 leftPosWorld = GetLeftEdgeConnection();
                    Vector3 rightPosWorld = GetRightEdgeConnection();
                    Gizmos.DrawSphere(leftPosWorld, radius);
                    Gizmos.DrawSphere(rightPosWorld, radius);
                }

                // draw viewport borders
                Gizmos.color = Color.green;
                ScrollRect scrollRect = GetScrollRect();
                if (scrollRect) {
                    Gizmos.DrawSphere(GetViewportTop(), radius);
                    Gizmos.DrawSphere(GetViewportBottom(), radius);
                }
            }
        }

    }
}
