using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.RegionProperties;
using VRVis.Utilities;

namespace VRVis.Elements {

    /// <summary>
    /// Holds all the information about a region.<para/>
    /// 
    /// This basically includes:<para/>
    /// - id<para/>
    /// - location (file it depends on)<para/>
    /// - start (start line)<para/>
    /// - end (end line)<para/>
    /// - properties (node properties with type and value)<para/>
    /// 
    /// Furthermore, an instance includes information about the according game objects:<para/>
    /// - region UI GameObject (visually shown region in opened code files)<para/>
    /// DISCARDED: [- region node GameObject (according node in the graph)]<para/>
    /// 
    /// The game object information is necessary to apply visual properties on regions.<para/>
    /// Those could change attributes like color or size according to property values.
    /// </summary>
    public class Region {

        private readonly string id;
        private readonly string location;
        private int[] nodes;
        private int locs; // total lines of code

        // to store node sections (e.g. if nodes = [1,2,3,4,8,9] -> [from: 1, to: 4] and [from: 8, to: 9])
        public struct Section {

            /// <summary>Starts at 1 if represents first line in a file</summary>
            public int start;
            public int end;
            private int locs;

            public Section(int start, int end) {
                this.start = start;
                this.end = end;
                locs = 0;
                UpdateLOCs();
            }

            /// <summary>Lines of code this section uses.</summary>
            public int GetLOCs() { return locs; }
            public void UpdateLOCs() { locs = end - start; }
        }

        private List<Section> sections = new List<Section>();

        // different kinds of loaded properties, their names and instances
        // it is stored in such a complex structure to keep the "indexing" performance aspect
        // (we often have to search for specific properties, which is now pretty easy - not always iteration over all objects required)
        private Dictionary<ARProperty.TYPE, Dictionary<string, ARProperty>> properties = new Dictionary<ARProperty.TYPE, Dictionary<string, ARProperty>>();

        // the game objects that represents this region in the scene
        private Dictionary<ARProperty.TYPE, List<GameObject>> gameObjects = new Dictionary<ARProperty.TYPE, List<GameObject>>();

        // stores min/max values of non functional properties (NFP) (key = prop. name)
        private Dictionary<string, MinMaxValue> nfpMinMaxValues = new Dictionary<string, MinMaxValue>();

        // store current NFP color
        private Color currentNFPColor = Color.black;


        // CONSTRUCTOR

        /// <summary>
        /// Instantiate the region from the given JSON data.<para/>
        /// ToDo: check if JSON data includes the required information and throw errors accordingly (maybe create new static method "FromJSON")
        /// </summary>
        public Region(JObject o) {
            
            // get id and location
            id = (string) o["id"];    
            location = (string) o["location"];
            //Debug.Log("Loading information of region: " + id);

            // get nodes int array
            int[] readNodes = new int[0];
            if (o["nodes"] != null) {

                if (o["nodes"].Type == JTokenType.Array) {

                    // ToDo: maybe add ability to add ranges in array like ["5-30", "40-50", ...]
                    // get entry like "[5, 6, 7, ...]" and add to the nodes array
                    JArray jsonNodes = (JArray) o["nodes"];
                    readNodes = new int[jsonNodes.Count];
                    for (int i = 0; i < jsonNodes.Count; i++) {
                        int val = (int) jsonNodes[i];
                        readNodes[i] = val > 0 ? val : 0; // only positive numbers allowed
                    }
                }
                else if (o["nodes"].Type == JTokenType.String) {

                    // get entry like "5-30" and add all numbers "5, 6, ... 29, 30" to the nodes array
                    string jsonString = (string) o["nodes"];
                    readNodes = Utility.StringFromToArray(jsonString, '-', true);
                }
            }

            // set the nodes and process the sections
            SetNodes(readNodes);
            ProcessNodes();
            locs = readNodes.Length;

            // get properties
            //Debug.Log("Loading region properties...");
            JArray jsonProps = (JArray) o["properties"];
            for (int i = 0; i < jsonProps.Count; i++) {
                JObject jsonProp = (JObject) jsonProps[i];
                ARProperty property = RPropertyFactory.GetInstance().GetPropertyFromJSON(this, jsonProp, i);
                if (property == null) { continue; }
                AddProperty(property);
            }
            string msg = "Region " + id + " has " + properties.Count + " propert" + (properties.Count == 1 ? "y" : "ies");
            if (properties.Count == 0) { Debug.LogWarning(msg); } //else { Debug.Log(msg); }
        }


        // GETTER AND SETTER

        public string GetID() { return id; }

        public string GetLocation() { return location; }

        public int[] GetNodes() { return nodes; }
        private void SetNodes(int[] nodes) { this.nodes = nodes; }

