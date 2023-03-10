using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using VRVis.Effects;
using VRVis.Elements;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.Mappings;
using VRVis.Mappings.Methods;
using VRVis.RegionProperties;
using VRVis.Spawner.File;
using VRVis.Spawner.Regions;
using VRVis.Utilities;

namespace VRVis.Spawner.Edges {

    /// <summary>
    /// This script handles a connection between two points in space connecting two nodes or regions.<para/>
    /// It will be attached as a component to the object that has the LineRenderer component (the "from" window).<para/>
    /// The instance will be managed by the "CodeWindowEdgeSpawner" and only exists when both code files are spawned.
    /// </summary>
    /// 
    /// Done:
    /// - [x] Use Bezier curves instead of simple lines
    /// - [x] Only Update when required (e.g. window moved/rotated or user scrolled)
    /// 
    /// Possible Improvements:
    /// - [] Maybe safe some processing time by creating the start and end-sphere in the prefab and just assigning them
    [RequireComponent(typeof(LineRenderer))]
    public class CodeWindowEdgeConnection : MonoBehaviour {

        // pixel error per text-element to consider for region creation
        [Tooltip("Error correction value for multiple text instances")]
        public float ERROR_PER_ELEMENT = 0.2f; // 0.2 seems to be a good value for font-size 8-14

        [Tooltip("Size of the attachment spheres")]
        public Vector3 attachmentSphereSize = new Vector3(0.15f, 0.15f, 0.15f);

        [Tooltip("The vfx control flow effect indicating direction and nfp value")]
        public VisualEffect vfxControlFlow;

        [Tooltip("Speed of the VFX particles in m/s")]
        public float vfxParticleSpeed = 1.4f;

        [Tooltip("Distance between the VFX particles in m")]
        public float distanceBetweenParticles = 0.4f;

        public GameObject hoverPointPrefab;

        [Tooltip("Material of attachment spheres")]
        public Material attachmentSphereMat;

        [Tooltip("Min width of the line in unity world units")]
        public float minLineWidth = 0.01f;

        [Tooltip("Max width of the line in unity world units")]
        public float maxLineWidth = 0.3f;

        [Tooltip("Canvas scale to scale down the edge width accordingly for regions")]
        public float canvasScale = 0.01f;

        [Tooltip("Steps of the bezier curve (linear if less than 4)")]
        [Range(0, 100)] public uint curveSteps = 20;

        [Tooltip("Distance of control points based on edge length")]
        [Range(0, 1)] public float curveStrength = 0.25f; 

        [Tooltip("Add some random noise between 0 and this value to the strength to distinguish different edges")]
        [Range(0, 1)] public float curveStrengthNoise = 0.1f;

        [Tooltip("Show debug points used for region positioning and rescaling in editor")]
        public bool showRegionDebugGizmos = false;

        [Tooltip("Show bezier curve control points in editor")]
        public bool showControlPointGizmos = false;

        public LineHighlight CallMethodHighlight { get; set; }
        public LineHighlight TargetMethodHighlight { get; set; }

        public Vector3 LineStart { get; private set; }
        public Vector3 ControlPoint1 { get; private set; }
        public Vector3 ControlPoint2 { get; private set; }
        public Vector3 LineEnd { get; private set; }
        public Region TargetRegion { get; private set; }

        private Transform startPoint;
        private Transform endPoint;

        private CodeFileReferences fromWindowRefs;
        private CodeFileReferences toWindowRefs;
        private bool fromToSameFile = false; // connection from this file to this file

        // distance between left and right next to the windows (converted to world units)
        private float from_leftRightDist;
        private float to_leftRightDist;

        private Edge edge;
        private bool updateEdge;
        private bool successfulAttached = false;

        private LineRenderer lineRenderer;
        private ZLineRenderer zLineRenderer;

        private BezierCurve bezierCurve;
        private float rndCurveStrengthNoise = 0;

        // represent the attachment points
        private GameObject startHoverPoint;
        private GameObject endHoverPoint;
        private GameObject midHoverPoint;

        // Store previous attributes to check if update is required.
        // The window rotation and scroll will affect the positions as well,
        // because both transforms are parented to the according objects that change.
        private Vector3 previousStartPointPos;
        private Vector3 previousEndPointPos;

        /// <summary>Tells about how this edge should be mapped visually.</summary>
        private EdgeSetting edgeSetting;

        /// <summary>Tells how many code lines are encloded at the start.</summary>
        private float startSpan = 0;
        private float edgeStartWidth = 0; // span with line height applied

        /// <summary>Tells how many code lines are encloded at the end.</summary>
        private float endSpan;
        private float edgeEndWidth = 0; // span with line height applied

        /// <summary>Height of a single code line</summary>
        private float lineHeight = 0;

        private Vector3 debug_pos = Vector3.zero;
        private Vector3 debug_pos_edge = Vector3.zero;

        // GETTER AND SETTER

        public Edge GetEdge() { return edge; }
        public void SetEdge(Edge edge) { this.edge = edge; }

        /// <summary>Get the code file this line starts at. Can be null on errors!</summary>
        public CodeFileReferences GetStartCodeFileInstance() {
            if (fromWindowRefs == null) { return null; }
            return fromWindowRefs;
        }

        /// <summary>Get the code file this line ends at. Can be null on errors!</summary>
        public CodeFileReferences GetEndCodeFileInstance() {
            if (toWindowRefs == null) { return null; }
            return toWindowRefs;
        }

        /// <summary>Get the start attachment transform.</summary>
        public Transform GetStart() { return startPoint; }

        /// <summary>Get the end attachment transform.</summary>
        public Transform GetEnd() { return endPoint; }

        public LineRenderer GetLineRenderer() { return lineRenderer; }

        public ZLineRenderer GetZLineRenderer() { return zLineRenderer; }

        /// <summary>Get settings for this edge.</summary>
        public EdgeSetting GetEdgeSetting() { return edgeSetting; }

