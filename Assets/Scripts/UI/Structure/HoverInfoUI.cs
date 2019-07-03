using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.Spawner.Structure;

namespace VRVis.UI.Structure {

    /// <summary>
    /// A script used by the VR pointer to show information
    /// about the element the user is currently hovering above.<para/>
    /// E.g. pointing at a file or folder shows name and path ...<para/>
    /// Attach this component to the file and folder prefabs.
    /// </summary>
    public class HoverInfoUI : MonoBehaviour {

        public GameObject uiPrefab;

        [Header("Local Position And Rotation")]
        public Vector3 uiPosition;
        public Vector3 uiRotation;

        private GameObject uiInstance;


        /// <summary>
        /// Called through SendMessage method call from Laser Pointer.
        /// </summary>
        public void PointerEntered(Hand hand) {
            AttachUI(hand);
        }

        /// <summary>
        /// Called through SendMessage method call from Laser Pointer.
        /// </summary>
        public void PointerExit(Hand hand) {
            DetachUI();
        }


        /// <summary>
        /// Spawn the UI for this hand.
        /// </summary>
        private void AttachUI(Hand hand) {

            // parent to controller or to hand itself
            Transform parentTo = hand.transform;
            if (hand.currentAttachedObject != null) { parentTo = hand.currentAttachedObject.transform; }
            uiInstance = Instantiate(uiPrefab, parentTo);
            uiInstance.transform.localPosition = uiPosition;
            uiInstance.transform.localRotation = Quaternion.Euler(uiRotation);

            // set according structure information
            UpdateUIInfo();
        }


        /// <summary>
        /// Detach hover UI from hand.
        /// </summary>
        private void DetachUI() {
            if (uiInstance) { Destroy(uiInstance); }
        }


        void Update() {

            if (uiInstance) {

                // ignore x and z rotation of UI
                Quaternion rotMod = uiInstance.transform.rotation;
                rotMod.x = rotMod.z = 0;
                uiInstance.transform.rotation = rotMod;
            }
        }


        /// <summary>
        /// Update the shown information using structure nodes.
        /// </summary>
        void UpdateUIInfo() {

            // we can get the node information from this object
            // because this component is attached to it
            StructureNodeInfoV2 nInfV2 = GetComponent<StructureNodeInfoV2>();
            if (nInfV2) { SetUIInfo(nInfV2.GetSNode()); return; }

            Debug.LogWarning("Failed to set UI info! - No node found.", this);
        }


        /// <summary>
        /// Apply the new information.
        /// </summary>
        private void SetUIInfo(SNode n) {

            StructureHoverInfo shi = uiInstance.GetComponent<StructureHoverInfo>();
            if (shi == null) { return; }

            shi.SetName(n.GetName());
            shi.SetPath(n.GetPath());

            ApplicationLoader loader = ApplicationLoader.GetInstance();
            if (loader == null) {
                Debug.LogError("Missing application loader instance!", this);
                return;
            }

            // show additional info
            StringBuilder additionalInfo = new StringBuilder();
            if (n.GetNodeType() == SNode.DNodeTYPE.FILE) {

                // spawned information
                FileSpawner fs = (FileSpawner) loader.GetSpawner("FileSpawner");
                if (fs != null) {
                    bool spawned = fs.IsFileSpawned(n.GetFullPath());
                    additionalInfo.Append("- Spawned: ");
                    additionalInfo.Append(spawned ? "<b><color=#92d992>yes</color></b>" : "no");
                    additionalInfo.Append("\n");
                }

                // get codefile instance
                CodeFile cf = null;
                if (loader.GetStructureLoader() != null) {
                    cf = loader.GetStructureLoader().GetFileByFullPath(n.GetFullPath());
                }

                // outgoing edges
                if (loader.GetEdgeLoader() != null && cf != null) {
                    additionalInfo.Append("- Edges outgoing: ");
                    additionalInfo.Append(loader.GetEdgeLoader().GetEdgeCountOfFile(cf));
                }
            }
            else if (n.GetNodeType() == SNode.DNodeTYPE.FOLDER) {

                // ToDo: this could be done in a previous step! (while loading the structure)
                // gather information
                uint folders = 0;
                uint files = 0;
                foreach (SNode cn in n.GetNodes()) {
                    switch (cn.GetNodeType()) {
                        case SNode.DNodeTYPE.FILE: files++; break;
                        case SNode.DNodeTYPE.FOLDER: folders++; break;
                    }
                }

                // files info
                additionalInfo.Append("- Files: ");
                additionalInfo.Append(files);
                additionalInfo.Append('\n');

                // folders info
                additionalInfo.Append("- Folders: ");
                additionalInfo.Append(folders);
                additionalInfo.Append('\n');
            }

            shi.SetInfo(additionalInfo.ToString());
        }

    }
}