        /// <summary>Get the lines of code (equal to the amount of nodes).</summary>
        public int GetLOCs() { return locs; }

        public List<Section> GetSections() { return sections; }

        public List<ARProperty.TYPE> GetPropertyTypes() { return new List<ARProperty.TYPE>(properties.Keys); }

        /// <summary>This method needs some performance, so avoid using it often.</summary>
        public List<ARProperty> GetProperties() {
            List<ARProperty> list = new List<ARProperty>();
            foreach (KeyValuePair<ARProperty.TYPE, Dictionary<string, ARProperty>> entry in properties) {
                list.AddRange(entry.Value.Values);
            }
            return list;
        }

        /// <summary>
        /// Returns the properties for this property type
        /// or null if the property type does not exist.
        /// </summary>
        public IEnumerable<ARProperty> GetProperties(ARProperty.TYPE propType) {
            if (!HasPropertyType(propType)) { return null; }
            return properties[propType].Values;
        }

        /// <summary>Get amount of different property types.</summary>
        public int GetPropertyTypesCount() { return properties.Keys.Count; }

        /// <summary>Check if the property type is known.</summary>
        public bool HasPropertyType(ARProperty.TYPE propertyType) {
            return properties.ContainsKey(propertyType);
        }

        /// <summary>Checks if this property with the according name is known.</summary>
        public bool HasProperty(ARProperty.TYPE propertyType, string propertyName) {
            return HasPropertyType(propertyType) && properties[propertyType].ContainsKey(propertyName);
        }

        /// <summary>Get the according visual property or null if unknown.</summary>
        public ARProperty GetProperty(ARProperty.TYPE propertyType, string propertyName) {
            if (!HasProperty(propertyType, propertyName)) { return null; }
            return properties[propertyType][propertyName];
        }

        private bool AddProperty(ARProperty property) {

            // check if property type is not known yet and add it if so
            ARProperty.TYPE propType = property.GetPropertyType();
            if (!HasPropertyType(propType)) {
                properties.Add(propType, new Dictionary<string, ARProperty>());
            }

            // check if this property is not yet known and add it if so
            string propName = property.GetName().ToLower();
            if (!HasProperty(propType, propName)) {
                properties[propType].Add(propName, property);

                // check if the feature occurs in the variability model and warn the user if it doesn't
                if (propType == ARProperty.TYPE.FEATURE) {

                    if (ApplicationLoader.GetInstance().GetVariabilityModelLoader().LoadedSuccessful()) {
                        VariabilityModel vm = ApplicationLoader.GetInstance().GetVariabilityModel();
                        if (!vm.HasOption(propName)) {
                            Debug.LogWarning("Feature (" + propName + ") of region (" + GetID() + ") not present in variability model!");
                        }
                    }
                }

                return true;
            }
            
            Debug.LogError("Region (" + GetID() + ") property already exists: " + propName + "!");
            return false;
        }

        /// <summary>Returns the raw dictionary. Some game objects might be null.</summary>
        public Dictionary<ARProperty.TYPE, List<GameObject>> GetUIGameObjects() { return gameObjects; }

        /// <summary>Returns the list of GameObjects matching this property type or null if type is unknown!</summary>
        public List<GameObject> GetUIGameObjects(ARProperty.TYPE propType) {
            if (!gameObjects.ContainsKey(propType)) { return null; }
            return gameObjects[propType];
        }

        /// <summary>Remove null game object entries of the specified type.</summary>
        public void ClearUIGameObjects(ARProperty.TYPE propType) {
            if (!HasPropertyType(propType)) { return; }
            GetUIGameObjects()[propType].RemoveAll(gameObj => gameObj == null);
        }

        /// <summary>
        /// Remove deleted game objects from the list.<para/>
        /// Requires an iteration over all components so use it with care.
        /// </summary>
        public void ClearUIGameObjects() {
            foreach (ARProperty.TYPE propertyType in GetUIGameObjects().Keys) {
                ClearUIGameObjects(propertyType);
            }
        }

        /// <summary>
        /// Add the game object if it is not already inside the list.<para/>
        /// Returns true if added and false otherwise.
        /// </summary>
        public bool AddUIGameObject(ARProperty.TYPE propType, GameObject newGO) {

            // create list and add object
            if (!gameObjects.ContainsKey(propType)) {
                gameObjects.Add(propType, new List<GameObject>());
                gameObjects[propType].Add(newGO);
                return true;
            }

            // just add the object if it doesnt exist yet
            if (gameObjects[propType].Contains(newGO)) { return false; }
            gameObjects[propType].Add(newGO);
            return true;
        }