        /// <summary>
        /// Set the edge setting for this edge type.<para/>
        /// Use the according apply method to apply mapping methods.
        /// </summary>
        public void SetEdgeSetting(EdgeSetting edgeSetting) {

            this.edgeSetting = edgeSetting;
            curveSteps = edgeSetting.GetSteps();
            curveStrength = edgeSetting.GetCurveStrength();
            curveStrengthNoise = edgeSetting.GetCurveNoise();
        }

        /// <summary>Height of a code line (can be 0 until correctly initialized).</summary>
        public float GetLineHeight() { return lineHeight; }


        public float GetEdgeStartWidth() { return edgeStartWidth; }

        /// <summary>Set width of edge line at the start.</summary>
        public void SetEdgeStartWidth(float width, bool applyCanvasScale) {
            width *= applyCanvasScale ? canvasScale : 1;
            edgeStartWidth = width < minLineWidth ? minLineWidth : width;
        }

        public float GetEdgeEndWidth() { return edgeEndWidth; }

        /// <summary>Set width of edge line at the end./summary>
        public void SetEdgeEndWidth(float width, bool applyCanvasScale) {
            width *= applyCanvasScale ? canvasScale : 1;
            edgeEndWidth = width < minLineWidth ? minLineWidth : width;
        }



        // FUNCTIONALITY

        // Called when this component starts.
        // Prepares parts of the connection that do not depend on the edge or its settings.
        void Start() {

            // get line renderer component
            if (!lineRenderer) { lineRenderer = gameObject.GetComponent<LineRenderer>(); }
            if (!zLineRenderer) { zLineRenderer = gameObject.GetComponent<ZLineRenderer>(); }

            // apply min and max line width
            if (!zLineRenderer) { Debug.LogError("Edge Connection is missing ZLineRenderer component!"); }
            else {

                // is this edge from and to the same file?
                zLineRenderer.SetSameFileMode(fromToSameFile);
            }

            // get vfx control flow
            if (!vfxControlFlow) {
                vfxControlFlow = transform.Find("ControlFlowFx").GetComponent<VisualEffect>();

                if (!vfxControlFlow) {
                    Debug.LogError("VFX Effect is missing! Probably not set in the inspector!");
                }
            }

            if (!hoverPointPrefab) {
                Debug.LogError("Hover point prefab is null and probably needs to be assigned using the inspector!");
            }

            // create attachment
            startHoverPoint = CreateOuterHoverPoint("startHoverPoint", fromWindowRefs.transform);
            endHoverPoint = CreateOuterHoverPoint("endHoverPoint", toWindowRefs.transform);

            midHoverPoint = CreateMidHoverPoint("midHoverPoint", startHoverPoint.transform, endHoverPoint.transform);

            // prepare bezier curve and mappings
            PrepareAfterStart();
        }


	    void Update() {
		    
            // ToDo: remove unity line renderer if no longer required
            if (!lineRenderer && !zLineRenderer) { return; }

            if (successfulAttached && updateEdge) {
                UpdateLinePosition();
            }
	    }


        void OnDestroy() {
            
            updateEdge = false;

            // take care of cleaning up the start and end hover points 
            if (startHoverPoint) { Destroy(startHoverPoint); }
            if (midHoverPoint) { Destroy(midHoverPoint); }
            if (endHoverPoint) { Destroy(endHoverPoint); }

            var fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
            var edgeLoader = ApplicationLoader.GetInstance().GetEdgeLoader();

            var edgesWithSameStartMethod = edgeLoader.GetOutgoingEdgesOfMethod(fromWindowRefs.GetCodeFile(), edge.GetFrom().callMethodLines.from, ConfigManager.GetInstance().selectedConfig);
            var edgesWithSameStartAndEndMethod = edgeLoader.GetEdges(edge.GetFrom().file, edge.GetFrom().callMethodLines.from, edge.GetTo().file, edge.GetTo().lines.from, ConfigManager.GetInstance().selectedConfig);
            var edgesWithSameEndMethod = edgeLoader.GetRefEdgesOfMethod(toWindowRefs.GetCodeFile(), edge.GetTo().lines.from, ConfigManager.GetInstance().selectedConfig);
            var edgesGoingIntoCallMethod = edgeLoader.GetRefEdgesOfMethod(fromWindowRefs.GetCodeFile(), edge.GetFrom().callMethodLines.from, ConfigManager.GetInstance().selectedConfig);
            var edgesGoingOutOfTargetMethod = edgeLoader.GetOutgoingEdgesOfMethod(toWindowRefs.GetCodeFile(), edge.GetTo().lines.from, ConfigManager.GetInstance().selectedConfig);

            bool keepCallMethodHighlight = false;
            bool keepTargetMethodHighlight = false;
            bool keepCallGraphEdgeMarking = false;

            foreach (var e in edgesWithSameStartMethod) {
                if (edge.GetID() == e.GetID()) continue;
                if (fs.edgeSpawner.EdgeIsSpawned(e)) {
                    keepCallMethodHighlight = true;
                    break;
                }
            }

            foreach (var e in edgesGoingIntoCallMethod) {
                if (edge.GetID() == e.GetID()) continue;
                if (fs.edgeSpawner.EdgeIsSpawned(e)) {
                    keepCallMethodHighlight = true;
                    break;
                }
            }

            foreach (var e in edgesWithSameEndMethod) {
                if (edge.GetID() == e.GetID()) continue;
                if (fs.edgeSpawner.EdgeIsSpawned(e)) {
                    keepTargetMethodHighlight = true;
                    break;
                }
            }

            foreach (var e in edgesGoingOutOfTargetMethod) {
                if (edge.GetID() == e.GetID()) continue;
                if (fs.edgeSpawner.EdgeIsSpawned(e)) {
                    keepTargetMethodHighlight = true;
                    break;
                }
            }

            foreach (var e in edgesWithSameStartAndEndMethod) {
                if (edge.GetID() == e.GetID()) continue;
                if (fs.edgeSpawner.EdgeIsSpawned(e)) {
                    keepCallGraphEdgeMarking = true;
                }
            }

            if (CallMethodHighlight != null && !keepCallMethodHighlight) {
                fromWindowRefs.RemoveMethodHighlight(fromWindowRefs.GetCodeFile().GetNode().GetRelativePath(), edge.GetFrom().callMethodLines.from);
                Destroy(CallMethodHighlight.gameObject);
            }

            if (TargetMethodHighlight != null && !keepTargetMethodHighlight) {
                toWindowRefs.RemoveMethodHighlight(toWindowRefs.GetCodeFile().GetNode().GetRelativePath(), edge.GetTo().lines.from);
                Destroy(TargetMethodHighlight.gameObject);
            }

            if (ApplicationLoader.GetInstance().glimpsUserStudy && !keepCallGraphEdgeMarking) {
                var edgeSpawner = ApplicationLoader.GetInstance().gameObject.GetComponent<CodeWindowEdgeSpawner>();
                var connectionManager = edgeSpawner.connectionManager;
                if (connectionManager == null) {
                    connectionManager = GameObject.FindGameObjectsWithTag("ConnectionManager")[0];
                }

                if (connectionManager == null) {
                    Debug.LogError("Connection Manager was not found!");
                    return;
                }

                string connectionName = $"{edge.GetFrom().file.Replace('/', '.')}:{edge.GetFrom().callMethodLines.from} <> {edge.GetTo().file.Replace('/', '.')}:{edge.GetTo().lines.from}";
                var connectionComponent = connectionManager.transform.Find(connectionName).GetComponent<Connection>();
                //connectionComponent.points[0].color = Color.white;
                //connectionComponent.points[1].color = Color.white;
                connectionComponent.ChangeColor(Color.white);
            }
        }


