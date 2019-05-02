using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO.Structure;


namespace VRVis.Spawner.Structure {

    /// <summary>
    /// This script should be attached to game objects representing structure nodes.<para/>
    /// It holds information about the file or folder this node represents.<para/>
    /// This makes it easier to find the correct information.
    /// </summary>
    public class StructureNodeInfo : MonoBehaviour {

	    public SNode representedNode;

        // tells if this node is currently selected to be spawned
        private bool selectedForSpawning = false;


        // GETTER AND SETTER

        public SNode GetSNode() { return representedNode; }
        public void SetSNode(SNode node) { representedNode = node; }

        public bool IsSelectedForSpawning() { return selectedForSpawning; }
        public void SetSelectedForSpawning(bool selected) { selectedForSpawning = selected; }

    }
}