        /// <summary>
        /// Removes the passed object from all lists.<para/>
        /// Returns true if the object could be found and removed from all lists!
        /// </summary>
        public bool RemoveUIGameObject(GameObject GO) {
            
            bool allRemoved = true;
            bool objectFound = false;
            foreach (KeyValuePair<ARProperty.TYPE, List<GameObject>> entry in gameObjects) {
                if (entry.Value.Contains(GO)) {
                    objectFound = true;
                    bool removed = entry.Value.Remove(GO);
                    if (allRemoved) { allRemoved = removed; }
                }
            }

            if (!allRemoved && objectFound) {
                Debug.LogWarning("Found GameObject requested to be deleted from region (" +
                    GetID() + ") but failed to remove all occurrences!");
            }

            return objectFound && allRemoved;
        }


        /// <summary>Get min/max values of all non functional properties.</summary>
        public Dictionary<string, MinMaxValue> GetNFPMinMaxValues() { return nfpMinMaxValues; }

        public void SetCurrentNFPColor(Color c) { currentNFPColor = c; }
        public Color GetCurrentNFPColor() { return currentNFPColor; }


        // FUNCTIONALITY

        /// <summary>
        /// Returns some summed up information about this region.<para/>
        /// The returned string must not be unique!<para/>
        /// Mainly used for printing debug information.
        /// </summary>
        public override string ToString() {
            return "(id: " + id +
                ", location: " + location + 
                ", nodes: " + nodes +
                ", properties: " + properties.Count +
                ")";
        }

        /// <summary>
        /// Process the given nodes.<para/>
        /// Sortes them and creates the list of sections.
        /// </summary>
        private void ProcessNodes() {

            // sort nodes
            Array.Sort(nodes);

            // remove duplicates
            bool dupesFound = false;
            bool lastDupe = false;
            List<int> fixedArray = new List<int>();
            for (int i = 1; i < nodes.Length; i++) {
                
                bool isDupe = nodes[i] == nodes[i-1];

                if (isDupe) {
                    dupesFound = true;

                    // consider last entry (add accordingly)
                    if (i == nodes.Length - 1 && !lastDupe) {
                        fixedArray.Add(nodes[i-1]);
                    }

                    lastDupe = true;
                }
                else {
                    fixedArray.Add(nodes[i-1]);
                    lastDupe = false;
                }
            }

            // set the fixed array to be the new one
            if (dupesFound) {
                int dupesCount = nodes.Length - fixedArray.Count;
                SetNodes(fixedArray.ToArray());
                Debug.LogWarning("Duplicates found (" + dupesCount + ") and removed in nodes of region: " + GetID() + " (location: " + location + ")");
            }

            // create sections (e.g. a section is [4,5,6,7] -> a continuous list)
            sections.Clear();
            int startIndex = 0;
            for (int i = 1; i <= nodes.Length; i++) {

                // to consider the last entry
                int curVal = 0;
                if (i == nodes.Length) { curVal = nodes[i-1] + 2; }
                else { curVal = nodes[i]; }

                if (curVal > nodes[i-1] + 1) {
                    Section s = new Section(nodes[startIndex], nodes[i-1]);
                    //Debug.Log("Region [" + GetID() + "] - section added: [" + s.start + ".." + s.end + "]");
                    sections.Add(s);
                    startIndex = i;
                }
            }
        }

        /// <summary>
        /// Updates the performance influence model values
        /// as well as the min/max value of all NFPs.
        /// </summary>
        public void UpdateNFPValues() {

            // get properties and check if any for this type event exist
            IEnumerable<ARProperty> NFPs = GetProperties(ARProperty.TYPE.NFP);
            if (NFPs == null) { return; }

            // reset previous min max values
            foreach (MinMaxValue mm in nfpMinMaxValues.Values) { mm.ResetMinMax(); }

            foreach (RProperty_NFP property in NFPs) {

                // calculate performance influence value of this property using the variability model configuration
                VariabilityModel vm = null;
                VariabilityModelLoader vml = ApplicationLoader.GetInstance().GetVariabilityModelLoader();
                if (vml.LoadedSuccessful()) { vm = vml.GetModel(); }
                
                // if variability model is missing, calculate the average of all values
                if (vm == null) { property.SetValue(property.GetAverageValue()); }
                else { vm.CalculatePIMValue(property); }

                // check if instance exists and update value
                MinMaxValue minMax = new MinMaxValue();
                string propName = property.GetName();
                if (nfpMinMaxValues.ContainsKey(propName)) { minMax = nfpMinMaxValues[propName]; }
                else { nfpMinMaxValues.Add(propName, minMax); }
                minMax.Update(property.GetValue());
            }
        }

    }
}