        /// <summary>
        /// Creates the hover point and returns it.
        /// </summary>
        private GameObject CreateOuterHoverPoint(string name, Transform t) {
            
            // create hover point from prefab
            var hoverPoint = Instantiate(hoverPointPrefab);
            hoverPoint.name = name;

            // attach to this GameObject instance
            hoverPoint.transform.SetParent(t);
            hoverPoint.transform.rotation = t.rotation;

            // set size of spheres
            var hoverPointComponent = hoverPoint.GetComponent<HoverPoint>();
            if (attachmentSphereSize.magnitude > 0) {
                hoverPointComponent.ChangeSize(attachmentSphereSize.x / 2);
            }
            hoverPointComponent.AttachToControlFlowEdge(this);

            //// set material of spheres
            //if (attachmentSphereMat) {
            //    hoverPoint.GetComponent<Renderer>().material = attachmentSphereMat;
            //}

            //// remove colliders
            //Destroy(hoverPoint.GetComponent<Collider>());

            // set invisible
            hoverPoint.SetActive(false);

            return hoverPoint;
        }

        /// <summary>
        /// Creates the hover point and returns it.
        /// </summary>
        private GameObject CreateMidHoverPoint(string name, Transform startHoverPoint, Transform endHoverPoint) {

            // create hover point from prefab
            var hoverPoint = Instantiate(hoverPointPrefab);
            hoverPoint.name = name;

            hoverPoint.transform.SetParent(startHoverPoint.parent);
            hoverPoint.transform.rotation = Quaternion.Lerp(startHoverPoint.rotation, endHoverPoint.rotation, 0.5f);
            hoverPoint.transform.position = Vector3.Lerp(startHoverPoint.position, endHoverPoint.position, 0.5f);

            hoverPoint.GetComponent<HoverPoint>().closeEdgeOnClick = true;

            // set size of spheres
            var hoverPointComponent = hoverPoint.GetComponent<HoverPoint>();
            if (attachmentSphereSize.magnitude > 0) {
                hoverPointComponent.ChangeSize(attachmentSphereSize.x / 2);
            }
            hoverPointComponent.AttachToControlFlowEdge(this);
            hoverPoint.SetActive(true);

            return hoverPoint;
        }


        /// <summary>
        /// Prepare the initialization of this edge connection.<para/>
        /// This will set the edge.
        /// </summary>
        public void Prepare(Edge edge) {

            // set the edge this connection represents
            SetEdge(edge);

            // the span of the enclosed connection region
            startSpan = edge.GetFrom().lines.to - edge.GetFrom().lines.from;
            endSpan = edge.GetTo().lines.to - edge.GetTo().lines.from;
        }

        /// <summary>
        /// Apply the edge settings mapping for this type of edge after this component started.
        /// </summary>
        private void PrepareAfterStart() {

            // get setting for this edge type
            ValueMappingsLoader mappingsLoader = ApplicationLoader.GetInstance().GetMappingsLoader();
            EdgeSetting settings = mappingsLoader.GetEdgeSetting(edge.GetEdgeType());
            SetEdgeSetting(settings);
            ApplyEdgeSettingMethods(settings, true, true);

            // create bezier curve instance and generate a random curve strength noise
            bezierCurve = new BezierCurve();
            rndCurveStrengthNoise = GenerateRandomCurveStrengthNoise(curveStrengthNoise);
        }


