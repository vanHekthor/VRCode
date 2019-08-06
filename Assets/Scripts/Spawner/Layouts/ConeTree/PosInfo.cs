using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Spawner.Layouts.ConeTree {

    /// <summary>Used while position information gathering.</summary>
    public class PosInfo {
        public GenericNode node;
        public int level = 0;
        public float radius = 0;
        public Vector2 relPos = Vector2.zero;
        public List<PosInfo> childNodes = new List<PosInfo>();
    }

}
