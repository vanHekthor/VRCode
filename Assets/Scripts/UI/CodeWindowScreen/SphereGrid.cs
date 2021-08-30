using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Utilities;

namespace VRVis.UI.Helper {

    /// <summary>
    /// Grid that can be added to a sphere and can hold object that have a GridElement component. 
    /// </summary>
    public class SphereGrid : MonoBehaviour {

        [Tooltip("Sphere that shall have a grid")]
        public GameObject screenSphere;

        [Tooltip("Represents the anchors grid elements can snap to")]
        public GameObject gridPointObject;

        [Tooltip("Polar spacing (left-right) in degrees between grid point objects")]
        public float polarSpacing;

        [Tooltip("Elevation spacing (top-bottom) in degrees between grid point objects")]
        public float elevationSpacing;

        [Tooltip("Distance to sphere surface")]
        public float distanceToSphereSurface;

        [Tooltip("Number of points each grid layer has")]
        public int GridPointsPerLayer;

        public List<List<SphereGridPoint>> Grid { get; private set; } 

        private const float PolarOrigin = Mathf.PI;
        private const float ElevationOrigin = 0f;

        private const int LayerCount = 2;        

        private Vector3 sphereOrigin;
        private float sphereRadius;

        void Start() {
            if (!screenSphere) {
                Debug.LogError("Missing screen sphere reference!");
            }

            if (!gridPointObject) {
                Debug.LogError("Missing grid point object reference for the sphere grid!");
            }

            sphereOrigin = screenSphere.transform.position;
            sphereRadius =
                screenSphere.GetComponent<SphereCollider>().radius * screenSphere.transform.lossyScale.x;

            Grid = GeneratePointsOnSphere();
            InstantiateGrid(Grid);
        }

        public SphereGridPoint GetGridPoint(int layerIdx, int columnIdx) {
            return Grid[layerIdx][columnIdx];
        }

        /// <summary>
        /// Properly attaches a grid element to the grid point at the passed indices. Checks if the grid point
        /// is already occupied. Ensures that the grid element knows where it is attached to and that the grid point
        /// has a reference to the grid element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="layerIdx"></param>
        /// <param name="columnIdx"></param>
        /// <returns></returns>
        public bool AttachGridElement(ref GridElement element, int layerIdx, int columnIdx) {
            if (!Grid[layerIdx][columnIdx].Occupied) {

                Grid[layerIdx][columnIdx].AttachedElement = element;

                // This does not work as expected. Becomes null after a while for unknown reasons.
                // Better use the indices for referencing the grid point this grid element is attached to. 
                element.AttachedTo = Grid[layerIdx][columnIdx]; 

                // Using these indices works as expected.
                element.GridPositionLayer = layerIdx;
                element.GridPositionColumn = columnIdx;

                SetGridPointOccupied(layerIdx, columnIdx, true);

                Debug.Log("Successfully attached element to grid point ("
                    + "layer: "+ layerIdx + ", column: " + columnIdx + ")");

                return true;
            }

            Debug.LogWarning("Attaching element to grid failed, because grid point ("
                + "layer: " + layerIdx + ", column: " + columnIdx + ") is already occupied!");
            return false;
        }

        /// <summary>
        /// Properly detaches a grid element from the grid. The grid point where the grid element was attached to
        /// can be occupied again. Attributs of the grid point referencing the grid element and vice versa get cleared.
        /// </summary>
        /// <param name="element"></param>
        public void DetachGridElement(ref GridElement element) {
            SetGridPointOccupied(element.GridPositionLayer, element.GridPositionColumn, false);
            Grid[element.GridPositionLayer][element.GridPositionColumn].AttachedElement = null;
            element.AttachedTo = null;

            int layerIdx = element.GridPositionLayer;
            int columnIdx = element.GridPositionColumn;

            element.GridPositionLayer = -1;
            element.GridPositionColumn = -1;

            Debug.Log("Detached element from grid point ("
                    + "layer: " + layerIdx + ", column: " + columnIdx + ")");
        }

        public bool IsOccupied(int layerIdx, int columnIdx) {
            return Grid[layerIdx][columnIdx].Occupied;
        }

        public SphereGridPoint GetClosestGridPoint(Vector3 point) {
            SphereGridPoint closestGridPoint = null;
            float minDistance = Mathf.Infinity;

            foreach (List<SphereGridPoint> layer in Grid) {
                foreach (SphereGridPoint gridPoint in layer) {
                    if (!gridPoint.Occupied) {
                        float distance = (gridPoint.Position - point).sqrMagnitude;

                        if (distance < minDistance) {
                            minDistance = distance;
                            closestGridPoint = gridPoint;
                        }
                    }
                }
            }

            return closestGridPoint;
        }