        /// <summary>
        /// Prepare and spawn the edge objects.<para/>
        /// Ensure to set the edge before calling this method by using the "Prepare" method!<para/>
        /// </summary>
        public bool InitConnection(CodeFileReferences fromCodeFileInstance, CodeFileReferences toCodeFileInstance) {
            fromWindowRefs = fromCodeFileInstance;
            toWindowRefs = toCodeFileInstance;

            updateEdge = true;
            successfulAttached = false;
            string base_err = "Failed to spawn edge";
            if (edge == null) {
                Debug.LogError(base_err + " - edge instance is null!");
                updateEdge = false;
                return false;
            }

            // get relative file paths that user entered
            string file1 = fromCodeFileInstance.GetCodeFile().GetNode().GetPath(); //edge.GetFrom().file.ToLower();
            string file2 = toCodeFileInstance.GetCodeFile().GetNode().GetPath(); //edge.GetTo().file.ToLower();

            // get according code files (null if not found)
            CodeFile cf1 = fromCodeFileInstance.GetCodeFile();
            CodeFile cf2 = toCodeFileInstance.GetCodeFile();

            // get target region
            int offset = 1; // dirty offset to handle methods with annotations

            TargetRegion = cf2.GetRegion(edge.GetTo().lines.from + offset, ARProperty.TYPE.NFP);
            if (TargetRegion == null) {
                Debug.LogWarning("Target region was not found!");
            }

            // code file error handling
            string file1_err = "(file: " + file1 + ")";
            string file2_err = "(file: " + file2 + ")";
            if (cf1 == null) { Debug.LogError(base_err + " - missing code file 1! " + file1_err); return false; }
            if (fromCodeFileInstance == null) { Debug.LogError(base_err + " - missing code file 1 references! " + file1_err); return false; }
            if (cf2 == null) { Debug.LogError(base_err + " - missing code file 2! " + file2_err); return false; }
            if (toCodeFileInstance == null) { Debug.LogError(base_err + " - missing code file 2 references! " + file2_err); return false; }            

            if (fromCodeFileInstance == toCodeFileInstance) { fromToSameFile = true; }

            // create point transforms telling where the connection should start and end
            GameObject p1 = new GameObject("EdgeStartAnchor", typeof(RectTransform));
            GameObject p2 = new GameObject("EdgeEndAnchor", typeof(RectTransform));
            startPoint = p1.transform;
            endPoint = p2.transform;

            // attach them to the code window canvas to automatically update
            // the world positions based on rotation, movement and scroll
            AttachPoint(p1.GetComponent<RectTransform>(), edge.GetFrom().lines.from, fromWindowRefs);
            AttachPoint(p2.GetComponent<RectTransform>(), edge.GetTo().lines.from, toWindowRefs);

            // get distances between left and right next to each window
            // and convert them from canvas units in world units (mult with scale)
            from_leftRightDist = fromWindowRefs.GetEdgePoints().GetLeftRightDistance() * startPoint.lossyScale.x;
            to_leftRightDist = toWindowRefs.GetEdgePoints().GetLeftRightDistance() * endPoint.lossyScale.x;

            updateEdge = true;
            successfulAttached = true;
            return true;
        }

        /// <summary>
        /// Forces update of the edge positions.
        /// </summary>
        public void ForcePositionUpdate() {
            UpdateLinePosition(true);
        }

        /// <summary>Initially attach the point / "node" to the canvas of the code file.</summary>
        /// <param name="pointSpan">How big the region is that this connection encloses.</param>
        private bool AttachPoint(RectTransform point, int startLine, CodeFileReferences fileRefs) {

            // get code file and line information
            CodeFile codeFile = fileRefs.GetCodeFile();
            if (!codeFile.IsLineInfoSet()) { return false; }

            CodeFile.LineInformation lineInfo = codeFile.GetLineInfo();
            int lineCount = lineInfo.lineCount;
            if (startLine > lineCount) { return false; }

            // get line height and store global one
            float thisLineHeight = lineInfo.lineHeight;
            if (thisLineHeight == 0) { return false; }
            lineHeight = thisLineHeight;
            
            // set initial edge curve width taking the line height into account
            SetEdgeStartWidth(startSpan * lineHeight, true);
            SetEdgeEndWidth(endSpan * lineHeight, true);

            // attach to parent container and take its position and scale factor
            Transform anchorContainer = fileRefs.GetEdgePoints().anchorContainer;
            if (!anchorContainer) { return false; }
            point.SetParent(anchorContainer, false);

            // prepare rect transform
            point.anchorMin = new Vector2(0, 1);
            point.anchorMax = new Vector2(0, 1);
            point.sizeDelta = new Vector2(5, 5);

            // set position according to the line number (node)
            float curLinePos = startLine / (float) lineCount;
            float pxErr = curLinePos * ((float) fileRefs.GetTextElements().Count-1) * ERROR_PER_ELEMENT;
            float yPosOffset = lineHeight * 0.5f; // to get correct position ("middle of line")
            float yPos = (startLine - 1) * lineHeight + yPosOffset - pxErr;
            //float xPos = fileRefs.GetEdgePoints().left; // if left or right will be decided on update
            float xPos = fileRefs.GetEdgePoints().linkOffset;
            point.anchoredPosition = new Vector2(xPos, -yPos); // yPos needs to be a negative value!
            return true;
        }


