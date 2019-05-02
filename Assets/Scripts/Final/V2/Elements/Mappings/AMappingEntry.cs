using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;

namespace VRVis.Mappings {

    /// <summary>
    /// Abstract Mapping Entry class.<para/>
    /// This is the base class for mapping settings.<para/>
    /// It tells about the active state which all have in common.
    /// </summary>
    public abstract class AMappingEntry {

        private bool active = true;
        private bool active_default = true;


        // CONSTRUCTOR

        public AMappingEntry(bool active_default) {
            this.active_default = active_default;
            SetActive(active_default);
        }

        public AMappingEntry() : this(true) {}


        // GETTER AND SETTER

        protected void SetActive(bool state) { active = state; }

        public bool IsActive() { return active; }

        protected void SetActiveDefault(bool state) { active_default = state; }

        public bool IsActiveByDefault() { return active_default; }


        // FUNCTIONALITY

        /// <summary>
        /// Load base settings from JSON.<para/>
        /// Returns true on success and false if something is missing.
        /// </summary>
        public virtual bool LoadFromJSON(JObject o, ValueMappingsLoader loader, string name) {
        
            // optional active state (default: true)
            if (o["active"] != null) {
                bool active = true;
                if (bool.TryParse((string) o["active"], out active)) {
                    SetActive(active);
                    SetActiveDefault(active);
                }
            }

            return true;
        }

    }
}
