using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.Fallback;
using VRVis.Interaction.LaserPointer;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Utilities;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Holds information about a single element of the code city.<para/>
    /// An instance of this script is attached to the spawned elements.<para/>
    /// Last-Updated: 22.08.2019
    /// </summary>
    public class CodeCityElement : MonoBehaviour, IPointerClickHandler {

        /// <summary>Node of the partitioning tree</summary>
	    private CodeCityV1.PNode pNode;

        /// <summary>Contains useful information about the element</summary>
        private Dictionary<string, string> info = new Dictionary<string, string>();
        private bool infoBuilt = false;


        // GETTER/SETTER

        /// <summary>Get according node of the structure tree representing a file or folder.</summary>
        public SNode GetSNode() { return pNode.corNode; }

        /// <summary>Assign the partitioning tree node. (Should only be used by the CodeCity itself!)</summary>
        public void SetNode(CodeCityV1.PNode pNode) { this.pNode = pNode; }

        /// <summary>
        /// Returns useful information about the element as a dictionary.<para/>
        /// Only builds the whole dictionary once on the first call.<para/>
        /// After that, only changing entries are updated.
        /// </summary>
        public IDictionary<string, string> GetInfo() {

            if (pNode == null) { infoBuilt = true; }

            // only perform partial updates
            if (infoBuilt) {

                // ToDo: consider that height/width, ... may change as well in future versions
                AddSpawnedStateToInfo();
                return info;
            }

            info.Clear();
            info.Add("Name", pNode.corNode.GetName());
            info.Add("Type", pNode.corNode.GetNodeType().ToString());
            
            // sub-element count and similar could also be calculated recursively
            // [to see an example have a look at GetSubElementCount()]

            // prepare additional height information
            float ch = (float) Math.Round(pNode.cityHeight, 2);
            string heightAdd = Utility.ToStr(ch);
            if (pNode.isLeaf) {
                float hp = (float) Math.Round(pNode.heightPercentage * 100, 2);
                heightAdd += ", " + Utility.ToStr(hp) + " %";
            }

            info.Add("Width", pNode.GetWidth().ToString());
            info.Add("Length", pNode.GetLength().ToString());
            info.Add("Height", pNode.GetHeight().ToString() + " (" + heightAdd + ")");

            // show sub-elements of folders
            if (pNode.corNode.IsFolder()) { info.Add("Sub-Elements", pNode.subElements.ToString()); }

            // add info about spawned state (this needs to be updated on every new call)
            AddSpawnedStateToInfo();

            infoBuilt = true;
            return info;
        }


        // FUNCTIONALITY

        /// <summary>Retrieves the amount of sub-elements.</summary>
        private uint GetSubElementCount(CodeCityV1.PNode node) {
            if (node == null) { return 0; }
            return GetSubElementCountRecursive(node.left) + GetSubElementCountRecursive(node.right);
        }

        private uint GetSubElementCountRecursive(CodeCityV1.PNode node) {
            if (node == null) { return 0; }
            if (node.isLeaf || node.isPackage) { return 1; }
            return GetSubElementCountRecursive(node.left) + GetSubElementCountRecursive(node.right);
        }


        /// <summary>Adds information about the spawned state to the info dictionary.</summary>
        private void AddSpawnedStateToInfo() {

            // tells if a file is already spawned
            if (pNode.corNode.IsFile()) {

                FileSpawner fs = ApplicationLoader.GetInstance().GetSpawner("FileSpawner") as FileSpawner;
                if (fs != null) {
                    bool spawned = fs.IsFileSpawned(pNode.corNode.GetFullPath());
                    info["Spawned"] = spawned ? "yes" : "no";
                }
            }
        }


        // EVENT HANDLING

        public void OnPointerClick(PointerEventData eventData) {
            
            Debug.Log("City element pointer click: " + GetSNode().GetName());
            eventData.Use();

            // called from fallback camera (mouse click)
            MouseNodePickup.MousePickupEventData e = eventData as MouseNodePickup.MousePickupEventData;
            if (e != null) {
                MouseNodePickup mnp = e.GetMNP();
                if (e.button.Equals(PointerEventData.InputButton.Left)) {
                    mnp.AttachFileToSpawn(GetSNode(), e.pointerCurrentRaycast.worldPosition);
                }
            }

            // called from laser pointer controller
            LaserPointerEventData d = eventData as LaserPointerEventData;
            if (d != null) {
                ViveUILaserPointerPickup p = d.controller.GetComponent<ViveUILaserPointerPickup>();
                if (p) {
                    p.StartCodeWindowPlacement(GetSNode(), transform);
                }
            }
            
        }

    }
}
