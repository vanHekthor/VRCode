using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Utilities;

namespace VRVis.Spawner.Layouts.ConeTree {

    /// <summary>
    /// Class that helps to create a cone tree layout uniformly.<para/>
    /// Created: 05.08.2019 (by Leon H.)<para/>
    /// Updated: 05.08.2019
    /// </summary>
    public class ConeTreeLayout {

        private LayoutSettings settings;
        private readonly GenericNode rootNode;
        private int treeLevels = 0;

        public delegate float GetLeafRadius(GenericNode node);
        private GetLeafRadius leafRadiusOverride = null;

        private readonly float fullCircle = 2 * Mathf.PI;
        private float nodeRotationRad = 0;


        // CONSTRUCTOR

	    public ConeTreeLayout(GenericNode rootNode, LayoutSettings settings) {
            this.rootNode = rootNode;
            this.settings = settings;
        }


        // GETTER / SETTER

        /// <summary>Get the generic node assigned as layout entry.</summary>
        public GenericNode GetRootNode() { return rootNode; }
        public int GetTreeLevels() { return treeLevels; }
        public LayoutSettings GetSettings() { return settings; }
        public void SetConeTreeLayoutSettings(LayoutSettings settings) { this.settings = settings; }

        /// <summary>
        /// Assign a delegate method to override retrieval of a leaf's radius.<para/>
        /// Returning a negative number will be treated as invalid and not applied.
        /// </summary>
        public void SetLeafRadiusOverride(GetLeafRadius overrideMethod) {
            leafRadiusOverride = overrideMethod;
        }


        // FUNCTIONALITY

        /// <summary>
        /// Calculates the layout for the tree specified by the root.<para/>
        /// Returns the same tree of information using the PosInfo class.
        /// </summary>
        public PosInfo Create() {
            Debug.Log("Creating generic cone tree layout...");
            nodeRotationRad = Utility.DegreeToRadians(settings.nodeRotation);
            return CalculateLevelPositioningRecursively(rootNode, 0);
        }

        /// <summary>
        /// Recursively calculate the position of nodes for each level so that they do not intersect.<para/>
        /// This requires traversing the whole hierarchy once!<para/>
        /// Returns the according PosInfo of the recently processed level/node.
        /// </summary>
        private PosInfo CalculateLevelPositioningRecursively(GenericNode node, int level) {

            if (level > treeLevels) { treeLevels = level; }

            // initial information about a node
            PosInfo info = new PosInfo {
                node = node,
                level = level,
                radius = settings.minRadius
            };


            // assign radius depending on type of leaf node and return if leaf
            bool isLeaf = node.IsLeaf();
            if (isLeaf || node.GetNodesCount() == 1) {

                // use delegate function to calculate leaf radius
                if (leafRadiusOverride != null) {
                    float r = leafRadiusOverride(node);
                    if (r > 0 && r > settings.minRadius) { info.radius = r; }
                }

                if (isLeaf) { return info; }
            }


            // get info of child nodes recursively
            float radius_sum = 0;
            foreach (GenericNode child in node.GetNodes()) {
                PosInfo childInfo = CalculateLevelPositioningRecursively(child, level + 1);
                info.childNodes.Add(childInfo);
                radius_sum += childInfo.radius + settings.nodeSpacing * 0.5f;
            }


            // ---------------------------------------------------
            // calculate position for each child node

            // the final calculated radius of this node
            float nodeRadius = info.radius;

            if (node.GetNodesCount() == 1) {
                float childRad = info.childNodes[0].radius;
                if (childRad > nodeRadius) { nodeRadius = childRad; }
            }
            else {

                // use angular sector calculation for multiple nodes
                float angleTotal = 0;
                float size_n = settings.minRadius; // size of center "node" n

                // calculate positioning on angular sector around n
                foreach (PosInfo childInfo in info.childNodes) {
                    float r = childInfo.radius + settings.nodeSpacing * 0.5f;
                    float angle = r / radius_sum * fullCircle;
                    float d_i = Mathf.Max(size_n + r, r / Mathf.Sin(angle * 0.5f));
                    float pos_x = d_i * Mathf.Cos(angleTotal + angle * 0.5f + nodeRotationRad);
                    float pos_y = d_i * Mathf.Sin(angleTotal + angle * 0.5f + nodeRotationRad);
                    childInfo.relPos = new Vector2(pos_x, pos_y);
                    angleTotal += angle;
                }
                
                // find enclosing disk / circle using Welzl
                List<Disk> P = CreateShuffledList(info.childNodes);
                List<Disk> R = new List<Disk>(3);
                Disk SED = Welzl(P, R, P.Count);

                // readjust the centroid / position of parent node accordingly by
                // moving the relative positions from (0,0) to the center of the disk
                foreach (PosInfo childInfo in info.childNodes) { childInfo.relPos -= SED.pos; }

                // readjust the final radius accordingly
                nodeRadius = SED.radius;
            }

            // apply the calculated radius based on previous levels
            if (settings.useRadiusOfPreviousLevel) { info.radius = nodeRadius; }

            // limit radius to maximum value
            if (settings.maxRadius > 0 && nodeRadius > settings.maxRadius) {
                Debug.LogError("Maximum graph radius reached (" + settings.maxRadius + ")!");
                nodeRadius = settings.maxRadius;
            }

            //Debug.LogWarning("Radius = " + nodeRadius);
            return info;
        }


