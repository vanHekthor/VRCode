using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.Helper {

    /// <summary>
    /// Grid point of a SphereGrid. Objects with a GridElement component can be attached here.
    /// </summary>
    public class SphereGridPoint {

        public Vector3 Position { get; set; }

        public Vector3 AttachmentPoint { get; set; }

        public int ColumnIdx { get; }

        public int LayerIdx { get; }

        public bool Occupied { get; set; }

        public GridElement AttachedElement { get; set; }

        public GameObject AttachmentPointObject { get; set; }

        public SphereGridPoint(int layerIdx, int columnIdx, Vector3 position) {
            ColumnIdx = columnIdx;
            LayerIdx = layerIdx;
            Position = position;
            AttachmentPoint = position;
            Occupied = false;
        }

        public SphereGridPoint(int layerIdx, int columnIdx, Vector3 position, Vector3 attachmentPoint) {
            ColumnIdx = columnIdx;
            LayerIdx = layerIdx;
            Position = position;
            AttachmentPoint = attachmentPoint;
            Occupied = false;
        }
    }

}
