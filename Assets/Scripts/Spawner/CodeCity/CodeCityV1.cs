using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;

namespace VRVis.Spawner {

    public class CodeCityV1 : ASpawner {

        public Transform parent;
        public GameObject cubePrefab;
        public GameObject spherePrefab;
        public bool PRINT_DEBUG = true;
        public bool addSpaceAndMargin = true;
        public Vector2 citySize = new Vector2(10, 10);
        public Vector2 space = new Vector2(10, 10);

        // ToDo: fix, is weird if numbers are way higher!
        public Vector2 spaceGround = new Vector2(10, 10);
        public float groundHeight = 0.01f;


        /// <summary>
        /// A single node of a ptree (2D kd-tree).<para/>
        /// 
        /// ToDo:<para/>
        /// - [ ] width/length/height values<para/>
        /// - [ ] coloring<para/>
        /// - [ ] spawning size of the city fixed/relative
        /// </summary>
        public class PNode {

            public PNode left = null;
            public PNode right = null;
            public Vector2 pos = Vector2.zero; // always relative to parent
            public Vector2 size = Vector2.zero;
            //public PNode placedElement = null;
            public SNode corNode = null; // corresponding node
            public float height = 1;
            public bool isGround = false;
            public bool isLeaf = false;

            // for spacing between elements (districts)
            public Vector2 spacing = new Vector2(10, 10);

            // ToDo: use margin!
            // margin between elements
            public Vector2 margin = new Vector2(0, 0);

            public PNode() {}

            public PNode(Vector2 pos, Vector2 size, SNode corNode) {
                this.pos = pos;
                this.size = size;
                this.corNode = corNode;
            }

            public Vector2 GetSize(bool addSpaceAndMargin = false) {
                if (addSpaceAndMargin) { return size + spacing * 2 + margin * 2; }
                return size;
            }

            public Vector2 GetMin() { return pos; }
            public Vector2 GetMax(bool addSpaceAndMargin = false) {
                if (addSpaceAndMargin) { return pos + size + spacing * 2 + margin * 2; }
                return pos + size;
            }

            public float GetWidth(bool addSpaceAndMargin = false) {
                if (addSpaceAndMargin) { return size.x + spacing.x * 2 + margin.x * 2; }
                return size.x;
            }
            public float GetLength(bool addSpaceAndMargin = false) {
                if (addSpaceAndMargin) { return size.y + spacing.y * 2 + margin.y * 2; }
                return size.y;
            }
        }

        private PNode pTreeRoot = null;

        // information about the currently spawned city
        private float i_max_height = 0;
        private Vector2 i_max_size = Vector2.zero;


        /// <summary>Prepares and spawns the visualization.</summary>
        public override bool SpawnVisualization() {

            StructureLoader sl = ApplicationLoader.GetInstance().GetStructureLoader();

            if (!sl.LoadedSuccessful()) {
                Debug.LogWarning("Failed to spawn code city! Loading was not successful.");
                return false;
            }

            // set root node and spawn the structure
            bool success = SpawnCodeCity(sl.GetRootNode());

            if (!success) { Debug.LogWarning("Failed to spawn code city v1."); }
            else { Debug.Log("Code City V1 successfully spawned."); }

            return success;
        }

        /// <summary>Spawns the code city visualization.</summary>
        public bool SpawnCodeCity(SNode rootNode) {

            // create the layout with relative positions of nodes
            pTreeRoot = RecursiveLayout(rootNode);
            i_max_size = pTreeRoot.GetSize(addSpaceAndMargin);

            Debug.Log("Code City Partition Tree Created. (" +
                "max_size: " + i_max_size + ", max_height: " + i_max_height + ")");

            // spawn the layout
            SpawnCity(pTreeRoot);

            return true;
        }


