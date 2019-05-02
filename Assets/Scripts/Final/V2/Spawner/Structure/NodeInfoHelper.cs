using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Spawner.Structure {

    /// <summary>
    /// Helper object used by the structure spawner v1.<para/>
    /// It tells about width, height and position of the current game object.<para/>
    /// This is used in recursion to position the objects correctly.
    /// </summary>
    public class NodeInfoHelper {

        // start position regaring all nodes on this level
        public GameObject gameObject = null;
        public Vector3 pos = Vector3.zero;
        public float width = 0;
        public float height = 0;

        public NodeInfoHelper() {}

        public NodeInfoHelper(NodeInfoHelper info) {
            gameObject = info.gameObject;
            pos = info.pos;
            width = info.width;
            height = info.height;
        }

    }
}