        /// <summary>Recalculates and changes the line position.</summary>
        private void UpdateLinePosition(bool forceUpdate = false) {

            // check if we even have to update
            if (!IsUpdateRequired() && !forceUpdate) { return; }


            // get position left and right next to the "from" window
            //Vector3 from_leftPos = startPoint.position;
            //Vector3 from_rightPos = startPoint.position + startPoint.right * from_leftRightDist;

            // get position left and right next to the "to" window
            //Vector3 to_leftPos = endPoint.position;
            //Vector3 to_rightPos = endPoint.position + endPoint.right * to_leftRightDist;

            // ==> [UPDATE: no longer affected by hor. scroll] ==>

            // get position left and right next to the "from" window
            Vector3 from_ea_left = fromWindowRefs.GetLeftEdgeConnection(); // edge attachment left

            // correction vector that is needed because of horizontal scrolling
            Vector3 correction = Vector3.Dot(fromWindowRefs.GetEdgePoints().topLeft.position - startPoint.position, startPoint.right) * startPoint.right;
            Vector3 from_leftPos = startPoint.position + correction;
            Vector3 from_rightPos = from_leftPos + startPoint.right * from_leftRightDist;

            // get position left and right next to the "to" window [UPDATE: no longer affected by hor. scroll]
            Vector3 to_ea_left = toWindowRefs.GetLeftEdgeConnection(); // edge attachment left

            correction = Vector3.Dot(toWindowRefs.GetEdgePoints().topLeft.position - endPoint.position, endPoint.right) * endPoint.right;
            Vector3 to_leftPos = endPoint.position + correction;
            Vector3 to_rightPos = to_leftPos + endPoint.right * to_leftRightDist;


            // check which connection is closer (false = left, true = right)
            // (e.g. lr means "from-window-left" to "to-window-right")
            // (always round the result to avoid "fast switching" when almost the same value!)
            float distance_ll = Mathf.Round(Vector3.Distance(from_leftPos, to_leftPos) * 100) / 100;
            float shortest = distance_ll;
            bool attachFromLeft = true, attachToLeft = true;

            float distance_lr = Mathf.Round(Vector3.Distance(from_leftPos, to_rightPos) * 100) / 100;
            if (distance_lr < shortest) {
                shortest = distance_lr;
                attachFromLeft = true; attachToLeft = false;
            }

            float distance_rr = Mathf.Round(Vector3.Distance(from_rightPos, to_rightPos) * 100) / 100;
            if (distance_rr < shortest) {
                shortest = distance_rr;
                attachFromLeft = false; attachToLeft = false;
            }

            float distance_rl = Mathf.Round(Vector3.Distance(from_rightPos, to_leftPos) * 100) / 100;
            if (distance_rl < shortest) {
                shortest = distance_rl;
                attachFromLeft = false; attachToLeft = true;
            }

            // line start is position at "from-window" and end at "to-window"
            LineStart = attachFromLeft ? from_leftPos : from_rightPos;
            LineEnd = attachToLeft ? to_leftPos : to_rightPos;

            // check if the position / region is in the "content bounds" and adjust accordingly
            bool startOutOfBounds = false;
            bool endOutofBounds = false;

            // ignore multiple lines for now
            startSpan = 0;
            if (startSpan > 0) {
                float startSpanFinal = startSpan * GetLineHeight() * toWindowRefs.transform.lossyScale.y * canvasScale;
                LineStart = ValidateBoundsRegion(true, LineStart, startSpanFinal, fromWindowRefs, attachFromLeft, out startOutOfBounds, startHoverPoint);
            }
            else {
                LineStart = ValidateBounds(LineStart, fromWindowRefs, attachFromLeft, out startOutOfBounds, startHoverPoint);
            }

            // ignore multiple lines for now
            endSpan = 0;
            if (endSpan > 0) {
                float endSpanFinal = endSpan * GetLineHeight() * toWindowRefs.transform.lossyScale.y * canvasScale;
                LineEnd = ValidateBoundsRegion(false, LineEnd, endSpanFinal, toWindowRefs, attachToLeft, out endOutofBounds, endHoverPoint);
            }
            else {
                LineEnd = ValidateBounds(LineEnd, toWindowRefs, attachToLeft, out endOutofBounds, endHoverPoint);
            }


            // if we have a from-to the same file connection out of bounds
            if (fromToSameFile) {

                bool lineRendererState = !(startOutOfBounds && endOutofBounds);

                if (zLineRenderer) {

                    // UPDATE: following is no longer desired! (do not hide if out of bounds in same file)
                    // do not render line mesh
                    //lineRenderer.gameObject.GetComponent<MeshRenderer>().enabled = lineRendererState;

                    // hide spheres
                    if (!lineRendererState) {
                        if (startHoverPoint) { startHoverPoint.SetActive(false); }
                        if (endHoverPoint) { endHoverPoint.SetActive(false); }
                    }
                }

                // UPDATE: following is no longer desired! (do not hide if out of bounds in same file)
                //if (!lineRendererState) { return; }
            }



            // Does not always work due to the way the default line renderer works!
            /*
            // make this object look in the correct direction so that
            // we always get a straight line using renderer alignment "transform Z"
            Vector3 middle = lineStart + (lineEnd - lineStart) * 0.5f;
            Quaternion newRotation = Quaternion.Euler(middle) * Quaternion.Euler(0, 90, 0);
            newRotation.x = newRotation.z = 0; // only use rotation around "up" axis
            transform.rotation = newRotation;
            */


            // Positioning now done in ValidateBounds
            /*
            // position attachment spheres and set active state according to out of bounds
            PositionSphere(startSphere, startOutOfBounds, lineStart);
            PositionSphere(endSphere, endOutofBounds, lineEnd);
            */



            // CREATE CURVE (type of curve is based on steps - less than 4 is linear)

            // regenerate the random curve strength noise if values changed
            float rndStrength = rndCurveStrengthNoise;
            float finalCurveStrength = curveStrength + rndStrength;
            if (finalCurveStrength < 0 || finalCurveStrength > 1) {
                rndCurveStrengthNoise = GenerateRandomCurveStrengthNoise(curveStrengthNoise);
                finalCurveStrength = curveStrength + rndCurveStrengthNoise;
            }

            // set curve steps
            bezierCurve.SetSteps(curveSteps);

            Vector3 startSide = startPoint.right * (attachFromLeft ? -1 : 1);
            Vector3 endSide = endPoint.right * (attachToLeft ? -1 : 1);

            if (!fromToSameFile) {

                // if dist is 0, then its a linear line - 2 steps speed up calculation
                float dist = Vector3.Distance(LineStart, LineEnd);
                if (dist == 0) { bezierCurve.SetSteps(2); }

                float multiplier = finalCurveStrength * dist;
                ControlPoint1 = LineStart + startSide * multiplier;
                ControlPoint2 = LineEnd + endSide * multiplier;
            }
            else {
                float windowWidth = Vector3.Distance(fromWindowRefs.GetEdgePoints().topLeft.position,
                    fromWindowRefs.GetEdgePoints().topRight.position);
                float windowHeight = Vector3.Distance(fromWindowRefs.GetEdgePoints().topLeft.position,
                    fromWindowRefs.GetEdgePoints().bottomLeft.position);
                float min = Mathf.Min(windowWidth, windowHeight);
                ControlPoint1 = LineStart + startPoint.up * 0.025f + startSide * (finalCurveStrength * min * 0.67f);
                ControlPoint2 = LineEnd + endPoint.up * -0.025f + endSide * (finalCurveStrength * min * 0.67f);
            }

            Vector3[] curvePoints = bezierCurve.CalculatePoints(LineStart, ControlPoint1, ControlPoint2, LineEnd);
            UpdateMidHoverPoint(curvePoints[curvePoints.Length / 2]);
            UpdateVFXControlFlow(LineStart, ControlPoint1, ControlPoint2, LineEnd, CurveLength(curvePoints));


            // ToDo: remove unity line renderer if no longer required
            if (lineRenderer) {
                lineRenderer.positionCount = (int)curveSteps;
                lineRenderer.SetPositions(curvePoints);
            }

            if (zLineRenderer) {

                // update the width curve start and end width
                int keyCount = zLineRenderer.widthCurve.keys.Length;
                Keyframe[] keys = zLineRenderer.widthCurve.keys;
                if (startSpan > 0) { keys[0].value = GetEdgeStartWidth(); }
                if (endSpan > 0) { keys[keyCount - 1].value = GetEdgeEndWidth(); }
                zLineRenderer.widthCurve.keys = keys;

                // calculate scale direction vector
                CodeFileReferences.EdgeAnchors edgePoints = toWindowRefs.GetEdgePoints();
                Vector3 winTop = attachFromLeft ? edgePoints.topLeft.position : edgePoints.topRight.position;
                Vector3 winBottom = attachFromLeft ? edgePoints.bottomLeft.position : edgePoints.bottomRight.position;
                Vector3 scaleDirection = Vector3.Normalize(winTop - winBottom);

                // set new curve positions -> curve will be refreshed
                zLineRenderer.SetPositions(curvePoints, scaleDirection);
            }
        }

