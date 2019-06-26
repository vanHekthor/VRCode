using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.Elements;
using VRVis.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VRVis.RegionProperties;

namespace VRVis.IO {

    /// <summary>
    /// Loads nodes aka. "file regions" from given files.<para/>
    /// A region can be one or multiple lines and has properties that will be mapped to visual properties.
    /// Regions can intersect each other so only one NFP region is shown at a time which should not intersect.
    /// </summary>
    public class RegionLoader : FileLoader {

        // regions files naming convention definition search pattern
        // https://docs.microsoft.com/de-de/dotnet/api/system.io.directoryinfo.getfiles?view=netframework-4.7.2#System_IO_DirectoryInfo_GetFiles_System_String_System_IO_SearchOption_
        private static readonly string namingConvention = "regions_*.json";

        /// <summary>key = id of a region, value = the region instance</summary>
        private Dictionary<string, Region> regions = new Dictionary<string, Region>();

        // The following is "global" information that works like an index.
        // Key is a file name and value is a list of region IDs.
        // (this is like an implementation of the indexing for faster lookups)
        // https://docs.unity3d.com/ScriptReference/Hashtable.html
        // https://docs.microsoft.com/en-us/dotnet/api/system.collections.hashtable?redirectedfrom=MSDN&view=netframework-4.7.2
        private Dictionary<string, List<string>> fileRegions = new Dictionary<string, List<string>>();

        /// <summary>Different kinds of loaded properties and their names (to quickly tell if the property does even exist)</summary>
        private Dictionary<ARProperty.TYPE, HashSet<string>> propertyTypes = new Dictionary<ARProperty.TYPE, HashSet<string>>();


        // CONSTRUCTORS

	    public RegionLoader(string[] filePaths)
        : base(filePaths) {}

        public RegionLoader(string filePath)
        : this(new string[]{filePath}) {}


        // GETTER AND SETTER

