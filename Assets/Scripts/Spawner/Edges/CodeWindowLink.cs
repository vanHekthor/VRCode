using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Spawner.File;

namespace VRVis.Spawner.Edges {

    public class CodeWindowLink : MonoBehaviour {

        // pixel error per text-element to consider for region creation
        [Tooltip("Error correction value for multiple text instances")]
        public float ERROR_PER_ELEMENT = 0.2f; // 0.2 seems to be a good value for font-size 8-14

        [Tooltip("Canvas scale to scale down the edge width accordingly for regions")]
        public float canvasScale = 0.01f;

        [Tooltip("Show debug points used for region positioning and rescaling in editor")]
        public bool showRegionDebugGizmos = false;

        private Transform linkAnchorTransform;
        private Vector3 previousAnchorPointPos;

        public CodeFile BaseFile { get; private set; }
        private CodeFileReferences baseWindowReferences;

        public CodeFile TargetFile { get; private set; }

        // distance between left and right next to the windows (converted to world units)
        private float baseWindowLeftRightDistance;

        public Edge EdgeLink { get; private set; }
        private bool updateLink;
        private bool successfullyAttached = false;

        /// <summary>represent control flow starting points as link buttons</summaryr>
        private GameObject linkButton;
        private CodeWindowLinkButton linkButtonComponent;
        [SerializeField]
        private GameObject linkButtonPrefab;

        /// <summary>Tells how many code lines are encloded at the start.</summary>
        private float baseRegionSpan = 0;

        /// <summary>Height of a single code line</summary>
        public float LineHeight { get; private set; } = 0;

        private Vector3 debug_pos = Vector3.zero;
        private Vector3 debug_pos_edge = Vector3.zero;

        // Start is called before the first frame update
        void Start() {
            
        }

        // Update is called once per frame
        void Update() {
            if (successfullyAttached && updateLink) {
                UpdateLinePosition();
            }
        }

        void OnDestroy() {

            updateLink = false;

            // take care of cleaning up the link buttons
            if (linkButton) { Destroy(linkButton); }
        }

        public bool InitLink(Edge edge, CodeFile baseFile, CodeFile targetFile) {            
            BaseFile = baseFile;

            // set the edge this connection represents
            EdgeLink = edge;

            // the span of the enclosed connection region
            baseRegionSpan = edge.GetFrom().lines.to - edge.GetFrom().lines.from;
            //endSpan = edge.GetTo().lines.to - edge.GetTo().lines.from;            

            updateLink = true;
            successfullyAttached = false;

            string base_err = "Failed to spawn link button";
            if (EdgeLink == null) {
                Debug.LogError(base_err + " - link button instance is null!");
                updateLink = false;
                return false;
            }

            // get relative file paths that user entered
            string baseFilePath = baseFile.GetNode().GetPath();

            // code file error handling
            string baseFilePathError = "(file: " + baseFilePath + ")";
            if (baseFile == null) { Debug.LogError(base_err + " - missing code base file! " + baseFilePathError); return false; }
            if (baseFile.GetReferences() == null) { Debug.LogError(base_err + " - missing code base file references! " + baseFilePathError); return false; }

            // get edge point references
            baseWindowReferences = baseFile.GetReferences();

            // create point transforms telling where the link button should be placed
            GameObject linkAnchor = new GameObject("LinkAnchor", typeof(RectTransform));
            linkAnchorTransform = linkAnchor.transform;

            // set target file
            TargetFile = targetFile;

            // create link buttons
            linkButton = CreatePhysicalButton(
                "Button", 
                EdgeLink.GetFrom().columns.from, 
                EdgeLink.GetFrom().columns.to,
                baseWindowReferences.GetCodeFile().GetLineInfo().characterWidth);

            if (!linkButton) {
                Debug.LogError("Not able to properly instantiate the LinkButton targeting the file '" + targetFile + "'!");
            }            

            // attach them to the code window canvas to automatically update
            // the world positions based on rotation, movement and scroll
            PositionLinkAnchors(
                linkAnchor.GetComponent<RectTransform>(), 
                EdgeLink.GetFrom().lines.from, 
                EdgeLink.GetFrom().columns.from, 
                baseWindowReferences);

            // get distances between left and right next to each window
            // and convert them from canvas units in world units (mult with scale)
            baseWindowLeftRightDistance = baseWindowReferences.GetEdgePoints().GetLeftRightDistance() * linkAnchorTransform.lossyScale.x;

            updateLink = true;
            successfullyAttached = true;
            return true;
        }

