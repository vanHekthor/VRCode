using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.Mappings;
using VRVis.Mappings.Methods;

namespace VRVis.IO {

    /// <summary>
    /// Loads mappings of values to specific properties.<para/>
    /// For instance, mapping color to a region based on its value,
    /// or mapping color to a feature or mapping color, width and much more to an edge.<para/>
    /// Regions, features and edges have to be loaded previously to check validity of entries!
    /// </summary>
    public class ValueMappingsLoader : FileLoader {

        public enum SettingType { UNKNOWN, NFP, FEATURE, EDGE, FILENAME };

        /// <summary>Name of the key for default mappings (must be lower case!)</summary>
        public readonly string KEY_DEFAULT = "default";

        // mapping files naming convention definition search pattern
        // https://docs.microsoft.com/de-de/dotnet/api/system.io.directoryinfo.getfiles?view=netframework-4.7.2#System_IO_DirectoryInfo_GetFiles_System_String_System_IO_SearchOption_
        private static readonly string namingConvention = "mappings_*.json";

        /// <summary>
        /// Stores all method instances for non functional properties.<para/>
        /// Key = Name of the method (lower case)<para/>
        /// Value = Method instance
        /// </summary>
        private Dictionary<string, IMappingMethod> methods_nfps = new Dictionary<string, IMappingMethod>();

        // use string as key instead of instance because we can not be sure that such nfps exist
        private Dictionary<string, NFPSetting> settings_nfps = new Dictionary<string, NFPSetting>();
        private bool firstNFPSetting = true;

        private Dictionary<string, IMappingMethod> methods_features = new Dictionary<string, IMappingMethod>();
        private Dictionary<string, FeatureSetting> settings_features = new Dictionary<string, FeatureSetting>();
        private bool firstFeatureSetting = true;

        private Dictionary<string, IMappingMethod> methods_edges = new Dictionary<string, IMappingMethod>();
        private Dictionary<string, EdgeSetting> settings_edges = new Dictionary<string, EdgeSetting>();
        private bool firstEdgeSetting = true;

        private IDictionary<string, IMappingMethod> methods_filenames = new Dictionary<string, IMappingMethod>();
        private IDictionary<string, FilenameSetting> settings_filenames = new Dictionary<string, FilenameSetting>();
        private bool firstFilenameSetting = true;

        /// <summary>Path of the currently loaded file</summary>
        private string curFile = "";



        // CONSTRUCTOR

        public ValueMappingsLoader(string[] filePaths)
        : base(filePaths) {}

        public ValueMappingsLoader(string filePath)
        : this(new string[]{filePath}) {}



        // GETTER AND SETTER

        /// <summary>Returns the setting if found, or default setting otherwise!</summary>
        public NFPSetting GetNFPSetting(string nfpName) {
            nfpName = nfpName.ToLower();
            if (!settings_nfps.ContainsKey(nfpName)) { return settings_nfps[KEY_DEFAULT]; }
            return settings_nfps[nfpName];
        }

        /// <summary>Returns true if there is a mapping defined for this non functional property.</summary>
        public bool HasNFPSetting(string nfpName) { return settings_nfps.ContainsKey(nfpName.ToLower()); }

        /// <summary>Returns the setting if found, or default setting otherwise!</summary>
        public FeatureSetting GetFeatureSetting(string featureName) {
            featureName = featureName.ToLower();
            if (!settings_features.ContainsKey(featureName)) { return settings_features[KEY_DEFAULT]; }
            return settings_features[featureName];
        }
 
        /// <summary>Returns true if there is such a mapping defined.</summary>
        public bool HasFeatureSetting(string featureName) { return settings_features.ContainsKey(featureName.ToLower()); }

        /// <summary>Returns the setting if found, or default setting otherwise!</summary>
        public EdgeSetting GetEdgeSetting(string edgeType) {
            edgeType = edgeType.ToLower();
            if (!settings_edges.ContainsKey(edgeType)) { return settings_edges[KEY_DEFAULT]; }
            return settings_edges[edgeType];
        }

        /// <summary>Returns true if there is a mapping defined for this edge type.</summary>
        public bool HasEdgeSetting(string edgeType) { return settings_edges.ContainsKey(edgeType.ToLower()); }

        /// <summary>Returns the setting if found, or default setting otherwise!</summary>
        public FilenameSetting GetFilenameSetting(string name) {
            name = name.ToLower();
            if (!settings_filenames.ContainsKey(name)) { return settings_filenames[KEY_DEFAULT]; }
            return settings_filenames[name];
        }