        /// <summary>All region instances.</summary>
        public Region[] GetRegions() {
            Region[] arr = new Region[regions.Count];
            regions.Values.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>
        /// Try to add a new region.<para/>
        /// Returns true on success and false if it already exists.
        /// </summary>
        public bool AddRegion(string regionID, Region regionInstance) {
            if (regions.ContainsKey(regionID)) { return false; }
            regions.Add(regionID, regionInstance);
            return true;
        }


        /// <summary>Get IDs (value) of regions that belong to a specific file (key).</summary>
        public Dictionary<string, List<string>> GetFileRegionIDs() {
            return fileRegions;
        }

        /// <summary>
        /// Get all regions that belong to a file.<para/>
        /// If a file is unknown or has no regions,
        /// an empty list will be returned.<para/>
        /// Uses the relative path of a file!
        /// </summary>
        public List<Region> GetFileRegions(string file) {

            if (!fileRegions.ContainsKey(file)) {
                /*Debug.Log("There are no regions for file: " + file);*/
                return new List<Region>();
            }

            List<Region> regionsList = new List<Region>();
            foreach (string regionID in fileRegions[file]) {
                if (regions.ContainsKey(regionID)) {
                    regionsList.Add(regions[regionID]);
                }
            }
            
            return regionsList;
        }

        /// <summary>
        /// Holds the different property types available (as key)
        /// as well as the different property names for each type (value).<para/>
        /// Required to show user an overview of available properties to switch between.
        /// </summary>
        public Dictionary<ARProperty.TYPE, HashSet<string>> GetPropertyTypes() { return propertyTypes; }

        public bool PropertyTypeExists(ARProperty.TYPE propType) { return propertyTypes.ContainsKey(propType); }

        /// <summary>Tells if this property exists / has been loaded for any region.</summary>
        public bool PropertyExists(ARProperty.TYPE propType, string propName) {
            propName = propName.ToLower();
            if (!PropertyTypeExists(propType)) { return false; }
            return propertyTypes[propType].Contains(propName);
        }

        /// <summary>Returns the property names for the specified property type or empty list if type does not exist.</summary>
        public IEnumerable<string> GetPropertyNames(ARProperty.TYPE propertyType) {
            if (!PropertyTypeExists(propertyType)) { return new List<string>(); }
            return propertyTypes[propertyType];
        }


        // FUNCTIONALITY

        /// <summary>
        /// Used by ApplicationLoader to find files that are
        /// named according to naming convention ("regions_<name>.json").<para/>
        /// Returns a list of files that follow the convention.<para/>
        /// Returns null on errors so check for that before using the list!
        /// </summary>
        public static List<string> GetRegionFiles(string path) {

            DirectoryInfo dirInf = new DirectoryInfo(path);

            if (!dirInf.Exists) {
                Debug.LogError("Folder with region files does not exist: " + path);
                return null;
            }

            // get valid files and add full path to list
            FileInfo[] files = dirInf.GetFiles(namingConvention, SearchOption.TopDirectoryOnly);
            List<string> paths = new List<string>();
            foreach (FileInfo fi in files) { paths.Add(fi.FullName); }
            return paths;
        }

        /// <summary>
        /// Load the Region instances from the JSON files.<para/>
        /// Before: https://docs.unity3d.com/Manual/JSONSerialization.html <para/>
        /// Now: https://www.newtonsoft.com/json/help/html/ParsingLINQtoJSON.htm
        /// </summary>
        public override bool Load() {

            loadingSuccessful = false;

            Debug.Log("Loading regions...");

            // load the regions from each file
            int failed = 0;
            foreach (string filePath in GetFilePaths()) {
                if (!LoadFile(filePath)) {
                    Debug.LogWarning("Failed to load regions from file: " + filePath);
                    failed++;
                }
            }

            Debug.Log("Regions loaded (" + regions.Count + "). Gathering property information...");

            // to set default non functional property
            bool firstNFPset = false;

            foreach (Region region in regions.Values) {

                // index creation to find regions regarding specific files faster
                string fileLocation = Utility.GetFormattedPath(region.GetLocation());
                if (!fileRegions.ContainsKey(fileLocation)) { fileRegions[fileLocation] = new List<string>(); }
                fileRegions[fileLocation].Add(region.GetID());
                //Debug.Log("Added region index for file: " + fileLocation + " (regions total: " +  fileRegions[fileLocation].Count + ")");

                // store the different property types and their names (indexing step)
                // (this step is also required to provide the user an overview of existing properties to switch between)
                foreach (ARProperty prop in region.GetProperties()) {

                    if (!propertyTypes.ContainsKey(prop.GetPropertyType())) {
                        propertyTypes.Add(prop.GetPropertyType(), new HashSet<string>());
                    }

                    // always store lower case for uniqueness
                    string propertyName = prop.GetName().ToLower();
                    propertyTypes[prop.GetPropertyType()].Add(propertyName);

                    // set the first (default) selected non functional property
                    if (!firstNFPset && prop.GetPropertyType() == ARProperty.TYPE.NFP) {
                        firstNFPset = true;
                        ApplicationLoader.GetApplicationSettings().SetSelectedNFP(propertyName);
                        //ApplicationLoader.GetApplicationSettings().SetSelectedNFP("memory"); // testing
                    }
                }
            }

            string msg = "Loading region files finished (" + failed + "/" + GetFilePaths().Length + " failed)";
            if (failed > 0) { Debug.LogWarning(msg); } else { Debug.Log(msg); }
            loadingSuccessful = true;
            return true;
        }

        /// <summary>
        /// Load regions from a single file.<para/>
        /// This parses the JSON data and turns it into Region instances
        /// which will be added to the "regions" dictionary.
        /// </summary>
        private bool LoadFile(string filePath) {

            if (!File.Exists(filePath)) {
                Debug.LogError("Nodes file does not exist! (" + filePath + ")");
                return false;
            }

            Debug.Log("Loading regions from file: " + filePath);

            // parse the JSON data to Region instances and add those regions
            using (StreamReader streamReader = File.OpenText(filePath)) {
                JsonTextReader jsonTextReader = new JsonTextReader(streamReader);
                JObject jObject = (JObject) JToken.ReadFrom(jsonTextReader);
                JArray regions = (JArray) jObject["regions"];
                if (regions == null) { Debug.LogError("Missing regions array!"); }
                else {
                    for (int i = 0; i < regions.Count; i++) {
                        JObject jsonRegion = (JObject) regions[i];
                        Region region = new Region(jsonRegion);

                        // try to add the region (will fail if it already exists)
                        if (!AddRegion(region.GetID(), region)) {
                            Debug.LogError("Region with ID (" + region.GetID() + ") already exists!");
                        }
                    }
                }
            }

            return true;
        }

    }
}
