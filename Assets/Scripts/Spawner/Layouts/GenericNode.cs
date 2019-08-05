using System.Collections;
using System.Collections.Generic;

namespace VRVis.Spawner.Layouts {

    /// <summary>
    /// Describes an interface that a node used by the ConeTreeLayout must fulfil.
    /// </summary>
    public abstract class GenericNode {

        // in case this is a leaf node, define the radius
        private float leaf_radius = 0;
        private bool leaf_radius_set = false;

	    public abstract bool IsLeaf();
        public abstract IEnumerable GetNodes();
        public abstract int GetNodesCount();

        public float GetLeafRadius() { return leaf_radius; }
        public void SetLeafRadius(float leaf_radius) {
            this.leaf_radius = leaf_radius;
            leaf_radius_set = true;
        }

        public bool IsLeafRadiusSet() { return leaf_radius_set; }

    }
}
