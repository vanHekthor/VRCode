using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VRVis.IO.Features {

    /// <summary>
    /// Represents a software configuration containing binary and numery options. It can be evaluated by the Influence Model.   
    /// </summary>
    public class Configuration {
        public string Name { get; set; }

        private string configID;        

        private Dictionary<string, bool> binaryOptions;
        private Dictionary<string, float> numericOptions;
        private Dictionary<string, string> selectOptions;

        //public List<Feature_Boolean> BinaryOptionList { get; set; }
        //public List<Feature_Range> NumericOptionList { get; set; }

        //public Configuration(List<Feature_Boolean> binaryOptionList, List<Feature_Range> numericOptionList) {
        //    BinaryOptionList = binaryOptionList;
        //    NumericOptionList = numericOptionList;

        //    foreach (Feature_Boolean binaryOption in BinaryOptionList) {
        //        binaryOptions.Add(binaryOption.GetName(), binaryOption.GetValue() > 0);
        //    }

        //    foreach (Feature_Range numericOption in NumericOptionList) {
        //        numericOptions.Add(numericOption.GetName(), numericOption.GetValue());
        //    }
        //}

        public Configuration(string configID, Dictionary<string, bool> binaryOptions, Dictionary<string, float> numericOptions, Dictionary<string, string> selectOptions) {
            this.configID = configID;
            this.binaryOptions = binaryOptions;
            this.numericOptions = numericOptions;
            this.selectOptions = selectOptions;
        }

        public string GetConfigID() {
            return configID;
        }

        private void SetConfigID(string value) {
            configID = value;
        }

        public HashSet<string> GetActiveBinaryOptions() {
            HashSet<string> activeBinaryOptions = new HashSet<string>();
            foreach (KeyValuePair<string, bool> optionEntry in binaryOptions) {
                if (optionEntry.Value) {
                    activeBinaryOptions.Add(optionEntry.Key);
                }
            }
            return activeBinaryOptions;
        }

        public Dictionary<string, float> GetNumericOptions() {
            return numericOptions;
        }

        public float GetOptionValue(string optionName, out bool optionExists) {
            optionExists = false;
            if (binaryOptions.ContainsKey(optionName)) {
                optionExists = true;
                return binaryOptions[optionName] ? 1 : 0;
            }
            if (binaryOptions.ContainsKey(optionName.ToUpper())) {
                optionExists = true;
                return binaryOptions[optionName.ToUpper()] ? 1 : 0;
            }
            if (numericOptions.ContainsKey(optionName)) {
                optionExists = true;
                return numericOptions[optionName];
            }
            if (numericOptions.ContainsKey(optionName.ToUpper())) {
                optionExists = true;
                return numericOptions[optionName.ToUpper()];
            }

            Debug.LogError("Invalid option name! The configuration does not have an option called: " + optionName);
            return 0;
        }

        public void SaveAsJson(string filePath) {
            Debug.LogWarning("Starting to save config file as json!...");

            dynamic configObj = new ExpandoObject();

            dynamic binaryOptionsObj = new ExpandoObject();
            var binaryDict = (IDictionary<string, object>)binaryOptionsObj;

            dynamic numericOptionsObj = new ExpandoObject();
            var numericDict = (IDictionary<string, object>)numericOptionsObj;

            foreach (KeyValuePair<string, bool> binaryOption in binaryOptions) {
                // configObj.binaryOptions[binaryOption.Key] = binaryOption.Value;
                binaryDict.Add(binaryOption.Key, binaryOption.Value);
            }

            foreach (KeyValuePair<string, float> numericOption in numericOptions) {
                // configObj.numericOptions[numericOption.Key] = numericOption.Value;
                numericDict.Add(numericOption.Key, numericOption.Value);
            }

            configObj.configID = GetConfigID();
            configObj.binaryOptions = binaryDict;
            configObj.numericOptions = numericDict;

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(configObj);
            File.WriteAllText(filePath, jsonString);
        }

        public static Configuration LoadFromJson(string filePath) {

            string jsonString = File.ReadAllText(filePath);

            var result = JsonConvert.DeserializeObject<Configuration>(jsonString); 
            Debug.Log(result.binaryOptions);

            Debug.Log("ID: " + result.configID);

            foreach (KeyValuePair<string, bool> binOption in result.binaryOptions) {
                Debug.Log("Key: " + binOption.Key + " Value: " + binOption.Value);
            }

            foreach (KeyValuePair<string, float> numOption in result.numericOptions) {
                Debug.Log("Key: " + numOption.Key + " Value: " + numOption.Value);
            }

            foreach (KeyValuePair<string, string> selectOption in result.selectOptions) {
                Debug.Log("Key: " + selectOption.Key + " Value: " + selectOption.Value);
            }

            result.Name = new DirectoryInfo(filePath).Parent.Name;

            return result;
        }

    }

}
