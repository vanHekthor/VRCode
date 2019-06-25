﻿using System.Collections;
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
            public bool isLeave = false;

            public PNode() {}

            public PNode(Vector2 pos, Vector2 size, SNode corNode) {
                this.pos = pos;
                this.size = size;
                this.corNode = corNode;
            }

            public Vector2 GetMin() { return pos; }
            public Vector2 GetMax() { return pos + size; }

            public float GetWidth() { return size.x; }
            public float GetLength() { return size.y; }
        }

        private PNode pTreeRoot = null;


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

                Vector2 size = GetNodeSize(node);

                PNode leaf = new PNode(Vector2.zero, size, node) {
                    isLeave = true,
                    height = 5
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
                sizeSum += pnode.size;
            }

            // introduce "root" and set its size
            PNode root = new PNode(Vector2.zero, sizeSum, node) { isGround = true };
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

                foreach (PNode pn in emptyNodes) {
                    
                    // check if there is even enough space to place [el] in (pn)
                    // if not, then this (pn) is no candidate and can be skipped
                    Vector2 sizeDiff = pn.size - el.size;
                    if (sizeDiff.x < 0 || sizeDiff.y < 0) { continue; }

                    // check if placing [el] in (pn) would preserve bounds
                    Vector2 el_max = pn.pos + el.size;
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
                if (target.size == el.size) {
                    target.left = el;
                    emptyNodes.Remove(target);
                    continue;
                }


                // split the target accordingly if the element is smaller
                Vector2 sdiff = target.size - el.size;
                
                // split horizontally ( -- )
                if (sdiff.y > 0) {

                    // upper half is the element itself, lower is new
                    target.left = el;
                    target.right = new PNode(
                        target.pos + new Vector2(0, el.GetLength()),
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
                        thisTarget = new PNode(target.pos, new Vector2(target.GetWidth(), el.GetLength()), null);
                        target.left = thisTarget;
                    }

                    // left in tree is element and right is a free node
                    thisTarget.left = el;
                    thisTarget.right = new PNode(
                        thisTarget.pos + new Vector2(el.GetWidth(), 0),
                        new Vector2(sdiff.x, thisTarget.GetLength()),
                        null
                    );

                    emptyNodes.Add(thisTarget.right);
                }

                emptyNodes.Remove(target);
            }

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
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Returns the lines of code or 0 if the file does not exist.
        /// </summary>
        public long GetLOC(string file) {

            if (!System.IO.File.Exists(file)) { return 0; }

            // optimized using: https://nima-ara-blog.azurewebsites.net/counting-lines-of-a-text-file/
            char LF = '\n'; // line feed
            char CR = '\r'; // carriage return

            FileStream stream = new FileStream(file, FileMode.Open);
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
            SpawnCityRecursively(root, null, parent.position, parent);
            return true;
        }


        private void SpawnTestTree(PNode node, Transform parent, Vector3 lastPos, int depth) {

            lastPos += Vector3.down;

            if (node.corNode != null) {

                GameObject go = Instantiate(cubePrefab, lastPos, Quaternion.identity);
                go.transform.SetParent(parent, false);
                go.transform.localScale = go.transform.localScale / depth;
                go.name = node.corNode.GetName();
                if (node.isLeave) { go.name = go.name + " Leaf"; }
                if (node.isGround) { go.name = go.name + " Ground"; }
            }
            else {

                GameObject go = Instantiate(spherePrefab, lastPos, Quaternion.identity);
                go.transform.localScale = go.transform.localScale / depth;
                go.transform.SetParent(parent, false);
                if (node.isLeave) { go.name = go.name + " Leaf"; }
                if (node.isGround) { go.name = go.name + " Ground"; }
            }

            float fac = (200 - depth) / 200f * 10f;
            if (node.left != null) { SpawnTestTree(node.left, parent, lastPos + Vector3.left * fac, depth + 1); }
            if (node.right != null) { SpawnTestTree(node.right, parent, lastPos + Vector3.right * fac, depth + 1); }
        }


        /// <summary>
        /// Spawns city sections recursively.
        /// </summary>
        private void SpawnCityRecursively(PNode node, PNode parent, Vector3 parentPosWorld, Transform trans) {

            if (node.isGround || node.isLeave) {

                float div = 10000f;
                
                float height = 0.1f;
                if (node.isLeave) { height = node.height / 10f; }
                parentPosWorld += new Vector3(node.pos.x / div, height, node.pos.y / div);
                Debug.Log("PWP: " + parentPosWorld);

                GameObject cube = Instantiate(cubePrefab, parentPosWorld, Quaternion.identity);
                cube.name = node.corNode.GetName();

                // ToDo: improve
                Vector3 size = new Vector3(
                    node.GetWidth() / div,
                    height,
                    node.GetLength() / div
                );

                cube.transform.localScale = size;
                cube.transform.SetParent(trans, true);

                // ToDo: improve
                if (node.isLeave) {

                    if (cube.GetComponent<Renderer>()) {
                        cube.GetComponent<Renderer>().material.color = Color.red;
                    }

                    return;
                }

                trans = cube.transform;
            }
            
            if (node.left != null) { SpawnCityRecursively(node.left, node, parentPosWorld, trans); }
            if (node.right != null) { SpawnCityRecursively(node.right, node, parentPosWorld, trans); }
        }

    }
}
