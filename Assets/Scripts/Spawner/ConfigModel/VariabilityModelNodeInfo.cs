using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Effects;
using VRVis.IO;
using VRVis.IO.Features;

namespace VRVis.Spawner.ConfigModel {

    /// <summary>
    /// This script should be attached to each GameObject instance,
    /// that represents a node of the variability model.<para/>
    /// The attachment should not happen on runtime but already on the prefab.<para/>
    /// It holds information like the level in the hierarchy and so on.<para/>
    /// The component can be used for events when interacting with the objects.
    /// </summary>
    public class VariabilityModelNodeInfo : MonoBehaviour {

        [Tooltip("Needs to be set to attach child nodes correctly")]
        public Transform childNodesParent;

        [Tooltip("Required to show a connection line between this node and its parent")]
        public ConnectionLine connectionLine;

        [Tooltip("Tells where the line starts from")]
        public Transform connectionLineStart;

        [Tooltip("Tells where the line starts from when a notation is involved")]
        public Transform connectionLineStartNotation;

        [Tooltip("Tells where a line to this node should be attached without a notation")]
        public Transform connectionLineEnd;

        [Tooltip("Tells where a line to this node should be attached with a notation")]
        public Transform connectionLineEndNotation;

        [Tooltip("Tells where to put the connection notation above the node")]
        public Transform notationPositionTop;

        [Tooltip("Tells where to put the connection notation below the node")]
        public Transform notationPositionBottom;

        [Tooltip("Show/hide attachments in editor")]
        public bool showGizmos = true;

        private VariabilityModel vm; // ToDo: replace later by "configuration"
        private int optionIndex = -1; // index position of represented option
        [SerializeField] private int level = -1;
        [SerializeField] private float radius = 0;

        private bool notationTop = false;
        private bool notationBottom = false;
        private bool childrenAreGroup = false;



        // GETTER AND SETTER

        public void SetInformation(VariabilityModel vm, int optionIndex, int level, float radius) {
            this.vm = vm;
            this.optionIndex = optionIndex;
            this.level = level;
            this.radius = radius;
        }

        public VariabilityModel GetVariabilityModel() { return vm; }

        /// <summary>Get the index of the represented option/feature.</summary>
        public int GetIndex() { return optionIndex; }

        /// <summary>Get level in the graph.</summary>
        public int GetLevel() { return level; }

        public float GetRadius() { return radius; }

        public Transform GetChildNodesParent() { return childNodesParent; }


        public bool IsNotationTopSet() { return notationTop; }

        public bool IsNotationBottomSet() { return notationBottom; }


        public bool AreChildrenGroup() { return childrenAreGroup; }

        public void SetChildrenAreGroup(bool group) { childrenAreGroup = group; }

        /// <summary>Returns the according option or null if not found.</summary>
        public AFeature GetOption() {
            return ApplicationLoader.GetInstance().GetVariabilityModel().GetOption(optionIndex);
        }


        /// <summary>
        /// Returns either the top anchor of the node or the anchor for if a notation is set.<para/>
        /// Which one depends on if a notation at the top is set yet or not.
        /// </summary>
        public Transform GetAttachmentTop() {
            if (!IsNotationTopSet()) { return connectionLineStart; }
            return connectionLineStartNotation;
        }


        /// <summary>
        /// Returns either the bottom anchor of the node or the anchor for if a notation is set.<para/>
        /// Which one depends on if a notation at the bottom is set yet or not.
        /// </summary>
        public Transform GetAttachmentBottom() {
            if (!IsNotationBottomSet()) { return connectionLineEnd; }
            return connectionLineEndNotation;
        }



        // FUNCTIONALITY

        /// <summary>
        /// Draw attachment positions in editor.
        /// </summary>
        void OnDrawGizmos() {
            
            if (!showGizmos) { return; }

            if (notationPositionTop) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(notationPositionTop.position, new Vector3(0.08f, 0.01f, 0.08f));
            }

            if (notationPositionBottom) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(notationPositionBottom.position, new Vector3(0.08f, 0.01f, 0.08f));
            }


            if (connectionLineStart) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(connectionLineStart.position, 0.01f);
                Gizmos.DrawLine(connectionLineStart.position, connectionLineStart.position + Vector3.up * 0.1f);
            }

            if (connectionLineStartNotation) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(connectionLineStartNotation.position, 0.01f);
                Gizmos.DrawLine(connectionLineStartNotation.position, connectionLineStartNotation.position + new Vector3(0.5f, 1, 0) * 0.1f);
            }


            if (connectionLineEnd) {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(connectionLineEnd.position, 0.01f);
                Gizmos.DrawLine(connectionLineEnd.position, connectionLineEnd.position + Vector3.down * 0.1f);
            }

            if (connectionLineEndNotation) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(connectionLineEndNotation.position, 0.01f);
                Gizmos.DrawLine(connectionLineEndNotation.position, connectionLineEndNotation.position + new Vector3(0.5f, -1, 0) * 0.1f);
            }
        }

        /// <summary>
        /// Set the notation above the node to be the passed one.<para/>
        /// Will also do the required parenting and remove a possible already set notation.<para/>
        /// Pass null to remove the notation.
        /// </summary>
        public void SetNotationTop(GameObject notationInstance) {
            
            // remove possible old notation by clearing children of notationPositionTop
            if (IsNotationTopSet()) {
                foreach (Transform child in notationPositionTop) {
                    if (child != notationPositionTop) { Destroy(child); }
                }
            }

            // mark as "not set yet"
            notationTop = false;
            if (!notationInstance) { return; }

            // mark as set and do parenting accordingly
            notationTop = true;
            notationInstance.transform.SetParent(notationPositionTop, false);
        }

        /// <summary>
        /// Set the notation above the node to be the passed one.<para/>
        /// Will also do the required parenting and remove a possible already set notation.<para/>
        /// Pass null to remove the notation.
        /// </summary>
        public void SetNotationBottom(GameObject notationInstance) {
            
            // remove possible old notation by clearing children of notationPositionTop
            if (IsNotationBottomSet()) {
                foreach (Transform child in notationPositionBottom) {
                    if (child != notationPositionBottom) { Destroy(child); }
                }
            }

            // mark as "not set yet"
            notationBottom = false;
            if (!notationInstance) { return; }

            // mark as set and do parenting accordingly
            notationBottom = true;
            notationInstance.transform.SetParent(notationPositionBottom, false);
        }


        /// <summary>
        /// Updates the color accordingly.<para/>
        /// For boolean options, this will result in green or red whether it is selected or not.<para/>
        /// This method will not have any affect on numeric options.
        /// </summary>
        public void UpdateColor() {

            AFeature option = ApplicationLoader.GetInstance().GetVariabilityModel().GetOption(GetIndex());
            if (option == null) { return; }
            if (!(option is Feature_Boolean)) { return; }

            // get first visual object (for now)
            Transform visual = transform.Find("Visuals");
            if (visual.childCount > 0) { visual = visual.GetChild(0); }

            Renderer r = visual.GetComponent<Renderer>();
            if (!r) { return; }

            Feature_Boolean oBool = (Feature_Boolean) option;
            if (oBool.IsSelected(false)) { r.material.color = Color.green; }
            else { r.material.color = Color.red; }
        }

    }
}
