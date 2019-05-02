using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Effects;
using VRVis.IO.Structure;

namespace VRVis.Spawner.Structure {

    /// <summary>
    /// This script should be attached to game objects representing structure nodes.<para/>
    /// It holds information about the file or folder this node represents
    /// as well as information about the line connecting this node with another.
    /// </summary>
    public class StructureNodeInfoV2 : MonoBehaviour {

	    public SNode representedNode;

        [Tooltip("Needs to be set to attach child nodes correctly")]
        public Transform childNodesParent;

        [Tooltip("Required to show a connection line between this node and its parent")]
        public ConnectionLine connectionLine;

        [Tooltip("Tells where the line starts from")]
        public Transform connectionLineStart;

        [Tooltip("Tells where a line to this node should be attached without a notation")]
        public Transform connectionLineEnd;

        [Tooltip("Show/hide attachments in editor")]
        public bool showGizmos = true;

        // tells if this node is currently selected to be spawned
        private bool selectedForSpawning = false;
        
        [SerializeField] private int level = -1;
        [SerializeField] private float radius = 0;



        // GETTER AND SETTER

        public void SetInformation(SNode node, int level, float radius) {
            representedNode = node;
            this.level = level;
            this.radius = radius;
        }

        public SNode GetSNode() { return representedNode; }

        public bool IsSelectedForSpawning() { return selectedForSpawning; }
        public void SetSelectedForSpawning(bool selected) { selectedForSpawning = selected; }

        public int GetLevel() { return level; }

        public float GetRadius() { return radius; }

        public Transform GetChildNodesParent() { return childNodesParent; }

        /// <summary>Returns the top anchor of the node.</summary>
        public Transform GetAttachmentTop() { return connectionLineStart; }

        /// <summary>Returns the bottom anchor of the node.</summary>
        public Transform GetAttachmentBottom() { return connectionLineEnd; }



        // FUNCTIONALITY

        /// <summary>
        /// Draw attachment positions in editor.
        /// </summary>
        void OnDrawGizmos() {

            if (!showGizmos) { return; }

            if (connectionLineStart) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(connectionLineStart.position, 0.01f);
                Gizmos.DrawLine(connectionLineStart.position, connectionLineStart.position + Vector3.up * 0.1f);
            }

            if (connectionLineEnd) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(connectionLineEnd.position, 0.01f);
                Gizmos.DrawLine(connectionLineEnd.position, connectionLineEnd.position + Vector3.down * 0.1f);
            }
        }

    }
}