        /// <summary>Returns the whole collection of filename mappings.</summary>
        public IEnumerable<FilenameSetting> GetFilenameSettings() { return settings_filenames.Values; }

        /// <summary>Returns true if there is such a mapping defined.</summary>
        public bool HasFilenameSetting(string name) { return settings_filenames.ContainsKey(name.ToLower()); }
        public bool HasFilenameSettings() { return settings_filenames.Count > 0; }

        /// <summary>
        /// Get a mapping method with the specific name for this setting type.<para/>
        /// Returns the method instance or null if not found!
        /// </summary>
        public IMappingMethod GetMappingMethod(string name, SettingType settingType) {

            // convert name to lower case because method names were stored like this
            name = name.ToLower();
            
            // check dictionary of according type
            switch (settingType) {

                case SettingType.NFP:
                    return methods_nfps.ContainsKey(name) ? methods_nfps[name] : null;

                case SettingType.FEATURE:
                    return methods_features.ContainsKey(name) ? methods_features[name] : null;

                case SettingType.EDGE:
                    return methods_edges.ContainsKey(name) ? methods_edges[name] : null;

                case SettingType.FILENAME:
                    return methods_filenames.ContainsKey(name) ? methods_filenames[name] : null;
            }
            
            return null;
        }

        /// <summary>
        /// Get all edge types that are relative to the passed setting.<para/>
        /// Returns an enumerable of strings. This can be empty if there are no such edge types.
        /// </summary>
        public IEnumerable<string> GetEdgeTypesRelativeTo(EdgeSetting.Relative relativeTo) {

            HashSet<string> relEdgeTypes = new HashSet<string>();

            foreach (KeyValuePair<string, EdgeSetting> e in settings_edges) {
                if (e.Value.GetRelativeTo() == relativeTo) { relEdgeTypes.Add(e.Key); }
            }

            return relEdgeTypes;
        }


        /// <summary>Get the type from string. Returns type "UNKNOWN" on failure.</summary>
        public static SettingType GetPropertyTypeFromString(string type) {

            if (type == null) { return SettingType.UNKNOWN; }
            type = type.ToLower();

            if (type.Equals("nfp")) { return SettingType.NFP; }
            if (type.Equals("feature")) { return SettingType.FEATURE; }
            if (type.Equals("edge")) { return SettingType.EDGE; }
            if (type.Equals("filename")) { return SettingType.FILENAME; }
            
            return SettingType.UNKNOWN;
        }



        // FUNCTIONALITY

        /// <summary>
        /// Loads the mappings files.<para/>
        /// NOTE: "default" mapping must be the first entry read of a setting type, otherwise it wont be overwritten.
        /// </summary>
        /// <returns></returns>
        public override bool Load() {
            
            loadingSuccessful = false;

            // create the default settings for each type
            CreateDefaultSettings();

            // try to load the mappings from each file
            uint success = 0;
            uint total = 0;

            foreach (string filePath in GetFilePaths()) {
                total++;
                curFile = filePath;
                if (LoadFile(filePath)) { success++; }
                else { Debug.LogWarning("Failed to load mappings from file: " + filePath); }
            }

            // print debug result information
            string msg = "Finished loading " + success + "/" + total + " mapping files!\n(" +
                "methods: nfp=" + methods_nfps.Count + ", feature=" + methods_features.Count + ", edge=" + methods_edges.Count + ", filename=" + methods_filenames.Count + "; " +
                "settings: nfp=" + settings_nfps.Count + ", feature=" + settings_features.Count + ", edge=" + settings_edges.Count + ", filename=" + settings_filenames.Count + ")";
            if (success != total) { Debug.LogWarning(msg); } else { Debug.Log(msg); }

            loadingSuccessful = true;
            return true;
        }


