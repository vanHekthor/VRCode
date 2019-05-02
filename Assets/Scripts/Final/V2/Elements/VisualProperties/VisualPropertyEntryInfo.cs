using Newtonsoft.Json.Linq;
using VRVis.RegionProperties;

namespace VRVis.VisualProperties {

    /**
     * Holds information about a method
     * that is usually hold by a visual property.
     * Such information can be the name or whether it is active.
     * 
     * An instance of this class will be passed to the VisualPropertyMethod
     * when its "Apply"-Method is called. As a result, the method has
     * knowledge about the basic information as well as the additional.
     */
    public class VisualPropertyEntryInfo {

        private readonly ARProperty.TYPE propertyType;
        private readonly string propertyName;
        private readonly string methodName;
        private bool active;

        // the following could also be replaced by a Dictionary<string,object> instance
        private JObject additionalInfo;


        // CONSTRUCTOR

        public VisualPropertyEntryInfo(ARProperty.TYPE propertyType, string propertyName, string methodName, bool active) {
            this.propertyType = propertyType;
            this.propertyName = propertyName;
            this.methodName = methodName;
            this.active = active;
        }


        // GETTER AND SETTER
        
        public ARProperty.TYPE GetPropertyType() { return propertyType; }

        public string GetPropertyName() { return propertyName; }

        public string GetMethodName() { return methodName; }

        public bool IsActive() { return active; }
        public void SetActive(bool active) { this.active = active; }
	
        public JObject GetAdditionalInformation() { return additionalInfo; }
        public void SetAdditionalInformation(JObject info) { additionalInfo = info; }

    }
}
