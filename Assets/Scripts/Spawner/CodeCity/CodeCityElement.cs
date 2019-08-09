using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO.Structure;
using VRVis.Utilities;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Holds information about a single element of the code city.<para/>
    /// An instance of this script is attached to the spawned elements.
    /// </summary>
    public class CodeCityElement : MonoBehaviour {

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

        /// <summary>Returns useful information about the element as a dictionary.</summary>
        public Dictionary<string, string> GetInfo() {

            if (pNode == null) { infoBuilt = true; }
            if (infoBuilt) { return info; }

            info.Clear();
            info.Add("Name", pNode.corNode.GetName());
            info.Add("Type", pNode.corNode.GetNodeType().ToString());
            info.Add("Sub-Elements", pNode.subElements.ToString());
            
            // sub-element count and similar can be calculated recursively
            // [to see an example have a look at GetSubElementCount()]

            // prepare additional height information
            float ch = (float) Math.Round(pNode.cityHeight, 2);
            string heightAdd = Utility.ToStr(ch);
            if (pNode.isLeaf) {
                float hp = (float) Math.Round(pNode.heightPercentage * 100, 2);
                heightAdd += ", " + Utility.ToStr(hp) + " %";
            }

            info.Add("Height", pNode.GetHeight().ToString() + " (" + heightAdd + ")");
            info.Add("Width", pNode.GetWidth().ToString());
            info.Add("Length", pNode.GetLength().ToString());

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

    }
}
