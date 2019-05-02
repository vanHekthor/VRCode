using System.Collections;
using System.Collections.Generic;

namespace VRVis.VisualProperties {

    /**
     * [DEPRECATED]
     * 
     * Abstract base class for visual properties.
     * Visual Property instances only hold general information
     * about a node property of one specific type (e.g. performance).
     * Visual Properties in general change visual features of a region
     * depending on the property values of that region.
     */
    public abstract class VisualProperty {

        private readonly string propertyName;
        private List<VisualPropertyEntryInfo> methods = new List<VisualPropertyEntryInfo>();
        private bool active;


        // CONSTRUCTOR
        
        public VisualProperty(string propertyName, bool active) {
            this.propertyName = propertyName;
            this.active = active;
        }


        // GETTER AND SETTER
        
        public string GetPropertyName() { return propertyName; }

        public List<VisualPropertyEntryInfo> GetMethods() { return methods; }
        public void SetMethods(List<VisualPropertyEntryInfo> methods) { this.methods = methods; }

        public void AddMethod(VisualPropertyEntryInfo method) { this.methods.Add(method); }
        public bool RemoveMethod(VisualPropertyEntryInfo method) { return this.methods.Remove(method); }

        public bool GetActive() { return active; }
        public void SetActive(bool active) { this.active = active; }


        // FUNCTIONALITY

        /**
         * Called for each regions property value of this visual property type.
         * It means that this method can be used to gather/collect
         * information about all values that apply to this property type.
         * For instance, one can use this functionality to get the value range (min/max).
         */
        //public virtual void ProcessValue(object value) {}
        // This is currently no longer used.
        // It seems easier to convert the visual property to its initial type and use a specific function instead.

    }
}
