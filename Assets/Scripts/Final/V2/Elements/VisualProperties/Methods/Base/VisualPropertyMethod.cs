using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.RegionProperties;
using VRVis.Spawner.Regions;
using VRVis.Utilities;

namespace VRVis.VisualProperties.Methods {

    /// <summary>
    /// Abstract base method for visual properties.
    /// </summary>
    public abstract class VisualPropertyMethod : ARangeMethod {

        private readonly string methodName;


        // CONSTRUCTOR

        public VisualPropertyMethod(string methodName) {
            this.methodName = methodName;
        }


        // GETTER AND SETTER

        public string GetMethodName() { return methodName; }


        // FUNCTIONALITY

        /// <summary>
        /// Apply the implementation of this method to the region.<para/>
        /// </summary>
        /// <param name="obj">The GameObject instance to apply visual features on</param>
        /// <param name="visProp">Visual Property (name of property and according methods)</param>
        /// <param name="entryInfo">Holds information about the method (name, active, additional information) - info stored in JSON file</param>
        public abstract bool Apply(GameObject obj, VisualProperty visProp, VisualPropertyEntryInfo entryInfo);

        /// <summary>
        /// Get the required region information attached to a game object.<para/>
        /// Returns the instance or null if not found and
        /// logs an error that applying the visual property failed.
        /// </summary>
        public RegionGameObject GetGameObjectInfo(GameObject obj) {
            RegionGameObject goInfo = obj.GetComponent<RegionGameObject>();
            if (!goInfo) {
                Debug.LogError("Failed to apply visual properties - RegionGameObject component missing!");
                return null;
            }
            return goInfo;
        }

        /// <summary>
        /// Get min/max values according to the definitions
        /// as well as to the current selections the user made (relative to file or global...).<para/>
        /// Returns true on success and false if e.g. property and/or codefile parameter is invalid.
        /// </summary>
        public bool GetMinMaxValues(ARProperty property, CodeFile codeFile, out float min, out float max) {

            // if there is global min/max information available for this property,
            // and if min and/or max is explicitly set in the visual properties file,
            // use last one as the range because user input has priority
            // --- ONLY if the user selected "local file min/max",
            // then always this will be shown and visual property definition has no affect!
                
            min = GetRangeMin();
            max = GetRangeMax();

            if (property == null || codeFile == null) { return false; }

            // If the user only wants to see the value relative to min/max of the file,
            // overwrite the min and max value here accordingly!
            // This requires accessing the CodeFile instance and that it has these values already calculated!
            string propertyName = property.GetName();
            bool localMinMax = ApplicationLoader.GetApplicationSettings().GetApplyLocalMinMaxValues();
            if (localMinMax) {
                MinMaxValue fileMinMax = codeFile.GetNFPMinMaxValue(propertyName);
                if (fileMinMax == null) { return false; }
                min = fileMinMax.GetMinValue();
                max = fileMinMax.GetMaxValue();
            }
            else { // use globally calculated min/max values

                MinMaxValue globalMinMax = ApplicationLoader.GetInstance().GetStructureLoader().GetNFPMinMaxValue(propertyName);
                if (globalMinMax == null) { return false; }
                if (!IsRangeMinSet()) { min = globalMinMax.GetMinValue(); }
                if (!IsRangeMaxSet()) { max = globalMinMax.GetMaxValue(); }

            }

            return true;
        }

    }
}