        /// <summary>
        /// Load the mappings from a file.
        /// </summary>
        private bool LoadFile(string filePath) {

            // load the json object from file
            Debug.Log("Loading mappings from file: " + filePath);
            using (StreamReader sr = File.OpenText(filePath)) {

                // get the main json element
                JObject o = (JObject) JToken.ReadFrom(new JsonTextReader(sr));

                // try to get any of the types
                foreach (SettingType sType in Enum.GetValues(typeof(SettingType))) {

                    // skip unknown entry
                    if (sType == SettingType.UNKNOWN) { continue; }

                    // check if there is an entry for this type
                    string curType = sType.ToString().ToLower();
                    string curType_upper = sType.ToString().ToUpper();

                    JObject entry = null;
                    if (o[curType] != null) { entry = (JObject) o[curType]; }
                    else if (o[curType_upper] != null) { entry = (JObject) o[curType_upper]; }

                    if (entry != null) {

                        // see https://stackoverflow.com/a/7216958
                        JArray methods = (JArray) entry["methods"];
                        if (methods == null || methods.Count == 0) { Debug.LogWarning("No method defined in JSON file! (path: " + filePath + ", type: " + curType + ")");  }
                        else { ParseMethods(sType, methods); }

                        JArray props = (JArray) entry["mapping"];
                        if (props == null || props.Count == 0) { Debug.LogWarning("No mapping defined in JSON file! (path: " + filePath + ", type: " + curType + ")"); }
                        else { ParseMappings(sType, props); }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Used by ApplicationLoader to find files that are
        /// named according to naming convention ("mappings_<name>.json").<para/>
        /// Returns a list of files that follow the convention.<para/>
        /// Returns null on errors so check for that before using the list!
        /// </summary>
        public static List<string> GetMappingFiles(string path) {

            DirectoryInfo dirInf = new DirectoryInfo(path);

            if (!dirInf.Exists) {
                Debug.LogError("Folder with mapping files does not exist: " + path);
                return null;
            }

            // get valid files and add full path to list
            FileInfo[] files = dirInf.GetFiles(namingConvention, SearchOption.TopDirectoryOnly);
            List<string> paths = new List<string>();
            foreach (FileInfo fi in files) { paths.Add(fi.FullName); }
            return paths;
        }


        /// <summary>
        /// Create default settings with name "default".<para/>
        /// The user can overwrite such settings in the file.
        /// </summary>
        private void CreateDefaultSettings() {

            // reset everything
            firstNFPSetting = true;
            firstFeatureSetting = true;
            firstEdgeSetting = true;
            firstFilenameSetting = true;
            if (settings_nfps != null) { settings_nfps.Clear(); }
            if (settings_features != null) { settings_features.Clear(); }
            if (settings_edges != null) { settings_edges.Clear(); }
            if (settings_filenames != null) { settings_filenames.Clear(); }

            // add default settings
            settings_nfps[KEY_DEFAULT] = NFPSetting.Default();
            settings_features[KEY_DEFAULT] = FeatureSetting.Default();
            settings_edges[KEY_DEFAULT] = EdgeSetting.Default();
            settings_filenames[KEY_DEFAULT] = FilenameSetting.Default();
        }


        /// <summary>
        /// Parse user defined methods.<para/>
        /// Such methods are defined in the JSON file under the key "methods".
        /// </summary>
        private void ParseMethods(SettingType settingType, JArray methods) {

            // parse all method entries
            uint failed = 0;
            for (int i = 0; i < methods.Count; i++) {
                JObject entry = (JObject) methods[i];
                
                // parse single entry to method instance
                IMappingMethod method;

                try { method = ParseMethod(entry); }
                catch (Exception ex) {
                    Debug.LogError("Failed to parse mapping entry " + i + " (file: " + curFile + ")");
                    Debug.LogError(ex.StackTrace);
                    continue;
                }
                
                if (method == null) {
                    Debug.LogError("Failed to parse method mapping " + i + " (file: " + curFile + ")");
                    continue;
                }

                if (method.GetMethodName().Length == 0) {
                    Debug.LogWarning("Missing method name of methods entry " + i + " - skipping it (file: " + curFile + ")");
                    continue;
                }

                // Add the method instance to the according dictionary.
                // This will overwrite an already existing instance and thus, result in unique method names.
                string methodName = method.GetMethodName().ToLower();
                switch (settingType) {
                    case SettingType.NFP: methods_nfps[methodName] = method; break;
                    case SettingType.FEATURE: methods_features[methodName] = method; break;
                    case SettingType.EDGE: methods_edges[methodName] = method; break;
                    case SettingType.FILENAME: methods_filenames[methodName] = method; break;
                }
            }

            if (failed > 0) { Debug.LogWarning("Failed parsing " + failed + "/" + methods.Count + " mapping methods!"); }
            else { Debug.Log("Successfully parsed " + methods.Count + " mapping methods for type: " + settingType); }
        }

        /// <summary>Parse a single method definition.</summary>
        private IMappingMethod ParseMethod(JObject o) {
            return MappingMethodFactory.Create(o);
        }


        /// <summary>
        /// Parse the passed mapping array.<para/>
        /// Tries to get the according instance (NFP region, Feature or Edge)
        /// and storing the loaded settings for this instance.
        /// </summary>
        private void ParseMappings(SettingType settingType, JArray mappings) {

            uint failed = 0;
            for (int i = 0; i < mappings.Count; i++) {

                // get entry and try to parse it
                JObject jObj = (JObject) mappings[i];

                string name = "";
                string err_msg = "";
                AMappingEntry entry = ParseMapping(settingType, jObj, out name, out err_msg);
                if (entry == null) {
                    Debug.LogError("Failed to parse mapping entry " + i + " - " + err_msg + " (file: " + curFile + ")");
                    failed++;
                    continue;
                }

                // ToDo: maybe add an option to still enable/switch between multiple entries in application?
                // currently just ignore this entry so that it wont even show up
                if (!entry.IsActive()) {
                    Debug.LogWarning("Ignoring mapping entry " + i + " (disabled) of file: " + curFile);
                    continue;
                }

                // convert name to lower case
                name = name.ToLower();

                // add successfully parsed entry to according dictionary
                // (overwrites entries with names already used)
                switch (settingType) {
                    case SettingType.NFP: settings_nfps[name] = (NFPSetting) entry; break;
                    case SettingType.FEATURE: settings_features[name] = (FeatureSetting) entry; break;
                    case SettingType.EDGE: settings_edges[name] = (EdgeSetting) entry; break;
                    case SettingType.FILENAME: settings_filenames[name] = (FilenameSetting) entry; break;
                }
            }

            if (failed > 0) { Debug.LogWarning("Failed parsing " + failed + "/" + mappings.Count + " mappings!"); }
            else { Debug.Log("Successfully parsed " + mappings.Count + " mappings for type: " + settingType); }
        }


        /// <summary>
        /// Parse a single mapping.<para/>
        /// Returns the mapping entry instance or null on errors.
        /// </summary>
        /// <param name="err_msg">Contains the error message if null is returned</param>
        private AMappingEntry ParseMapping(SettingType settingType, JObject o, out string name, out string err_msg) {

            name = (string) o["name"];
            err_msg = "";

            if (name == null || name.Length == 0) {
                err_msg = "Missing required key \"name\"!";
                return null;
            }

            // convert to lower case
            name = name.ToLower();

            // check if overwriting default is allowed
            if (name.Equals(KEY_DEFAULT)) {

                bool ow = true;
                string t = "";

                switch (settingType) {
                    case SettingType.NFP: if (!firstNFPSetting) { ow = false; }; t = "nfps"; break;
                    case SettingType.FEATURE: if (!firstFeatureSetting) { ow = false; }; t = "features"; break;
                    case SettingType.EDGE: if (!firstEdgeSetting) { ow = false; }; t = "edges"; break;
                    case SettingType.FILENAME: if (!firstFilenameSetting) { ow = false; }; t = "filenames"; break;
                }

                if (!ow) {
                    err_msg = "Default mapping must always be the first one!";
                    return null;
                }
                else if (t.Length > 0) { Debug.LogWarning("Overwriting default mapping of " + t); }
            }

            // entry instance to be returned
            AMappingEntry entry = null;
            err_msg = "Failed to parse entry from JSON!"; // in case parsing fails

            // parse JSON to according type
            switch (settingType) {

                case SettingType.NFP:
                    entry = NFPSetting.FromJSON(o, this, settings_nfps[KEY_DEFAULT], name);
                    firstNFPSetting = false;
                    break;

                case SettingType.FEATURE:
                    entry = FeatureSetting.FromJSON(o, this, settings_features[KEY_DEFAULT], name);
                    firstFeatureSetting = false;
                    break;

                case SettingType.EDGE:
                    entry = EdgeSetting.FromJSON(o, this, settings_edges[KEY_DEFAULT], name);
                    firstEdgeSetting = false;
                    break;

                case SettingType.FILENAME:
                    entry = FilenameSetting.FromJSON(o, this, settings_filenames[KEY_DEFAULT], name);
                    firstFilenameSetting = false;
                    break;
            }

            return entry;
        }

    }
}