        /// <summary>
        /// Creates physical link button and returns it.
        /// </summary>
        private GameObject CreatePhysicalButton(string name, int startColumn, int endColumn, float columnWidth) {

            // create physical link button
            GameObject linkButton = Instantiate(linkButtonPrefab);
            linkButtonComponent = linkButton.GetComponent<CodeWindowLinkButton>();
            if (!linkButtonComponent) {
                DestroyImmediate(linkButton);
                return null;
            }

            // attach this CodeWindowLink to the physical button
            linkButton.name = name;
            linkButtonComponent.Link = this;
            linkButtonComponent.BaseCodeWindowObject = BaseFile.GetReferences().gameObject;

            // attach to this GameObject instance
            linkButton.transform.SetParent(transform, false);
            linkButton.transform.rotation = transform.rotation;

            // stretch physical link button to width of method call expression

            // 1. convert column width to corresponding x-scale value for the link button
            float columnWidthToScale = columnWidth / 10;
            // 2. scale button to column width
            linkButtonComponent.ButtonBody.localScale = 
                new Vector3(columnWidthToScale, linkButtonComponent.ButtonBody.localScale.y, linkButtonComponent.ButtonBody.localScale.z);
            // 3. stretch button to method call width
            float stretchVal = linkButtonComponent.ButtonBody.localScale.x * (endColumn + 1 - startColumn);
            linkButtonComponent.ButtonBody.localScale = 
                new Vector3(stretchVal, linkButtonComponent.ButtonBody.localScale.y, linkButtonComponent.ButtonBody.localScale.z);

            // set button text to match method call name
            var splitLabel = EdgeLink.GetLabel().Split('.');
            string methodCallName = splitLabel.Length == 1 ? splitLabel[0] : splitLabel[splitLabel.Length - 1];
            linkButtonComponent.ChangeLinkText(methodCallName);

            linkButton.SetActive(false);

            return linkButton;
        }

        /// <summary>
        /// Positions the link anchors at the location of the corresponding method call inside the displayed code.
        /// </summary>
        /// <param name="anchorRectTransform"></param>
        /// <param name="startLine"></param>
        /// <param name="fileRefs"></param>
        /// <returns></returns>
        private bool PositionLinkAnchors(RectTransform anchorRectTransform, int startLine, int startColumn, CodeFileReferences fileRefs) {

            // get code file and line information
            CodeFile codeFile = fileRefs.GetCodeFile();
            if (!codeFile.IsLineInfoSet()) { return false; }

            CodeFile.LineInformation lineInfo = codeFile.GetLineInfo();
            int lineCount = lineInfo.lineCount;
            if (startLine > lineCount) { return false; }

            // get line height and store global one
            float thisLineHeight = lineInfo.lineHeight;
            if (thisLineHeight == 0) { return false; }
            LineHeight = thisLineHeight;

            //// set initial edge curve width taking the line height into account
            //SetEdgeStartWidth(startSpan * lineHeight, true);
            //SetEdgeEndWidth(endSpan * lineHeight, true);

            // attach to parent container and take its position and scale factor
            Transform anchorContainer = fileRefs.GetEdgePoints().anchorContainer;
            if (!anchorContainer) { return false; }            
            anchorRectTransform.SetParent(anchorContainer, false);

            // prepare rect transform
            anchorRectTransform.anchorMin = new Vector2(0, 1);
            anchorRectTransform.anchorMax = new Vector2(0, 1);
            anchorRectTransform.sizeDelta = new Vector2(5, 5);

            // set position according to the line number (node)
            float currentLinePos = startLine / (float)lineCount;
            float pixelError = currentLinePos * ((float)fileRefs.GetTextElements().Count - 1) * ERROR_PER_ELEMENT;
            float yPosOffset = LineHeight * 0.5f; // to get correct position ("middle of line")
            float yPos = (startLine - 1) * LineHeight + yPosOffset - pixelError;
            //float xPos = fileRefs.GetEdgePoints().left; // if left or right will be decided on update
            float xPos = startColumn * fileRefs.GetCodeFile().GetLineInfo().characterWidth + 25;
            anchorRectTransform.anchoredPosition = new Vector2(xPos, -yPos); // yPos needs to be a negative value!

            return true;
        }

