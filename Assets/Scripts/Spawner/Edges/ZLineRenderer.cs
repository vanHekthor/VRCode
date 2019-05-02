using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Utilities;

namespace VRVis.Spawner.Edges {

    /// <summary>
    /// Trying to get rid of the buggy line renderer.<para/>
    /// Should generate a line in 3D space where faces are extruded
    /// always in the same direction.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ZLineRenderer : MonoBehaviour {

        public Vector3[] positions;

        [Tooltip("Shows segment directions as gizmos in scene view")]
        public bool showSegmentDirections = false;
        public bool useAlmostFix = false;

        [Tooltip("Width curve between 0 and 1 telling about the width of the line segment at a position")]
        public AnimationCurve widthCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public Gradient color;

        public enum ScaleAlong { X, Y, Z };
        public ScaleAlong scaleAlong = ScaleAlong.Y; // fixed at Y-axis for now

        public bool useWorldSpace = true;

        private Mesh lineMesh;
        private bool updateRequired = false;

        // ToDo: Can be removed if not required. (Currently unused - just leftover of an idea)
        /// <summary>If set, stops vertices from being generated above/below the value</summary>
        private MinMaxValue vertexMinMax = new MinMaxValue();

        /// <summary>Tells if this is an edge from-to the same file.</summary>
        private bool sameFileMode = false;

        private Vector3[] dirs; // for segments directions



        // CONSTRUCTOR

