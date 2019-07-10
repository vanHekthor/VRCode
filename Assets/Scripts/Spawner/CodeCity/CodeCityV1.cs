using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner.CodeCity;

namespace VRVis.Spawner {

    /// <summary>
    /// Code City Visualization.<para/>
    /// 
    /// Created: 06/25/2019
    /// Updated: 06/28/2019
    /// 
    /// Created by Leon Hutans, according to
    /// "Software Systems as Cities" by Richard Wettel.
    /// </summary>
    public class CodeCityV1 : ASpawner {

        [Tooltip("Parent element to attach objects to")]
        public Transform parent;

        [Tooltip("Basic cube prefab for the city buildings")]
        public GameObject cubePrefab;

        [Tooltip("Total size of the city (width/height)")]
        public Vector2 citySize = new Vector2(10, 10);

        [Tooltip("Enable/Disable space and margin")]
        public bool addSpaceAndMargin = true;

        [Tooltip("Space between elements")]
        public Vector2 spacing = new Vector2(10, 10);

        [Tooltip("Space between elements and their parent border")]
        public Vector2 margin = new Vector2(20, 20);

        [Tooltip("Height of packages")]
        public float groundHeight = 0.01f;

        [Tooltip("Range of height of the city buildings")]
        public Vector2 cityHeightRange = new Vector2(0.01f, 1);

        [Tooltip("Packages differ in color according to their depth")]
        public Color packageColorFrom = new Color(0.3f, 0.3f, 0.3f);
        public Color packageColorTo = new Color(0.9f, 0.9f, 0.9f);

        public ClassMapping[] classMappings;

        [Header("Debug")]
        public bool PRINT_DEBUG = true;


        // ToDo: for testing purposes - put in value mappings format later
        [Serializable]
        public class ClassMapping {
            public string name;
            public bool apply = true;
            public string[] fileEndings;
            public Color color = Color.white;
        }


        /// <summary>
        /// Class representing a node of the partitioning tree.<para/>
        /// (With respect to the publication: ptree (2D kd-tree)).
        /// </summary>
        public class PNode {

            /// <summary>Position relative to parent node</summary>
            public Vector2 pos = Vector2.zero;

            /// <summary>Corresponding structure node</summary>
            public SNode corNode = null;

            /// <summary>Values for gap calculation</summary>
            public Vector2 spacing = Vector2.zero;

            public PNode left = null;
            public PNode right = null;
            public bool isPackage = false;
            public bool isLeaf = false;
            public float height = 1;

            private Vector2 size = Vector2.zero;

            // additional information (not required for building ptree)
            public uint depthPos = 0;
            public int subElements = 0;
            public float heightPercentage = 0;
            public float cityHeight = 0;


            public PNode() {}

            public PNode(Vector2 pos, Vector2 size, SNode corNode) {
                this.pos = pos;
                this.size = size;
                this.corNode = corNode;
            }


            // GETTER/SETTER METHODS
            public Vector2 GetSize() { return size; }
            public void SetSize(Vector2 size) { this.size = size;}
            public Vector2 GetMin() { return pos; }
            public Vector2 GetMax() { return pos + size; }
            public float GetWidth() { return size.x; }
            public float GetLength() { return size.y; }
            public float GetHeight() { return height; }

        }

        private PNode pTreeRoot = null;
        private float spawn_divBy = 1;
        private Vector2 spawn_multBy = Vector2.one;
        private bool isSpawned = false;


        // information about the currently spawned city
        private float i_max_height = 0;
        private Vector2 i_max_size = Vector2.zero;
        private uint i_spawned_elements = 0;
        private uint i_spawned_packages = 0;
        private uint i_max_depth = 0;


        /// <summary>Prepares and spawns the visualization.</summary>
        public override bool SpawnVisualization() {

            if (isSpawned) {
                Debug.LogWarning("Code city is already spawned!");
                return false;
            }

            StructureLoader sl = ApplicationLoader.GetInstance().GetStructureLoader();

            if (!sl.LoadedSuccessful()) {
                Debug.LogWarning("Failed to spawn code city! Loading was not successful.");
                return false;
            }

            // set root node and spawn the structure
            bool success = SpawnCodeCity(sl.GetRootNode());

            if (!success) { Debug.LogWarning("Failed to spawn code city v1."); }
            else {
                Debug.Log("Code City V1 successfully spawned.");
                isSpawned = true;
            }

            return success;
        }