        private void UpdateLinePosition() {

            // check if we even have to update
            if (!IsUpdateRequired()) { return; }

            // get position left and right next to the "base" window
            Vector3 baseWindowAttachmentLeft = baseWindowReferences.GetLeftEdgeConnection(); // edge attachment left
            Vector3 baseWindowLeftPosition = new Vector3(linkAnchorTransform.position.x, linkAnchorTransform.position.y, linkAnchorTransform.position.z);
            Vector3 basewindowRightPosition = baseWindowLeftPosition + linkAnchorTransform.right * baseWindowLeftRightDistance;

            bool attachToLeftSide = true;

            // line start is position at "from-window" and end at "to-window"
            Vector3 lineStart = attachToLeftSide ? baseWindowLeftPosition : basewindowRightPosition;

            // check if the position / region is in the "content bounds" and adjust accordingly
            bool startOutOfBounds = false;

            if (baseRegionSpan > 0) {
                float finalBaseRegionSpan = baseRegionSpan * LineHeight * canvasScale;
                lineStart = ValidateBoundsRegion(true, lineStart, finalBaseRegionSpan, baseWindowReferences, attachToLeftSide, out startOutOfBounds, linkButton);
            }
            else {
                lineStart = ValidateBounds(lineStart, baseWindowReferences, attachToLeftSide, out startOutOfBounds, linkButton);
            }




        }

        /// <summary>
        /// Validate the current position against the window content bounds.<para/>
        /// Returns the valid position which can be the current if its valid.
        /// </summary>
        /// <param name="attachLeft">If the current position is left attached</param>
        /// <param name="curPos">The current position to be validated</param>
        /// <param name="outOfBounds">Tells if this check resulted in out of bounds</param>
        private Vector3 ValidateBounds(Vector3 curPos, CodeFileReferences winRefs, bool attachLeft, out bool outOfBounds, GameObject linkButton) {

            CodeFileReferences.EdgeAnchors edgePoints = winRefs.GetEdgePoints();
            Vector3 winTop = attachLeft ? edgePoints.topLeft.position : edgePoints.topRight.position;
            Vector3 winBottom = attachLeft ? edgePoints.bottomLeft.position : edgePoints.bottomRight.position;
            Vector3 posOut = curPos;

            // check if out of viewport bounds
            outOfBounds = false;
            //if (curPos.y > winRefs.GetViewportTop().y) {
            if (curPos.y > winTop.y) { 
                // posOut.y = winTop.y;
                outOfBounds = true;
            }
            //else if (curPos.y < winRefs.GetViewportBottom().y) {
            else if (curPos.y < winBottom.y) { 
                // posOut.y = winBottom.y;
                outOfBounds = true;
            }

            float linkButtonWidthOffset = linkButtonComponent.GetButtonWidth();
            Vector3 windowLeftDirection = (edgePoints.topLeft.position - edgePoints.topRight.position).normalized;
            Vector3 curPosToWindowTopLeft = edgePoints.topLeft.position - (curPos - windowLeftDirection * linkButtonWidthOffset);
            Vector3 curPosToWindowTopRight = edgePoints.topRight.position - curPos;
            

            bool outOfLeftBound = Vector3.Dot(curPosToWindowTopLeft, windowLeftDirection) < 0;
            bool outOfRightBound = Vector3.Dot(curPosToWindowTopRight, -windowLeftDirection) < 0;

            if (outOfLeftBound) {
                outOfBounds = true;
            }
            else if (outOfRightBound) {
                outOfBounds = true;
            }

            // position the link button
            PositionLinkButton(linkButton, outOfBounds, posOut);

            return posOut;
        }