        /// <summary>
        /// Default version of the layout/packing of elements of a single hierarchy layer.
        /// </summary>
        /*
        public void Layout1(SNode node, List<SNode> elements) {

            // rot size is sum of all sub-elements sizes
            // also checks if NInf already exists
            Vector2 sizeSum = Vector2.zero;
            foreach (SNode n in elements) {
                if (!ninfos.ContainsKey(n)) { ninfos.Add(n, new NInf()); }
                NInf ni = ninfos[n];
                sizeSum += ni.size;
            }

            // order elements by one dimension (e.g. width)
            elements.Sort(SortDescending);

            // place first element at (0, 0) and set bounds
            NInf cur = ninfos[node];
            NInf ni1 = ninfos[elements[0]];
            
            ni1.pos = Vector2.zero;
            Vector2 bounds = ni1.size;

            // check where we can place the next element
            // so that bounds expend with aspect ratio closer to a square

        }
        */

         
        // ---------------------------------------------------------------------------------
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
        public PNode RecursiveLayout(SNode node) {

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
                    spacing = space
                };

                // ToDo: set height of leave nodes accordingly!
                if (PRINT_DEBUG) { Debug.Log("==> Leaf node: " + node.GetName()); }
                return leaf;
            }
            
            // continue with sub-nodes
            List<PNode> elements = new List<PNode>();
            Vector2 sizeSum = Vector2.zero;
            foreach (SNode n in node.GetNodes()) {
                PNode pnode = RecursiveLayout(n);
                elements.Add(pnode);
                sizeSum += pnode.GetSize(addSpaceAndMargin);
            }

            // introduce "root" and set its size
            PNode root = new PNode(Vector2.zero, sizeSum, node) { isGround = true, spacing = spaceGround };
            if (PRINT_DEBUG) { Debug.Log("Introducing root for: " + node.GetName() + " (leafs: " + elements.Count + ", size: " + sizeSum + ")"); }

            // order elements by one dimension (e.g. width)
            elements.Sort(SortDESC);

            // place elements in kd-tree accordingly
            List<PNode> emptyNodes = new List<PNode>{root};
            Vector2 bounds = Vector2.zero;

            foreach (PNode el in elements) {

                /*
                // if placed in here, bounds would be preserved but "float" is wasted space
                Dictionary<PNode, float> preservers = new Dictionary<PNode, float>();

                // if placed in here, bounds would be expanded and deviation from 1:1 ratio is "float"
                Dictionary<PNode, float> expanders = new Dictionary<PNode, float>();
                */

                // possible preserver for current bounds and remaining space
                PNode preserver = null;
                float remaining = 0;

                // possible expander for current bounds and according "dist" to a new bounds ratio of 1:1
                PNode expander = null;
                float distance = 0;
                Vector2 expandedBounds = bounds;

                Vector2 elSize = el.GetSize(addSpaceAndMargin);

                foreach (PNode pn in emptyNodes) {
                    
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

                Vector2 targetSize = target.GetSize();


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
                        target.pos + new Vector2(0, el.GetLength(addSpaceAndMargin)),
                        new Vector2(target.GetWidth(), sdiff.y),
                        null
                    ){ spacing = space };

                    emptyNodes.Add(target.right);
                }

                // split vertically ( | )
                if (sdiff.x > 0) {

                    PNode thisTarget = target;
                    
                    // if we did already split horizontally,
                    // add a new node that holds the element and introduces a free node
                    if (sdiff.y > 0) {
                        thisTarget = new PNode(target.pos, new Vector2(target.GetWidth(), el.GetLength(addSpaceAndMargin)), null){ spacing = space };
                        target.left = thisTarget;
                    }

                    // left in tree is element and right is a free node
                    thisTarget.left = el;
                    thisTarget.right = new PNode(
                        thisTarget.pos + new Vector2(el.GetWidth(addSpaceAndMargin), 0),
                        new Vector2(sdiff.x, thisTarget.GetLength()),
                        null
                    ){ spacing = space };

                    emptyNodes.Add(thisTarget.right);
                }

