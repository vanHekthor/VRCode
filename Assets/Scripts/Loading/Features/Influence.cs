using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.IO.Features {

    /// <summary>
    /// Represents an influence that may effect the property values of a software system.
    /// It contains the binary options that need to be active in order for the influence to actually take effect
    /// and the specific effect value it contributes to each property. 
    /// </summary>
    public class Influence {

        private readonly HashSet<string> relatedOptions;
        private readonly Dictionary<string, double> effectOnProperties;

        public Influence(HashSet<string> relatedOptions, Dictionary<string, double> effectOnProperties) {
            this.relatedOptions = relatedOptions;
            this.effectOnProperties = effectOnProperties;
        }

        public HashSet<string> GetRelatedOptions() {
            return relatedOptions;
        }

        public Dictionary<string, double> GetEffectOnProperties() {
            return effectOnProperties;
        }

    }
}