        /// <summary>Show/Hide the visualization.</summary>
        public override void ShowVisualization(bool state) {

            if (!isSpawned || !parent) { return; }
            parent.gameObject.SetActive(state);
        }


        /// <summary>Spawns the code city visualization.</summary>
        public bool SpawnCodeCity(SNode rootNode) {

            // create the layout with relative positions of nodes
            float ts = Time.time;
            i_max_depth = 0;
            pTreeRoot = RecursiveLayout(rootNode, 0);
            float td = Mathf.Round((Time.realtimeSinceStartup - ts) * 1000f) / 1000f;
            float ttotal = td;
            i_max_size = pTreeRoot.GetSize();
            Debug.Log("Code City Partition Tree Created [" + td + " seconds].\n(" +
                "max_size: " + i_max_size + ", max_height: " + i_max_height + ")");

            // division and multiplication factors to keep city in scales
            spawn_divBy = i_max_size.x > i_max_size.y ? i_max_size.x : i_max_size.y;
            spawn_multBy = new Vector2(citySize.x, citySize.y) * 0.5f; // * 0.5 important bc. of Unity scales!

            // spawn the city layout
            i_spawned_elements = 0;
            i_spawned_packages = 0;
            ts = Time.realtimeSinceStartup;
            SpawnCity(pTreeRoot);
            td = Mathf.Round((Time.realtimeSinceStartup - ts) * 1000f) / 1000f;
            ttotal += td;
            Debug.Log("Code City Spawned [" + td + " seconds, total: " + ttotal + "].\n(" + 
                "elements: " + i_spawned_elements + ", packages: " + i_spawned_packages + ")");
            return true;
        }

         
        // ---------------------------------------------------------------------------------------
        // Recursive City-Layout Creation.
        
        /// <summary>Sort in descending order.</summary>
	    public int SortDESC(PNode a, PNode b) {
            if (a.GetWidth() < b.GetWidth()) { return 1; }
		    else if (a.GetWidth() > b.GetWidth()) { return -1; }
		    return 0;
	    }