                emptyNodes.Remove(target);
            }

            // apply max bounds
            root.size = bounds;
            return root;
        }


        // ---------------------------------------------------------------------------------
        // Getting a leaf node's size.

        /// <summary>
        /// Gets and returns the size of the node.
        /// </summary>
        public Vector2 GetNodeSize(SNode node) {

            int v = 1;

            switch (v) {
                
                // version 1 using lines of code
                case 1:
                    long locs = GetLOC(node.GetFullPath());
                    return new Vector2(locs, locs);

                // version 2 using amount of regions
                case 2:
                    int regs = GetNumOfRegions(node);
                    return new Vector2(regs, regs);
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Get the height for a single node.<para/>
        /// Can return zero in special cases.
        /// </summary>
        public float GetNodeHeight(SNode node) {

            int v = 2;

            switch (v) {

                // version 1 using lines of code
                case 1: return GetLOC(node.GetFullPath());
                
                // version 2 using number of regions
                case 2: return GetNumOfRegions(node);
            }
            
            return 0;
        }

        /// <summary>
        /// Returns the lines of code or 0 if the file does not exist.
        /// </summary>
        private long GetLOC(string file) {

            if (!System.IO.File.Exists(file)) { return 0; }

            // optimized using: https://nima-ara-blog.azurewebsites.net/counting-lines-of-a-text-file/
            char LF = '\n'; // line feed
            char CR = '\r'; // carriage return

            FileStream stream = null;
            try { stream = new FileStream(file, FileMode.Open); }
            catch (Exception ex) {
                Debug.LogError("Failed to open file: " + file);
                Debug.LogError(ex.StackTrace);
                return 0;
            }

            byte[] buff = new byte[512*512];
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

        /// <summary>
        /// Get number of regions for this node.
        /// </summary>
        private int GetNumOfRegions(SNode node) {

            RegionLoader rl = ApplicationLoader.GetInstance().GetRegionLoader();
            if (!rl.LoadedSuccessful()) { return 0; }

            return rl.GetFileRegions(node.GetPath()).Count;
        }
        

        // ---------------------------------------------------------------------------------
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

            //SpawnTestTree(root, parent, parent.position, 0);
            //SpawnCityRecursivelyV1(root, null, parent.position, parent);
            SpawnCityRecursivelyV2(root, null, parent.position, parent);
            return true;
        }


        // ToDo: DEBUG - remove if no longer required!
        private void SpawnTestTree(PNode node, Transform parent, Vector3 lastPos, int depth) {

            lastPos += Vector3.down;

            if (node.corNode != null) {

                GameObject go = Instantiate(cubePrefab, lastPos, Quaternion.identity);
                go.transform.SetParent(parent, false);
                go.transform.localScale = go.transform.localScale / depth;
                go.name = node.corNode.GetName();
                if (node.isLeaf) { go.name = go.name + " Leaf"; }
                if (node.isGround) { go.name = go.name + " Ground"; }
            }
            else {

                GameObject go = Instantiate(spherePrefab, lastPos, Quaternion.identity);
                go.transform.localScale = go.transform.localScale / depth;
                go.transform.SetParent(parent, false);
                if (node.isLeaf) { go.name = go.name + " Leaf"; }
                if (node.isGround) { go.name = go.name + " Ground"; }
            }

            float fac = (200 - depth) / 200f * 10f;
            if (node.left != null) { SpawnTestTree(node.left, parent, lastPos + Vector3.left * fac, depth + 1); }
            if (node.right != null) { SpawnTestTree(node.right, parent, lastPos + Vector3.right * fac, depth + 1); }
        }


        /// <summary>
        /// Spawns city sections recursively.
        /// </summary>
        private void SpawnCityRecursivelyV1(PNode node, PNode parent, Vector3 parentPosWorld, Transform trans) {

            if (node.isGround || node.isLeaf) {

                float div = 1000f;  
                float height = 0.01f;
                parentPosWorld += new Vector3(node.pos.x / div * 0.5f, height, node.pos.y / div * 0.5f); // * 0.5 important!
                
                if (node.isLeaf) { height = node.height / 1000f; }

                // ToDo: improve
                Vector3 size = new Vector3(
                    node.GetWidth() / div / 2,
                    height,
                    node.GetLength() / div / 2
                );

                // cubePos corrects position of block
                Vector3 cubePos = parentPosWorld + new Vector3(size.x, height, size.z) * 0.5f;
                GameObject cube = Instantiate(cubePrefab, cubePos, Quaternion.identity);
                cube.name = node.corNode.GetName();
                cube.transform.localScale = size;
                cube.transform.SetParent(trans, true);

                // ToDo: improve
                if (node.isLeaf) {

                    if (cube.GetComponent<Renderer>()) {
                        cube.GetComponent<Renderer>().material.color = Color.red;
                    }
                }

                trans = cube.transform;
            }
            
            if (node.left != null) { SpawnCityRecursivelyV1(node.left, node, parentPosWorld, trans); }
            if (node.right != null) { SpawnCityRecursivelyV1(node.right, node, parentPosWorld, trans); }
        }

        /// <summary>
        /// Spawns city sections recursively.
        /// </summary>
        /// <param name="scaleSub">local scale subtraction value to get "pyramids"</param>
        private void SpawnCityRecursivelyV2(PNode node, PNode parent, Vector3 parentPosWorld, Transform trans) {

            if (node.isGround || node.isLeaf) {
                
                Vector2 city_height_range = new Vector2(0.01f, 1);
                Vector2 divBy = i_max_size;
                Vector2 multBy = new Vector2(citySize.x, citySize.y) * 0.5f; // * 0.5 important!

                // default ground height
                float height = groundHeight;

                // positioning and spacing correction
                float pos_x = node.pos.x + (addSpaceAndMargin ? node.spacing.x : 0);
                float pos_y = node.pos.y + (addSpaceAndMargin ? node.spacing.y : 0);

                // debug: to test if spacing works
                //float pos_x = node.pos.x;
                //float pos_y = node.pos.y;
                parentPosWorld += new Vector3(pos_x / divBy.x * multBy.x, height, pos_y / divBy.y * multBy.y);
                
                // leaf height calculation
                if (node.isLeaf) {

                    // get percentage of height in the range
                    float hperc = node.height / (i_max_height == 0 ? 1 : i_max_height);
                    height = hperc * (city_height_range.y - city_height_range.x) + city_height_range.x;
                }

                // scaling and margin apply
                float size_x = node.GetWidth();// - (addSpaceAndMargin ? node.spacing.x : 0);
                float size_y = node.GetLength();// - (addSpaceAndMargin ? node.spacing.y : 0);
                
                // debug: to test if spacing works
                //float size_x = node.GetWidth(addSpaceAndMargin);
                //float size_y = node.GetLength(addSpaceAndMargin);
                Vector3 size = new Vector3(size_x / divBy.x * multBy.x, height, size_y / divBy.y * multBy.y);

                // position correction according to unity scales
                Vector3 cubePos = parentPosWorld + new Vector3(size.x, height, size.z) * 0.5f; // * 0.5 important!

                GameObject cube = Instantiate(cubePrefab, cubePos, Quaternion.identity);
                cube.name = node.corNode.GetName();
                cube.transform.localScale = size;
                cube.transform.SetParent(trans, true);


                // ToDo: improve
                if (node.isLeaf) {
                    if (cube.GetComponent<Renderer>()) {
                        cube.GetComponent<Renderer>().material.color = Color.red;
                    }
                }

                trans = cube.transform;
            }
            
            if (node.left != null) { SpawnCityRecursivelyV2(node.left, node, parentPosWorld, trans); }
            if (node.right != null) { SpawnCityRecursivelyV2(node.right, node, parentPosWorld, trans); }
        }

    }
}
