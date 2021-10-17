using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace VRVis.IO.Features {

    /// <summary>
    /// Contains all influences that could effect the (non-functional) properties of a software system.
    /// Does the evaluation of non-functional properties for a region given a configuration.
    /// </summary>
    public class InfluenceModel {
        private readonly HashSet<string> properties;
        private readonly Dictionary<string, HashSet<Influence>> regionInfluenceDict;

        public InfluenceModel(HashSet<string> properties, Dictionary<string, HashSet<Influence>> regionInfluenceDict) {
            this.properties = properties;
            this.regionInfluenceDict = regionInfluenceDict;
        }

        /// <summary>
        /// Calculates the property values of the (code) region that has the passed ID and considering the passed configuration. 
        /// </summary>
        /// <param name="configuration">Contains the binary options that are active and the numeric option values</param>
        /// <param name="regionID">ID of region whose property values need to be evaluated</param>
        /// <returns>Dictionary with property name as key and the property value of the region as value.</returns>
        public Dictionary<string, double> EvaluateConfiguration(Configuration configuration, string regionID) {
            HashSet<string> activeOptions = configuration.GetActiveBinaryOptions();
            HashSet<string> numericOptions = new HashSet<string>(configuration.GetNumericOptions().Keys);

            // numeric options are always active
            activeOptions.UnionWith(numericOptions);

            Dictionary<string, double> evaluation = new Dictionary<string, double>();
            foreach (string property in properties) {
                evaluation.Add(property, 0.0);
            }

            HashSet<Influence> activeInfluences = new HashSet<Influence>();
            foreach (Influence influence in regionInfluenceDict[regionID]) {
                if (activeOptions.IsSupersetOf(influence.GetRelatedOptions())) {
                    activeInfluences.Add(influence);
                }
            }

            foreach (Influence activeInfluence in activeInfluences) {
                HashSet<string> relatedNumericOptions = new HashSet<string>(activeInfluence.GetRelatedOptions().Intersect(numericOptions));

                float factor = 1.0f;
                foreach (string numericOption in relatedNumericOptions) {
                    factor *= configuration.GetNumericOptions()[numericOption];
                }

                foreach (KeyValuePair<string, double> effect in activeInfluence.GetEffectOnProperties()) {
                    if (evaluation.ContainsKey(effect.Key)) {
                        evaluation[effect.Key] += factor * effect.Value;
                    }
                    else {
                        evaluation.Add(effect.Key, evaluation[effect.Key] + factor * effect.Value);
                    }
                }
            }

            return evaluation;
        }

        public Dictionary<string, HashSet<Influence>> GetRegionInfluenceDict() {
            return regionInfluenceDict;
        }
    }
}