	    void Start () {
            
            lineMesh = new Mesh();
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter) { meshFilter.mesh = lineMesh; }
            updateRequired = true;
	    }
	    


        // GETTER AND SETTER

        /// <summary>Set the positions array</summary>
        public void SetPositions(Vector3[] positions) {
            this.positions = positions;
            updateRequired = true;
        }

        public void SetColor(Gradient colorGradient) { color = colorGradient; }
        
        public MinMaxValue GetVertexMinMax() { return vertexMinMax; }

        /// <summary>Min value according to "scale along" vector.</summary>
        public void SetVertexMin(float min) { vertexMinMax.SetMinValue(min); }

        /// <summary>Max value according to "scale along" vector.</summary>
        public void SetVertexMax(float max) { vertexMinMax.SetMaxValue(max); }

        /// <summary>Set if the line is from-to the same file.</summary>
        public void SetSameFileMode(bool enabled) { sameFileMode = enabled; }


        
        // FUNCTIONALITY

	    void Update () {
            if (updateRequired) { UpdateMesh(); }
	    }

        /// <summary>Called if e.g. a value was changed in inspector.</summary>
        void OnValidate() {
            updateRequired = true;
        }


        /// <summary>Updates the mesh representing the line.</summary>
        private void UpdateMesh() {

            updateRequired = false;
            lineMesh.Clear();

            // nothing to do
            if (positions == null || positions.Length < 1) { return; }
            
            Vector3 scaleAlongAxis = Vector3.zero;
            switch (scaleAlong) {
                case ScaleAlong.X: scaleAlongAxis = new Vector3(1, 0, 0); break;
                case ScaleAlong.Y: scaleAlongAxis = new Vector3(0, 1, 0); break;
                case ScaleAlong.Z: scaleAlongAxis = new Vector3(0, 0, 1); break;
            }

            if (sameFileMode) {
                bool invertRotDir = positions[positions.Length-1].y > positions[0].y ? true : false;
                lineMesh.vertices = GenerateVerticesFromToSameFile(invertRotDir);
            }
            else { lineMesh.vertices = GenerateVertices(scaleAlongAxis); }

            lineMesh.colors = GenerateVertexColors(lineMesh.vertexCount);
            lineMesh.triangles = GenerateTriangles(lineMesh.vertexCount);
            lineMesh.RecalculateNormals();
            lineMesh.RecalculateBounds();
        }

        void OnDrawGizmos() {

            if (showSegmentDirections && dirs != null && dirs.Length > 0) {
                Gizmos.color = Color.red;
                for (int i = 0; i < dirs.Length; i += 2) {
                    Gizmos.DrawSphere(dirs[i], 0.01f);
                    Gizmos.DrawLine(dirs[i], dirs[i] + dirs[i+1]);
                }
            }
        }

        /// <summary>
        /// Generate vertices from the given positions and returns them.<para/>
        /// Takes the width curve and scale axis into account.
        /// </summary>
        private Vector3[] GenerateVertices(Vector3 scaleAlong) {
            
            // two vertices per position -> two times the size
            Vector3[] vertices = new Vector3[positions.Length * 2];
            if (positions.Length < 1) { return vertices; }

            Vector3 direction = Vector3.zero;
            int vertexIndex = 0;
            int curveSamples = positions.Length - 1;

            if (showSegmentDirections) { dirs = new Vector3[positions.Length * 2]; }
            int dirIndex = 0;

            // for "almost" fix
            Vector3 totalDir = Vector3.Normalize(positions[positions.Length-1] - positions[0]);
            Vector3 totalSideVec = Vector3.zero;
            bool totalSideVecSet = false;
            //Vector3 prevDir = Vector3.zero;
            //bool prevDirSet = false;
            float flip = 1;

            for (int i = 0; i < positions.Length; i++) {
                
                // get position and according width of this segment
                Vector3 pos = positions[i];
                float t = curveSamples != 0 ? i / (float) curveSamples : 0;

                // animation width curve encodes absolute line width
                float width = widthCurve.Evaluate(t);
                float halfWidth = width * 0.5f;

                direction = scaleAlong;

                // ToDo: Fix if required (not required for our current use case - works good enough)
                /*
                // calculate direction in which face extrudes
                if (i > 0) {
                    Vector3 thisDir = positions[i] - positions[i-1];
                    Vector3 planeVec = pos + direction;
                    Vector3 rotVector = Vector3.Cross(thisDir, planeVec);
                    direction = Vector3.Normalize(Quaternion.Euler(rotVector * 90) * thisDir);
                }
                */

                // works almost but normals turn
                if (useAlmostFix) {
                    if (i > 0 && i < positions.Length - 1) {
                        //if (i == 2) { flip = 1; }
                        Vector3 prevPos = positions[i-1];
                        Vector3 nextPos = positions[i+1];
                        Vector3 dirVec = Vector3.Normalize(nextPos - prevPos);
                        //Vector3 sideVec = Vector3.Cross(dirVec, scaleAlong);
                        if (!totalSideVecSet) { totalSideVec = Vector3.Cross(dirVec, totalDir); totalSideVecSet = true; }
                        //Vector3 sideVec = Vector3.Cross(dirVec, totalSideVec);
                        direction = Vector3.Cross(dirVec, totalSideVec);
                        direction.Normalize();
                        //if (prevDirSet && Vector3.Angle(prevDir, direction) > 160) { flip *= -1; }
                        //prevDir = direction;
                        //prevDirSet = true;
                    }
                    //else { flip = -1; }
                }

                Vector3 v1 = pos + direction * halfWidth * flip; // (e.g. "up")
                Vector3 v2 = pos - direction * halfWidth * flip; // (e.g. "down")

                if (showSegmentDirections) {
                    dirs[dirIndex++] = pos;
                    dirs[dirIndex++] = direction * 0.5f * flip;
                }

                // check max (e.g. up) if set according to scale along vector
                if (vertexMinMax.IsMaxValueSet()) {
                    
                    float maxMag = Vector3.Scale(v1, scaleAlong).magnitude;
                    Vector3 maxVec = scaleAlong * vertexMinMax.GetMaxValue();

                    // set min value according to scale along vector
                    v1.x = maxVec.x != 0 ? maxVec.x : v1.x;
                    v1.y = maxVec.y != 0 ? maxVec.y : v1.y;
                    v1.z = maxVec.z != 0 ? maxVec.z : v1.z;
                }

                // check min (e.g. down) if set according to scale along vector
                if (vertexMinMax.IsMinValueSet()) {
                    
                    float minMag = Vector3.Scale(v2, scaleAlong).magnitude;
                    Vector3 minVec = scaleAlong * vertexMinMax.GetMinValue();

                    // set min value according to scale along vector
                    v2.x = minVec.x != 0 ? minVec.x : v2.x;
                    v2.y = minVec.y != 0 ? minVec.y : v2.y;
                    v2.z = minVec.z != 0 ? minVec.z : v2.z;
                }


                // transform the positions from local to world space
                if (useWorldSpace) {
                    v1 = transform.InverseTransformPoint(v1);
                    v2 = transform.InverseTransformPoint(v2);
                }

                vertices[vertexIndex++] = v1;
                vertices[vertexIndex++] = v2;
            }

            return vertices;
        }

        /// <summary>
        /// Generate vertices from the given positions and returns them.<para/>
        /// Takes the width curve and scale axis into account.<para/>
        /// This is for the "from-to same file mode".
        /// </summary>
        /// <param name="invertRotationDir">Invert the rotation direction</param>
        private Vector3[] GenerateVerticesFromToSameFile(bool invertRotationDir=true) {
            
            // two vertices per position -> two times the size
            Vector3[] vertices = new Vector3[positions.Length * 2];

            int vertexIndex = 0;
            int curveSamples = positions.Length - 1;

            // default direction and where to rotate it around
            Vector3 direction_default = transform.up;
            Vector3 direction_rotAxis = transform.forward;

            // rotate direction around Z-axis
            float rotStep = 180 / (float) (positions.Length-1);
            float inv = invertRotationDir ? -1 : 1;

            for (int i = 0; i < positions.Length; i++) {
                
                // get position and according width of this segment
                Vector3 pos = positions[i];
                float t = curveSamples != 0 ? i / (float) curveSamples : 0;

                // animation width curve encodes absolute line width
                float width = widthCurve.Evaluate(t);
                float halfWidth = width * 0.5f;

                // use rotated direction (around local forward / Z-axis)
                Vector3 direction = Quaternion.AngleAxis(i * rotStep * inv, transform.forward) * direction_default;

                Vector3 v1 = pos + direction * halfWidth; // (e.g. "up")
                Vector3 v2 = pos - direction * halfWidth; // (e.g. "down")

                // transform the positions from local to world space
                if (useWorldSpace) {
                    v1 = transform.InverseTransformPoint(v1);
                    v2 = transform.InverseTransformPoint(v2);
                }

                vertices[vertexIndex++] = v1;
                vertices[vertexIndex++] = v2;
            }

            return vertices;
        }


        /// <summary>
        /// Generate the colors at the vertex positions based on the color gradient.
        /// </summary>
        private Color[] GenerateVertexColors(int vertexCount) {

            Color[] vertexColors = new Color[vertexCount];

            int positionIndex = 0;
            int colorSamples = positions.Length - 1;

            for (int i = 0; i <= vertexCount-2; i += 2) {

                float t = colorSamples != 0 ? positionIndex / (float) colorSamples : 0;
                Color vColor = color.Evaluate(t);
                vertexColors[i] = vColor;
                vertexColors[i + 1] = vColor;
                positionIndex++;
            }

            return vertexColors;
        }

        /// <summary>
        /// Generate triangles based on the vertex indices to generate quads.
        /// </summary>
        private int[] GenerateTriangles(int vertexCount) {

            int[] triangles = new int[(positions.Length - 1) * 6];

            // generate one quad per step
            int triangleIndex = 0;
            for (int i = 0; i < vertexCount-2; i += 2) {
                
                // first triangle
                triangles[triangleIndex++] = i;
                triangles[triangleIndex++] = i + 1;
                triangles[triangleIndex++] = i + 2;

                // second triangle
                triangles[triangleIndex++] = i + 1;
                triangles[triangleIndex++] = i + 3;
                triangles[triangleIndex++] = i + 2;
            }

            return triangles;
        }

    }

    /*
    [CustomEditor(typeof(ZLineRenderer))]
    [CanEditMultipleObjects]
    public class ZLineRendererEditor : Editor {

        public override void OnInspectorGUI() {
            
            ZLineRenderer script = (ZLineRenderer) target;

            script.material = (Material) EditorGUILayout.ObjectField("Material", script.material, typeof(Material), false); // false = only assets allows
        }
    }
    */
}