        /// <summary>
        /// Validate the current position and region span against the window content bounds.<para/>
        /// Returns the valid position, which is equal to the passed one if the whole region is visible.
        /// </summary>
        /// <param name="start">If this is the edge start or not (end)</param>
        /// <param name="curPos">Current position to be validated</param>
        /// <param name="span">The total region span of this connection (ensure it uses the line height!)</param>
        /// <param name="outOfBounds">Tells if this region is completely out of bounds</param>
        private Vector3 ValidateBoundsRegion(bool start, Vector3 curPos, float span, CodeFileReferences winRefs, bool attachLeft, out bool outOfBounds, GameObject sphere) {

            CodeFileReferences.EdgeAnchors edgePoints = winRefs.GetEdgePoints();
            Vector3 winTop = attachLeft ? edgePoints.topLeft.position : edgePoints.topRight.position;
            Vector3 winBottom = attachLeft ? edgePoints.bottomLeft.position : edgePoints.bottomRight.position;
            Vector3 posOut = curPos;

            // get viewport bounds
            //Vector3 viewportTop = winRefs.GetViewportTop();
            Vector3 viewportTop = winTop;
            //Vector3 viewportBtm = winRefs.GetViewportBottom();
            Vector3 viewportBtm = winBottom;

            // [DEBUG]
            if (showRegionDebugGizmos) { debug_pos = posOut; }


            // check if region is completely out of bounds at top
            outOfBounds = false;
            float dist_top = curPos.y - viewportTop.y;
            if (dist_top >= span) {

                PositionLinkButton(sphere, true, posOut); // hide sphere
                
                outOfBounds = true;
                return winTop;
            }

            // check if region is completely out of bounds at bottom
            float dist_btm = curPos.y - viewportBtm.y;
            if (dist_btm <= 0) {

                PositionLinkButton(sphere, true, posOut); // hide sphere
                
                outOfBounds = true;
                return winBottom;
            }
            
            // check if part of the region is out of bounds at the top and/or btm window border
            bool region_outOfBounds_top = curPos.y > viewportTop.y;
            bool region_outOfBounds_btm = curPos.y - span < viewportBtm.y;
            float newEdgeWidth = span;

            // part of the region covers whole content size -> edge attachment pos at middle of content
            if (region_outOfBounds_top && region_outOfBounds_btm) {

                Vector3 btm_top = (viewportTop - viewportBtm);
                posOut.y = (viewportBtm + btm_top * 0.5f).y;

                // hide sphere
                PositionLinkButton(sphere, true, posOut);

                newEdgeWidth = btm_top.magnitude;
            }

            // part of the region is out at the top
            else if (region_outOfBounds_top) {

                float width_left = span - dist_top;
                Vector3 top_btm_norm = Vector3.Normalize(viewportBtm - viewportTop);
                posOut.y = (viewportTop + top_btm_norm * (width_left * 0.5f)).y;

                // sphere at end of region
                Vector3 regionEnd = curPos;
                regionEnd.y = (viewportTop + top_btm_norm * width_left).y;
                PositionLinkButton(sphere, false, regionEnd);

                newEdgeWidth = width_left;
            }

            // part of the region is out at the bottom
            else if (region_outOfBounds_btm) {

                float width_left = dist_btm;
                Vector3 btm_top_norm = Vector3.Normalize(viewportTop - viewportBtm);
                posOut.y = (viewportBtm + btm_top_norm * (width_left * 0.5f)).y;

                // sphere at start of region
                PositionLinkButton(sphere, false, curPos);

                newEdgeWidth = width_left;
            }

            // the whole region is visible
            else {

                Vector3 top_btm_norm = Vector3.Normalize(viewportBtm - viewportTop);
                posOut.y = (curPos + top_btm_norm * (span * 0.5f)).y;

                // hide sphere
                PositionLinkButton(sphere, true, posOut);
            }


            // [DEBUG]
            if (showRegionDebugGizmos) { debug_pos_edge = posOut; }

            return posOut;
        }

        /// <summary>Position and set active state of the link button accordingly.</summary>
        private void PositionLinkButton(GameObject linkButton, bool outOfBounds, Vector3 position) {

            if (!linkButton) { return; }
            if (outOfBounds) { linkButton.SetActive(false); }
            else {
                linkButton.transform.position = position;
                linkButton.SetActive(true);
            }
        }
        
        /// <summary>
        /// Tells if an link update is required by checking
        /// attributes of the two windows like position, rotation and scroll.
        /// </summary>
        private bool IsUpdateRequired() {            
            
            // check if start- and endpoint position changed
            // (if so, position, rotation or scroll changed)
            if (linkAnchorTransform.position != previousAnchorPointPos) {
                previousAnchorPointPos = linkAnchorTransform.position;
                return true;
            }

            return false;
        }

        // DEBUG!
        void OnDrawGizmos() {

            if (showRegionDebugGizmos) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(debug_pos, 0.05f);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(debug_pos_edge, 0.05f);
            }
        }
    }
}