        public SphereGridPoint GetLeftNeighbor(int layerIdx, int columnIdx) {
            return GetGridPoint(layerIdx, (columnIdx + 1) % GridPointsPerLayer);
        }

        public SphereGridPoint GetLeftNeighbor(SphereGridPoint gridPoint) {
            return GetGridPoint(gridPoint.LayerIdx, (gridPoint.ColumnIdx + 1) % GridPointsPerLayer);
        }

        public SphereGridPoint GetRightNeighbor(int layerIdx, int columnIdx) {
            return GetGridPoint(layerIdx, (columnIdx - 1 + GridPointsPerLayer) % GridPointsPerLayer);
        }

        public SphereGridPoint GetRightNeighbor(SphereGridPoint gridPoint) {
            return GetGridPoint(gridPoint.LayerIdx, 
                (gridPoint.ColumnIdx - 1 + gridPoint.ColumnIdx) % GridPointsPerLayer);
        }

        public SphereGridPoint GetTopNeighbor(int layerIdx, int columnIdx) {
            if (layerIdx >= 0 && layerIdx < LayerCount - 1) {
                return GetGridPoint(layerIdx + 1, columnIdx);
            }
            return null;
        }

        public SphereGridPoint GetTopNeighbor(SphereGridPoint gridPoint) {
            if (gridPoint.LayerIdx >= 0 && gridPoint.LayerIdx < LayerCount - 1) {
                return GetGridPoint(gridPoint.LayerIdx + 1, gridPoint.ColumnIdx);    
            }
            return null;
        }

        public SphereGridPoint GetBottomNeighbor(int layerIdx, int columnIdx) {
            if (layerIdx >= 1 && layerIdx < LayerCount) {
                return GetGridPoint(layerIdx - 1, columnIdx);
            }
            return null;
        }

        public SphereGridPoint GetBottomNeighbor(SphereGridPoint gridPoint) {
            if (gridPoint.LayerIdx >= 1 && gridPoint.LayerIdx < LayerCount) {
                return GetGridPoint(gridPoint.LayerIdx - 1, gridPoint.ColumnIdx);
            }
            return null;
        }

        /// <summary>
        /// Creates the visual representation of the grid in the scene using the grid point objects.
        /// </summary>
        /// <param name="grid">Grid defining the locations where grid point objects need to be placed.</param>
        private void InstantiateGrid(List<List<SphereGridPoint>> grid) {
            foreach (List<SphereGridPoint> layer in grid) {
                foreach (SphereGridPoint gridPoint in layer) {
                    Vector3 lookDirection = screenSphere.transform.position - gridPoint.Position;

                    Quaternion rotation = Quaternion.LookRotation(lookDirection);

                    Instantiate(gridPointObject, gridPoint.Position, rotation);
                }
            }
        }

        /// <summary>
        /// Generates the grid of SphereGridPoints according to the settings made in the inspector.
        /// </summary>
        /// <returns></returns>
        private List<List<SphereGridPoint>> GeneratePointsOnSphere() {
            List<List<SphereGridPoint>> gridLayers = new List<List<SphereGridPoint>>();

            float gridWidthAngle = (GridPointsPerLayer - 1) * polarSpacing * Mathf.PI / 180f;
            float gridHeightAngle = (LayerCount - 1) * elevationSpacing * Mathf.PI / 180f;

            float radius = sphereRadius + distanceToSphereSurface;
            
            for (int layerNum = 0; layerNum < LayerCount; layerNum++) {

                List<SphereGridPoint> layer = new List<SphereGridPoint>();

                for (int columnNum = 0; columnNum < GridPointsPerLayer; columnNum++) {
                    float polarOffset = columnNum * polarSpacing * Mathf.PI / 180f - gridWidthAngle / 2f;
                    float elevationOffset = layerNum * elevationSpacing * Mathf.PI / 180f;

                    Vector3 gridPointPos = PositionOnSphere.SphericalToCartesian(
                            radius,
                            PolarOrigin + polarOffset,
                            ElevationOrigin + elevationOffset,
                            screenSphere.transform.position);

                    Vector3 lookDirection = (sphereOrigin - gridPointPos).normalized;
                    Vector3 attachmentPos = gridPointPos + distanceToSphereSurface * lookDirection;

                    SphereGridPoint gridAnchorPoint = new SphereGridPoint(
                        layerNum,
                        columnNum,                        
                        gridPointPos,
                        attachmentPos
                    );

                    layer.Add(gridAnchorPoint);
                }
                gridLayers.Add(layer);
            }

            return gridLayers;
        }

        private void SetGridPointOccupied(int layerIdx, int columnIdx, bool occupied) {
            Grid[layerIdx][columnIdx].Occupied = occupied;
        }
    }

}