        /// <summary>
        /// Run recursive layout.<para/>
        /// Returns the size of that node.
        /// </summary>
        public PNode RecursiveLayout(SNode node, uint depth) {

            if (depth > i_max_depth) { i_max_depth = depth; }

            // check if is leaf or not
            if (node.GetNodesCount() <= 0) { // this is a leaf node

                //Vector2 size = new Vector2(100, 100);
                Vector2 size = GetNodeSize(node);

                // ToDo: separate size from actual value?
                // (e.g.: what if there is no value or the value is 0?)
                if (size.x + size.y < 2) { size = new Vector2(1, 1); }

                float height = GetNodeHeight(node);
                if (height > i_max_height) { i_max_height = height; }

                PNode leaf = new PNode(Vector2.zero, size, node) {
                    isLeaf = true,
                    height = height,
                    depthPos = depth
                };

                // ToDo: set height of leave nodes accordingly!
                if (PRINT_DEBUG) { Debug.Log("==> Leaf node: " + node.GetName()); }
                return leaf;
            }
            
            // continue with sub-nodes
            List<PNode> elements = new List<PNode>();
            Vector2 sizeSum = Vector2.zero;
            foreach (SNode n in node.GetNodes()) {
                PNode pnode = RecursiveLayout(n, depth + 1);
                elements.Add(pnode);
                sizeSum += pnode.GetSize();
            }

            // add spacing and margin to size (second margin is added at the end)
            if (addSpaceAndMargin) {
                sizeSum += margin + spacing * (elements.Count - 1);
            }

            // introduce "root" and set its size
            PNode root = new PNode(Vector2.zero, sizeSum, node) {
                isPackage = true,
                subElements = elements.Count,
                depthPos = depth
            };

            if (PRINT_DEBUG) { Debug.Log("Introducing root for: " + node.GetName() + " (leafs: " + elements.Count + ", size: " + sizeSum + ")"); }

            // order elements by one dimension (e.g. width)
            elements.Sort(SortDESC);

            // place elements in kd-tree accordingly
            List<PNode> emptyNodes = new List<PNode>{root};
            Vector2 bounds = Vector2.zero;

            foreach (PNode el in elements) {

                // possible preserver for current bounds and remaining space
                PNode preserver = null;
                float remaining = 0;

                // possible expander for current bounds and according "dist" to a new bounds ratio of 1:1
                PNode expander = null;
                float distance = 0;
                Vector2 expandedBounds = bounds;

                Vector2 elSize = el.GetSize();
                Dictionary<PNode, Vector2> pnSizeAdd = new Dictionary<PNode, Vector2>();

                foreach (PNode pn in emptyNodes) {
                    
                    // change size of element according to position
                    if (addSpaceAndMargin) {

                        // would be "hit" a border placing it here?
                        bool x_hb = pn.pos.x == 0;
                        bool y_hb = pn.pos.y == 0;

                        Vector2 sizeAdd = new Vector2(
                            x_hb ? margin.x : spacing.x,
                            y_hb ? margin.y : spacing.y
                        );

                        // store new element size including the margin and spacing
                        pnSizeAdd.Add(pn, sizeAdd);
                        elSize = el.GetSize() + sizeAdd;
                    }

                    // check if there is even enough space to place [el] in (pn)
                    // if not, then this (pn) is no candidate and can be skipped
                    Vector2 sizeDiff = pn.GetSize() - elSize;
                    if (sizeDiff.x < 0 || sizeDiff.y < 0) { continue; }

                    // check if placing [el] in (pn) would preserve bounds
                    Vector2 el_max = pn.pos + elSize;
                    if (el_max.x <= bounds.x && el_max.y <= bounds.y) {
                        
                        // calculate remaining space when using this node (pn)
                        float spaceLeft = sizeDiff.x + sizeDiff.y;

                        if (spaceLeft < remaining || preserver == null) {
                            preserver = pn;
                            remaining = spaceLeft;

                            // this would be a perfect fit
                            if (remaining == 0) { break; }
                        }

                        continue;
                    }

                    // placing [el] in (pn) will expand bounds so check "dist" to ratio 1:1
                    Vector2 newBounds = new Vector2(
                        el_max.x > bounds.x ? el_max.x : bounds.x,
                        el_max.y > bounds.y ? el_max.y : bounds.y
                    );

                    // get ratio and check how far away from 1 it is
                    float ratio = newBounds.x / newBounds.y; 
                    float dist = Mathf.Abs(1 - ratio);

                    if (dist < distance || expander == null) {
                        expander = pn;
                        distance = dist;
                        expandedBounds = newBounds;
                    }
                }

                // prefer bounds preservers over expanders
                PNode target = preserver ?? expander;
                if (target == null) {
                    if (PRINT_DEBUG) { Debug.LogError("Got NULL for target!"); }
                    continue;
                }


                // get size of target and element with spacing/margin accordingly
                Vector2 targetSize = target.GetSize();
                if (addSpaceAndMargin) {

                    Vector2 sizeAdd = pnSizeAdd[target];
                    elSize = el.GetSize() + sizeAdd;

                    // store spacing (required for later correction)
                    el.spacing = sizeAdd;
                }


                if (PRINT_DEBUG) {
                    string trg = target.corNode != null ? target.corNode.GetName() : "/";
                    string eln = el.corNode != null ? el.corNode.GetName() : "/";
                    Debug.Log("-- Adding element: " + eln + " to: " + trg);
                }


                // set element position
                el.pos = target.pos;

                // update the bounds if expander is used as target
                if (target == expander) { bounds = expandedBounds; }

                // check if [el] perfectly fits (target)
                if (targetSize == elSize) {
                    target.left = el;
                    emptyNodes.Remove(target);
                    continue;
                }


                // split the target accordingly if the element is smaller
                Vector2 sdiff = targetSize - elSize;
                
                // split horizontally ( -- )
                if (sdiff.y > 0) {

                    // upper half is the element itself, lower is new
                    target.left = el;
                    target.right = new PNode(
                        target.pos + new Vector2(0, elSize.y),
                        new Vector2(target.GetWidth(), sdiff.y),
                        null
                    );

                    emptyNodes.Add(target.right);
                }

                // split vertically ( | )
                if (sdiff.x > 0) {

                    PNode thisTarget = target;
                    
                    // if we did already split horizontally,
                    // add a new node that holds the element and introduces a free node
                    if (sdiff.y > 0) {
                        thisTarget = new PNode(target.pos, new Vector2(target.GetWidth(), elSize.y), null);
                        target.left = thisTarget;
                    }

                    // left in tree is element and right is a free node
                    thisTarget.left = el;
                    thisTarget.right = new PNode(
                        thisTarget.pos + new Vector2(elSize.x, 0),
                        new Vector2(sdiff.x, thisTarget.GetLength()),
                        null
                    );

                    emptyNodes.Add(thisTarget.right);
                }

                emptyNodes.Remove(target);
            }

            // apply max bounds and add margin
            root.SetSize(bounds + (addSpaceAndMargin ? margin : Vector2.zero));
            return root;
        }