        // -------------------------------------------------------------------------------------------
        // Smallest Enclosing Disk (SED) Calculation

        /// <summary>Class that represents a disk.</summary>
        class Disk {

            public Vector2 pos;
            public float radius; // for case that |R| == 1

            public Disk(Vector2 p, float r) { pos = p; radius = r; }

            /// <summary>
            /// Checks if the position of the disk d is inside this disk.
            /// </summary>
            public bool Contains(Disk d) {
                return (d.pos - pos).magnitude <= radius;
            }
        }

        /// <summary>
        /// Creates a list of randomly shuffled elements based on Durstenfeld's algorithm.<para/>
        /// The shuffled list is created in O(n) time and returned as a list of disks.<para/>
        /// See: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_modern_algorithm
        /// </summary>
        private List<Disk> CreateShuffledList(List<PosInfo> nodes) {

            List<Disk> result = new List<Disk>(nodes.Count);
            nodes.ForEach(n => result.Add(new Disk(n.relPos, n.radius)));

            // Note: we start at i = |nodes| bc. the upper bound in Random.Range is exclusive
            for (int i = nodes.Count; i > 0; i--) {
                int n = Random.Range(0, i);
                Disk tmp = result[i-1];
                result[i-1] = result[n];
                result[n] = tmp;
            }

            return result;
        }

        /// <summary>Use Welzl's algorithm for smallest enclosing disk calculation.</summary>
        /// <param name="P">Set of points</param>
        /// <param name="R">Set of points on boundary</param>
        /// <param name="max">Size of P to avoid modifying the list</param>
        private Disk Welzl(List<Disk> P, List<Disk> R, int max) {

            if (max == 0 || R.Count == 3) { return Trivial(R); }
            Disk p = P[max-1]; // list is already shuffled, so just select node
            Disk D = Welzl(P, R, max-1); // call without p in P
            if (D != null && D.Contains(p)) { return D; }
            List<Disk> R_ = new List<Disk>(R){p}; // add p to R
            return Welzl(P, R_, max-1); // call without p in P but p in R
        }

        /// <summary>
        /// Calculates the Disk for |R| less than 3 and returns it.<para/>
        /// Otherwise returns null!
        /// </summary>
        private Disk Trivial(List<Disk> R) {

            if (R.Count == 1) { return R[0]; }

            // create circle from 2 points
            if (R.Count == 2) {
                Vector2 p1 = R[0].pos + R[0].pos.normalized * R[0].radius;
                Vector2 p2 = R[1].pos + R[1].pos.normalized * R[1].radius;
                Vector2 mid = (p1 + p2) * 0.5f;
                float radius = Vector2.Distance(mid, p1);
                return new Disk(mid, radius);
            }

            // calculate circumcircle of triangle
            if (R.Count == 3) { return CalculateTriangleCircumcircle(R); }

            return null;
        }


