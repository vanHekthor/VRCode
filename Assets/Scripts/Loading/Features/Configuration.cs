using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRVis.IO.Features {

    /// <summary>
    /// Represents a software configuration containing binary and numery options. It can be evaluated by the Influence Model.   
    /// </summary>
    public class Configuration {
        private readonly Dictionary<string, bool> binaryOptions;
        private readonly Dictionary<string, float> numericOptions;

        public Configuration(Dictionary<string, bool> binaryOptions, Dictionary<string, float> numericOptions) {
            this.binaryOptions = binaryOptions;
            this.numericOptions = numericOptions;
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
    }

}