        private float CurveLength(Vector3[] points) {
            float length = 0f;

            for (int i = 0; i < points.Length - 1; i++) {
                length += (points[i + 1] - points[i]).magnitude;
            }

            return length;
        }

        private GradientColorKey[] colorKeys = new GradientColorKey[2];
        private void UpdateVFXControlFlow(Vector3 controlPoint0, Vector3 controlPoint1, Vector3 controlPoint2, Vector3 controlPoint3, float length) {
            vfxControlFlow.SetVector3("ControlPoint0", controlPoint0);
            vfxControlFlow.SetVector3("ControlPoint1", controlPoint1);
            vfxControlFlow.SetVector3("ControlPoint2", controlPoint2);
            vfxControlFlow.SetVector3("ControlPoint3", controlPoint3);

            vfxControlFlow.SetFloat("TimePerRun", length / vfxParticleSpeed);
            vfxControlFlow.SetFloat("ParticleCount", Mathf.RoundToInt(length / distanceBetweenParticles));
            vfxControlFlow.SetFloat("SpawnInterval", distanceBetweenParticles / vfxParticleSpeed);

            Gradient gradient = vfxControlFlow.GetGradient("Gradient");

            colorKeys = gradient.colorKeys;
            colorKeys[1].color = GetRegionColor(TargetRegion);
            gradient.SetKeys(colorKeys, gradient.alphaKeys);
            vfxControlFlow.SetGradient("Gradient", gradient);
        }

        private Color GetRegionColor(Region region) {

            var defaultColor = vfxControlFlow.GetGradient("Gradient").colorKeys[0].color;            

            if (region == null) {
                // Debug.LogError("Region is null!");
                return defaultColor;
            }

            // get the currently selected property to show
            string activeNFP = ApplicationLoader.GetInstance().GetAppSettings().GetSelectedNFP().ToLower();
            var nfpProp = region.GetProperty(ARProperty.TYPE.NFP, activeNFP) as RProperty_NFP;
            if (nfpProp == null || !nfpProp.GotValue()) { return defaultColor; }

            var vml = ApplicationLoader.GetInstance().GetMappingsLoader();
            var setting = vml.GetNFPSetting(activeNFP);

            // get color method and method min/max                
            var minMax = RegionModifier.GetMinMaxValues(
                nfpProp.GetName(), toWindowRefs.GetCodeFile(), setting.GetMinMaxValue());

            if (minMax == null) {
                Debug.LogError("Failed to get MinMaxValue from RegionModifier!");
                return defaultColor;
            }

            AColorMethod colMethod;
            if (ApplicationLoader.GetApplicationSettings().ComparisonMode) {
                colMethod = setting.GetMinMaxColorMethod(Settings.ApplicationSettings.NFP_VIS.CODE_MARKING, minMax.GetMinValue(), minMax.GetMaxValue());
            }
            else {
                colMethod = setting.GetColorMethod(Settings.ApplicationSettings.NFP_VIS.CODE_MARKING);
            }

            // get absolute value and crop it to the bounds
            float absValue = minMax.CropToBounds(nfpProp.GetValue());

            // return relative color
            float valuePercentage = minMax.GetRangePercentage(absValue);
            return colMethod.Evaluate(valuePercentage);
        }


        /// <summary>
        /// Generates a random noise used to modify the curve strength.<para/>
        /// It also ensures that adding it to the curve strength results in a range of [0, 1].
        /// </summary>
        private float GenerateRandomCurveStrengthNoise(float maxNoise) {
            
            // decide if we use a positive or negative noise
            sbyte sign = (sbyte) (Random.Range(0, 11) < 5 ? 1 : -1);

            // get how much "strength" is left when positive or negative
            float strengthLeft_pos = 1 - curveStrength; // for positive
            float strengthLeft_neg = curveStrength; // for negative
            float strengthLeft = sign > 0 ? strengthLeft_pos : strengthLeft_neg;

            // use the opposite sign if there is none left for the selected one
            if (strengthLeft == 0) { sign *= -1; }
            strengthLeft = sign > 0 ? strengthLeft_pos : strengthLeft_neg;

            // generate the random noise and apply the sign
            float max = maxNoise > strengthLeft ? strengthLeft : maxNoise;
            float rndNoise = Random.Range(0, max);
            return rndNoise * sign;
        }