        /// <summary>
        /// Calculates a disk / circle from the given 3 points.<para/>
        /// This circle is the circumcircle of the triangle they form together.
        /// </summary>
        private Disk CalculateTriangleCircumcircle(List<Disk> R) {

            // Possible other approach:
            // http://www.meracalculator.com/graphic/perpendicularbisector.php
            // 1. calculate midpoint of line like:                ML = (p1.x + p2.x, p1.y + p2.y) / 2
            // 2. calculate slope of line like:                   SL = (p2.y - p1.y) / (p2.x - p2.y)
            // 3. calculate slope of perpendicular bisector:      SB = -1 / slope_line
            // 4. get p. bisect. equation using midpoint & slope: y - ML.y = SB * (x - ML.x)  =>  y = SB * (x - ML.x) + ML.y
            //
            // Find intersection of two perpendicular bisector lines y = L1 = m1 * x1 + n1 &  y = L2 = m2 * x2 + n2:
            // 1. Same y-coordinate at point of interaction, so:     L1 = L2  =>  m1 * x1 + n1 = m2 * x2 + n2
            // 2. m and n are known, so only x is unknown -> solve:  
            //    (see steps: https://www.xarg.org/2016/10/calculate-the-intersection-point-of-two-lines/)

            // ---------------------------
            // Current approach

            // 90 degree rotation matrix [cos(a), -sin(a), sin(a), cos(a)]
            float[] rotMat = new float[4]{0, -1, 1, 0};

            // find midpoints of two points and calculate perpendicular two bisectors
            Vector2 p1 = R[0].pos + R[0].pos.normalized * R[0].radius;
            Vector2 p2 = R[1].pos + R[1].pos.normalized * R[1].radius;
            Vector2 p3 = R[2].pos + R[2].pos.normalized * R[2].radius;

            Vector2 pb_pos_12 = (p1 + p2) * 0.5f; // midpoint between p1 and p2
            Vector2 pb_dir_12 = CalculateBisectorDirection(ref rotMat, pb_pos_12, p2);
            
            Vector2 pb_pos_13 = (p1 + p3) * 0.5f; // midpoint between p1 and p3
            Vector2 pb_dir_13 = CalculateBisectorDirection(ref rotMat, pb_pos_13, p3);
            
            // "Koordinatenform aus Parameterform" (parameter form -> general form)
            // https://de.wikipedia.org/wiki/Koordinatenform#Aus_der_Parameterform
            // genf_xx = [a, b, c] , so that g = ax + by + c = 0
            // NOTE: In contrast to wikipedia (bug), calculate c = pos * (-norm) instead of c = pos * norm!
            Vector2 norm_12 = new Vector2(-pb_dir_12.y, pb_dir_12.x); // normal vector of direction vector
            Vector3 genf_12 = new Vector3(norm_12.x, norm_12.y, Vector2.Dot(pb_pos_12, -norm_12));

            Vector2 norm_13 = new Vector2(-pb_dir_13.y, pb_dir_13.x); // normal vector of direction vector
            Vector3 genf_13 = new Vector3(norm_13.x, norm_13.y, Vector2.Dot(pb_pos_13, -norm_13));

            // apply cross-product to find intersection point of both lines
            Vector3 cross = Vector3.Cross(genf_12, genf_13);

            // circumcenter
            Vector2 cc = new Vector2(cross.x / cross.z, cross.y / cross.z);

            // get radius (cc distance to any other point)
            float radius = Vector2.Distance(cc, p1);

            return new Disk(cc, radius);
        }

        /// <summary>
        /// Calculates and returns the perpendicular bisector direction.<para/>
        /// Uses a rotation-matrix to turn vector mp around point m.
        /// </summary>
        /// <param name="rotMat">2x2 rotation-matrix with total of 4 values (first 2 = first row)</param>
        /// <param name="m">Midpoint between p1 and p2</param>
        /// <param name="p">One of the points (either p1 or p2)</param>
        private Vector2 CalculateBisectorDirection(ref float[] rotMat, Vector2 m, Vector2 p) {

            // rotate vector mp around point m
            Vector2 mp = p - m;
            return new Vector2(
                rotMat[0] * mp.x + rotMat[1] * mp.y,
                rotMat[2] * mp.x + rotMat[3] * mp.y
            );
        }

    }
}