        // ---------------------------------------------------------------------------------------
        // Getting a leaf node's size.

        /// <summary>
        /// Gets and returns the size of the node.
        /// </summary>
        public Vector2 GetNodeSize(SNode node) {

            int v = 4;

            switch (v) {
                
                // version 1 using lines of code
                case 1:
                    long locs = GetLOC(node.GetFullPath());
                    return new Vector2(locs, locs);

                // version 2 using amount of regions
                case 2:
                    int regs = GetNumOfRegions(node);
                    return new Vector2(regs, regs);

                // version 3 using file size in bytes
                case 3:
                    long bytes = GetFileSizeBytes(node.GetFullPath());
                    return new Vector2(bytes, bytes);
                
                // fixed size
                case 4: return new Vector2(10, 10);
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Get the height for a single node.<para/>
        /// Can return zero in special cases.
        /// </summary>
        public float GetNodeHeight(SNode node) {

            int v = 3;

            switch (v) {

                // version 1 using lines of code
                case 1: return GetLOC(node.GetFullPath());
                
                // version 2 using number of regions
                case 2: return GetNumOfRegions(node);

                // version 3 using file size in bytes
                case 3: return GetFileSizeBytes(node.GetFullPath());
            }
            
            return 0;
        }

        /// <summary>
        /// Returns the lines of code or 0 if the file does not exist.<para/>
        /// Implemented with respect to the optimization approach of:<para/>
        /// https://nima-ara-blog.azurewebsites.net/counting-lines-of-a-text-file/
        /// </summary>
        private long GetLOC(string file) {

            if (!System.IO.File.Exists(file)) { return 0; }

            char LF = '\n'; // line feed
            char CR = '\r'; // carriage return

            FileStream stream = null;
            try { stream = new FileStream(file, FileMode.Open); }
            catch (Exception ex) {
                Debug.LogError("Failed to open file: " + file);
                Debug.LogError(ex.StackTrace);
                return 0;
            }

            byte[] buff = new byte[1024 * 1024];
            int bytesRead = 0;
            char prev = (char) 0;
            bool pending = false;
            long cnt = 0;

            while ((bytesRead = stream.Read(buff, 0, buff.Length)) > 0) {

                for (int i = 0; i < bytesRead; i++) {

                    char cur = (char) buff[i];
                    
                    // MAC: \r
                    // UNIX: \n
                    if (cur == CR || cur == LF) {

                        // WIN case: \r\n
                        // (we already detected \r and cnt+1 so skip this char)
                        if (prev == CR && cur == LF) { continue; }

                        pending = false;
                        cnt++;
                    }
                    else if (!pending) { pending = true; }

                    prev = cur;
                }
            }

            if (pending) { cnt++; }
            return cnt;
        }

        /// <summary>Get number of regions for this node.</summary>
        private int GetNumOfRegions(SNode node) {

            RegionLoader rl = ApplicationLoader.GetInstance().GetRegionLoader();
            if (!rl.LoadedSuccessful()) { return 0; }

            return rl.GetFileRegions(node.GetPath()).Count;
        }
        
        /// <summary>Get the size of a file in bytes.</summary>
        private long GetFileSizeBytes(string file) {

            if (!System.IO.File.Exists(file)) { return 0; }
            
            FileInfo fi = null;
            try { fi = new FileInfo(file); }
            catch (Exception e) {
                Debug.LogWarning("Failed to get size of file: " + file + "!");
                Debug.LogWarning(e.StackTrace);
                return 0;
            }

            return fi.Length;
        }


        // ---------------------------------------------------------------------------------------
        // Spawning the city's visual representation.

        /// <summary>
        /// Spawns the city by creating the elements accordingly.
        /// </summary>
        public bool SpawnCity(PNode root) {

            if (!parent) {
                Debug.LogError("Missing Code City Parent!");
                return false;
            }

            if (!cubePrefab) {
                Debug.LogError("Missing Code City cube prefab!");
                return false;
            }
            
            // start recursive creation of city
            SpawnCityRecursively(root, null, parent.position - new Vector3(0, groundHeight, 0), parent);
            return true;
        }

        /// <summary>
        /// Spawns city sections recursively.
        /// </summary>
        /// <param name="scaleSub">local scale subtraction value to get "pyramids"</param>
        private void SpawnCityRecursively(PNode node, PNode parent, Vector3 parentPosWorld, Transform trans) {

            if (node.isPackage || node.isLeaf) {

                // default ground height
                float height = groundHeight;
                
                // positioning and spacing correction
                float pos_x = node.pos.x + (addSpaceAndMargin ? node.spacing.x : 0);
                float pos_y = node.pos.y + (addSpaceAndMargin ? node.spacing.y : 0);
                parentPosWorld += new Vector3(
                    pos_x / spawn_divBy * spawn_multBy.x,
                    height,
                    pos_y / spawn_divBy * spawn_multBy.y
                );

                // leaf height calculation
                if (node.isLeaf) {

                    // get percentage of height in the range
                    float hperc = node.height / (i_max_height == 0 ? 1 : i_max_height);                    
                    height = hperc * (cityHeightRange.y - cityHeightRange.x) + cityHeightRange.x;

                    // additional info
                    node.heightPercentage = hperc;
                }

                // additional info
                node.cityHeight = height;

                // scaling (uses initial scales without margin/spacing)
                Vector3 size = new Vector3(
                    node.GetWidth() / spawn_divBy * spawn_multBy.x,
                    height,
                    node.GetLength() / spawn_divBy * spawn_multBy.y
                );

                // position correction according to unity scales
                Vector3 cubePos = parentPosWorld + new Vector3(size.x, height, size.z) * 0.5f; // * 0.5 important!

                // create the cube object in the scene
                GameObject cube = Instantiate(cubePrefab, cubePos, Quaternion.identity);
                cube.name = node.corNode.GetName();
                cube.transform.localScale = size;
                cube.transform.SetParent(trans, true);

                // add code city element and information
                CodeCityElement cce = cube.AddComponent<CodeCityElement>();
                cce.SetNode(node);
                
                // ToDo: improved color mapping and texturing of buildings
                // add color and more according to type of node
                if (node.isLeaf) {

                    i_spawned_elements++;

                    // ToDo: testing - improve!
                    if (cube.GetComponent<Renderer>()) {

                        string name = node.corNode.GetName();
                        Color c = Color.gray;

                        foreach (ClassMapping cm in classMappings) {

                            if (!cm.apply) { continue; }
                            bool applies = false;

                            foreach (string e in cm.fileEndings) {
                                if (name.EndsWith(e)) {
                                    applies = true;
                                    break;
                                }
                            }

                            if (applies) {
                                c = cm.color;
                                break;
                            }
                        }

                        cube.GetComponent<Renderer>().material.color = c;
                    }
                }
                else if (node.isPackage) {

                    i_spawned_packages++;

                    // assign color according to depth
                    if (cube.GetComponent<Renderer>()) {
                        float dp = node.depthPos / (float) i_max_depth;
                        Color c = (1 - dp) * packageColorFrom + dp * packageColorTo;
                        cube.GetComponent<Renderer>().material.color = c;
                    }
                }

                trans = cube.transform;
            }
            
            if (node.left != null) { SpawnCityRecursively(node.left, node, parentPosWorld, trans); }
            if (node.right != null) { SpawnCityRecursively(node.right, node, parentPosWorld, trans); }
        }

    }
}