        /// <summary>
        /// Validate the current position against the window content bounds.<para/>
        /// Returns the valid position which can be the current if its valid.
        /// </summary>
        /// <param name="attachLeft">If the current position is left attached</param>
        /// <param name="curPos">The current position to be validated</param>
        /// <param name="outOfBounds">Tells if this check resulted in out of bounds</param>
        private Vector3 ValidateBounds(Vector3 curPos, CodeFileReferences winRefs, bool attachLeft, out bool outOfBounds, GameObject sphere) {

            CodeFileReferences.EdgeAnchors edgePoints = winRefs.GetEdgePoints();
            Vector3 winTop = attachLeft ? edgePoints.topLeft.position : edgePoints.topRight.position;
            Vector3 winBottom = attachLeft ? edgePoints.bottomLeft.position : edgePoints.bottomRight.position;
            Vector3 posOut = curPos;

            // check if out of viewport bounds
            outOfBounds = false;
            //if (curPos.y > winRefs.GetViewportTop().y) {
            if (curPos.y > winTop.y) { 
                posOut = winTop;
                outOfBounds = true;
            }
            //else if (curPos.y < winRefs.GetViewportBottom().y) {
            else if (curPos.y < winBottom.y) {
                posOut = winBottom;
                outOfBounds = true;
            }

            // position the sphere
            PositionSphere(sphere, outOfBounds, posOut);

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

            Vector3 top_btm_norm = Vector3.Normalize(viewportBtm - viewportTop);

            // [DEBUG]
            if (showRegionDebugGizmos) { debug_pos = posOut; }


            // check if region is completely out of bounds at top
            outOfBounds = false;
            Vector3 dist_top = curPos - viewportTop;
            float y_dist_top = curPos.y - viewportTop.y;
            if (y_dist_top >= span) {

                PositionSphere(sphere, true, posOut); // hide sphere

                // set at min line width
                if (start) { SetEdgeStartWidth(0, false); }
                else { SetEdgeEndWidth(0, false); }

                outOfBounds = true;
                return winTop;
            }

            // check if region is completely out of bounds at bottom
            Vector3 dist_btm = curPos - viewportBtm;
            float y_dist_btm = curPos.y - viewportBtm.y;
            if (y_dist_btm <= 0) {

                PositionSphere(sphere, true, posOut); // hide sphere

                // set at min line width
                if (start) { SetEdgeStartWidth(0, false); }
                else { SetEdgeEndWidth(0, false); }

                outOfBounds = true;
                return winBottom;
            }


            // check if part of the region is out of bounds at the top and/or btm window border
            bool region_outOfBounds_top = curPos.y > viewportTop.y;
            bool region_outOfBounds_btm = curPos.y - (span * -top_btm_norm).y < viewportBtm.y;
            float newEdgeWidth = span;

            // part of the region covers whole content size -> edge attachment pos at middle of content
            if (region_outOfBounds_top && region_outOfBounds_btm) {

                Vector3 btm_top = (viewportTop - viewportBtm);
                posOut = (viewportBtm + btm_top * 0.5f);

                // hide sphere
                PositionSphere(sphere, true, posOut);

                newEdgeWidth = btm_top.magnitude;
            }

            // part of the region is out at the top
            else if (region_outOfBounds_top) {

                float width_left = span - dist_top.magnitude;
                posOut = (viewportTop + top_btm_norm * (width_left * 0.5f));

                // sphere at end of region
                Vector3 regionEnd = curPos;
                regionEnd = (viewportTop + top_btm_norm * width_left);
                PositionSphere(sphere, false, regionEnd);

                newEdgeWidth = width_left;
            }

            // part of the region is out at the bottom
            else if (region_outOfBounds_btm) {

                float width_left = dist_btm.magnitude;
                Vector3 btm_top_norm = Vector3.Normalize(viewportTop - viewportBtm);
                posOut = (viewportBtm + btm_top_norm * (width_left * 0.5f));

                // sphere at start of region
                PositionSphere(sphere, false, curPos);

                newEdgeWidth = width_left;
            }

            // the whole region is visible
            else {
                posOut = (curPos + top_btm_norm * (span * 0.5f));

                // hide sphere
                PositionSphere(sphere, true, posOut);
            }


            // [DEBUG]
            if (showRegionDebugGizmos) { debug_pos_edge = posOut; }


            // set new start/end edge line width
            if (start) { SetEdgeStartWidth(newEdgeWidth, false); }
            else { SetEdgeEndWidth(newEdgeWidth, false); }

            return posOut;
        }


        /// <summary>Position and set active state of the sphere accordingly.</summary>
        private void PositionSphere(GameObject sphere, bool outOfBounds, Vector3 position) {

            if (!sphere) { return; }
            if (outOfBounds) { sphere.SetActive(false); }
            else {
                sphere.transform.position = position;
                sphere.SetActive(true);
            }
        }

        private void UpdateMidHoverPoint(Vector3 position) {
            midHoverPoint.transform.rotation = Quaternion.Lerp(startHoverPoint.transform.rotation, endHoverPoint.transform.rotation, 0.5f);
            midHoverPoint.transform.position = position;
        }


        /// <summary>
        /// Tells if an edge update is required by checking
        /// attributes of the two windows like position, rotation and scroll.
        /// </summary>
        private bool IsUpdateRequired() {

            // check if start- and endpoint position changed
            // (if so, position, rotation or scroll changed)
            if (startPoint.position != previousStartPointPos ||
                endPoint.position != previousEndPointPos) {
                previousStartPointPos = startPoint.position;
                previousEndPointPos = endPoint.position;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Called when this edge should update its visual look based on the setting.<para/>
        /// All mapping methods will be applied properly.<para/>
        /// For instance, this can result in updated colors and/or widths.
        /// </summary>
        /// <param name="setting">The setting to use for this update</param>
        /// <param name="updateColors">Required if the edge uses colors of regions it connects to</param>
        /// <param name="updateWidth">It is not required to update the width again, so think about only doing so once</param>
        public void ApplyEdgeSettingMethods(EdgeSetting setting, bool updateColors, bool updateWidth) {

            if (setting == null) { return; }

            if (updateColors) {

                Color fromColor = setting.GetColor(0);
                Color toColor = setting.GetColor(1);
                
                //float valuePercentage = GetEdgeValuePercentage(setting.GetColorMethod().GetRange()); // ToDo: cleanup
                float valuePercentage = GetEdgeValuePercentage(setting.GetMinMaxValue());

                // assign color with respect to "relative_to" property of edge setting!
                switch (setting.GetRelativeTo()) {

                    case EdgeSetting.Relative.VALUE: // a fixed color changes depending on value

                        Color valueColor = setting.GetColor(valuePercentage);
                        fromColor = toColor = valueColor;
                        break;

                    case EdgeSetting.Relative.REGION: // color is taken from connection regions

                        // ToDo: implement coloring by connected regions

                        break;
                }
                
                // create color gradient for line renderer to use
                Gradient newColor = new Gradient {
                    colorKeys = new GradientColorKey[] {
                        new GradientColorKey(fromColor, 0),
                        new GradientColorKey(toColor, 1)
                    }
                };

                // apply color method on line renderer component
                if (zLineRenderer) { zLineRenderer.SetColor(newColor); }
                if (lineRenderer) { lineRenderer.colorGradient = newColor; }
            }


            if (updateWidth) {

                // turn min and max height into percentage floats
                float minHeightPerc = setting.GetWidth(0) / 100f;
                float maxHeightPerc = setting.GetWidth(1) / 100f;

                // map to min and max line width
                float minHeight = minLineWidth + minHeightPerc * (maxLineWidth - minLineWidth);
                float maxHeight = minLineWidth + maxHeightPerc * (maxLineWidth - minLineWidth);
                float minMaxRange = maxHeight - minHeight;


                // set the fixed width method which is just a basic line
                AnimationCurve widthCurve = new AnimationCurve();

                // prepare keys for start and end width of the line
                widthCurve.AddKey(0, minLineWidth);
                widthCurve.AddKey(0.1f, minHeight);
                widthCurve.AddKey(0.9f, minHeight);
                widthCurve.AddKey(1, minLineWidth);

                // if a width scale method is used (use animation curve with e-function instead of a linear)
                if (setting.GetWidth(0) != setting.GetWidth(1)) {

                    // get the value in percentage to determine the absolute height later
                    // float valuePercentage = GetEdgeValuePercentage(setting.GetWidthMethod().GetRange()); // ToDo: cleanup
                    float valuePercentage = GetEdgeValuePercentage(setting.GetMinMaxValue());

                    // curve settings
                    float midStep = 0.5f; // middle of curve
                    float midSpan = 0.125f; // how much to go left and right from mid
                    uint samples = setting.GetSteps() / 2; // curve samples per half curve
                    if (samples > 100) { samples = 100; }
                    //float sample_step = midSpan * 2 / samples; // step per sample

                    if (samples > 0) {

                        // min height where curve starts and ends and max in middle
                        int k1 = widthCurve.AddKey(midStep - midSpan, minHeight);
                        widthCurve.AddKey(midStep, minHeight + valuePercentage * minMaxRange);
                        int k2 = widthCurve.AddKey(midStep + midSpan, minHeight);


                        // INFO: NOT REQUIRED NOW BC. CURVE IS SMOOTHED EVEN WITH 3 POINTS

                        // sample the half curve created by the e-function
                        // (skip middle and end section because we already set them)
                        /*
                        for (uint sample = 1; sample < samples-2; sample++) {
                    
                            float x = Mathf.Round(sample * sample_step * 1000f) / 1000f;
                            float y = Mathf.Exp(-Mathf.Pow(x * 10, 2)) * valuePercentage;

                            // ensure we stay in bounds
                            if (y < 0.01) { y = 0; }
                            else if (y > height) { y = height; }

                            // ensure we don't overwrite keys at start and end of the curve
                            if (midStep + x < midStep + midSpan) {
                                widthCurve.AddKey(midStep + x, minHeight + y * minMaxRange);
                                widthCurve.AddKey(midStep - x, minHeight + y * minMaxRange);
                            }
                        }
                        */

                        // disable curve smoothing
                        if (k1 > 0) { AnimationUtility.SetKeyLeftTangentMode(widthCurve, k1, AnimationUtility.TangentMode.Linear); }
                        if (k2 > 0) { AnimationUtility.SetKeyRightTangentMode(widthCurve, k2, AnimationUtility.TangentMode.Linear); }
                    }
                }

                // disable animation curve smoothing at specific points
                int keysTotal = widthCurve.keys.Length;
                AnimationUtility.SetKeyRightTangentMode(widthCurve, 0, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyLeftTangentMode(widthCurve, 1, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(widthCurve, 1, AnimationUtility.TangentMode.Linear);

                if (keysTotal > 2) {
                    AnimationUtility.SetKeyRightTangentMode(widthCurve, keysTotal-2, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyLeftTangentMode(widthCurve, keysTotal-2, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyLeftTangentMode(widthCurve, keysTotal-1, AnimationUtility.TangentMode.Linear);
                }

                // apply width method on line renderer component
                if (zLineRenderer) { zLineRenderer.widthCurve = widthCurve; }
                if (lineRenderer) { lineRenderer.widthCurve = widthCurve; }
            }
        }


        /// <summary>
        /// Calculates and returns the edge value percentage [0, 1]
        /// regarding the min/max value for this edge type.<para/>
        /// It uses the calculates min/max type or - if provided -
        /// the passed min- and/or max values explicitly set in the settings.
        /// </summary>
        private float GetEdgeValuePercentage(MinMaxValue methodMinMax = null) {

            // calculate the value percentage of this edge
            EdgeLoader edgeLoader = ApplicationLoader.GetInstance().GetEdgeLoader();
            MinMaxValue edgeMinMax = edgeLoader.GetEdgeTypeMinMax(edge.GetEdgeType());
            float edgePercentageValue = 0;

            if (edgeMinMax != null) {
                
                // use the calculated min/max value of this edge type
                float emin = edgeMinMax.GetMinValue();
                float emax = edgeMinMax.GetMaxValue();

                // map value to min/max of size method instead of edge min max if set
                // (method min/max overwrites calculated min/max)
                if (methodMinMax != null) {
                    if (methodMinMax.IsMinValueSet()) { emin = methodMinMax.GetMinValue(); }
                    if (methodMinMax.IsMaxValueSet()) { emax = methodMinMax.GetMaxValue(); }
                }

                float edgeMMRange = emax - emin;
                if (edgeMMRange != 0) { edgePercentageValue = (edge.GetValue() - emin) / edgeMMRange; }
            }

            return edgePercentageValue;
        }



        // DEBUG!
        void OnDrawGizmos() {
            
            if (showRegionDebugGizmos) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(debug_pos, 0.05f);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(debug_pos_edge, 0.05f);
            }

            if (showControlPointGizmos) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(ControlPoint1, 0.05f);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(ControlPoint2, 0.05f);
            }
        }

    }
}
