using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.RegionProperties;
using VRVis.Spawner;

namespace VRVis.Testing.Interaction {

    /// <summary>
    /// To easily test user selection changing
    /// using basic UI components (onClick and so on).<para/>
    /// This component is attached to the terminal.<para/>
    /// ToDo: Improve in future versions and for new GUI
    /// </summary>
    public class ChangeUserSelection : MonoBehaviour {

        //public string[] nfpNames;
        //public string[] featureNames;
        
        public GameObject applyUI;
        private bool checkingVariabilityModel = false;


	    void Start() {
		
            // get property names
            //InitializeNameArray(ARProperty.TYPE.NFP, out nfpNames);
            //InitializeNameArray(ARProperty.TYPE.FEATURE, out featureNames);
	    }


        /// <summary>Get property names of specified type.</summary>
        private void InitializeNameArray(ARProperty.TYPE propType, out string[] array) {

            RegionLoader rl = ApplicationLoader.GetInstance().GetRegionLoader();
            if (rl == null || !rl.LoadedSuccessful()) { array = new string[0]; return; }

            IEnumerable<string> names = rl.GetPropertyNames(propType);
            array = new string[name.Length];

            int i = 0;
            foreach (string name in names) { array[i++] = name; }
        }

	
	    public void SetNFPProperty(string propertyName) {
            ApplicationLoader.GetApplicationSettings().SetSelectedNFP(propertyName);
        }

        public void SetFeatureActive(string featureName) {
            int result = ApplicationLoader.GetApplicationSettings().AddActiveFeature(featureName);
            if (result == 0) { Debug.LogWarning("Maximum active features reached!"); }
            else if (result == -1) { Debug.Log("Feature already selected as active one!"); }
        }

        public void RemoveFeatureActive(string featureName) {
            ApplicationLoader.GetApplicationSettings().RemoveActiveFeature(featureName);
        }

        public void ToggleNFPVisualizationSwitch() {
            ApplicationLoader.GetApplicationSettings().ToggleNFPVisSwitch();
        }

        public void ToggleNFPVisualization() {
            ApplicationLoader.GetApplicationSettings().ToggleNFPVis();
        }

        public void SetNFPsRelative(bool relative) {
            if (ApplicationLoader.GetApplicationSettings().SetApplyLocalMinMaxValues(relative)) {
                Debug.Log("User set values " + (relative ? "relative" : "global"));
            }
        }


        /// <summary>Show/hide active feature visualization accordingly.</summary>
        public void ToggleActiveFeatureVisualization(bool visible) {
            ApplicationLoader.GetApplicationSettings().ToggleActiveFeatureVisualization(visible);
        }

        public void ShowNFPRegionMarking(bool show) {
            ApplicationLoader.GetApplicationSettings().SetNFPVisActive(Settings.ApplicationSettings.NFP_VIS.CODE_MARKING, show, true);
        }

        public void ShowNFPHeightmap(bool show) {
            ApplicationLoader.GetApplicationSettings().SetNFPVisActive(Settings.ApplicationSettings.NFP_VIS.HEIGHTMAP, show, true);
        }


        // ------------------------------------------------------------
        // Show/hide other visualizations

        /// <summary>Shows/hides the visualization of a spawner.</summary>
        private void SetSpawnerVis(string name, bool active) {
            ASpawner s = ApplicationLoader.GetInstance().GetSpawner(name);
            if (s == null) { return; }
            s.ShowVisualization(active);
        }

        public void ToggleSoftwareGraphVisualization(bool show) { SetSpawnerVis("StructureSpawner", show); }
        public void ToggleFeatureGraphVisualization(bool show) { SetSpawnerVis("VariabilityModelSpawner", show); }
        public void ToggleCodeCityVisualization(bool show) { SetSpawnerVis("CodeCityV1", show); }


        /// <summary>Simply validate the feature model configuration.</summary>
        public void ValidateFeatureModelConfiguration(Text outText) {
            
            if (!outText) {
                Debug.LogError("Output text not assigned!", outText);
                return;
            }

            outText.text = "checking...";
            bool valid = ApplicationLoader.GetApplicationSettings().IsFeatureModelValid();
            outText.text = valid ? "<color=#22BB22>valid</color>" : "<color=#AA1111>invalid</color>";
        }


        /// <summary>New/more advanced version of the validation.</summary>
        public void ValidateFeatureModelConfiguration(GameObject validateUI) {

            // don't do anything if validation is running
            if (checkingVariabilityModel) { return; }

            string err_msg = "Failed to validate model config!";

            VariabilityModelLoader mLoader = ApplicationLoader.GetInstance().GetVariabilityModelLoader();
            if (mLoader == null || !mLoader.LoadedSuccessful()) {
                Debug.LogError(err_msg + " - Invalid state of model loader.");
                return;
            }

            VariabilityModel model = mLoader.GetModel();
            if (model.IsCurrentlyBeingValidated() || checkingVariabilityModel) {
                Debug.LogWarning(err_msg + " - There is already a validation running.");
                return;
            }

            // only revalidate if model changed
            if (model.ChangedSinceLastValidation()) {

                if (validateUI) {
                    validateUI.SendMessage("ChangeText", "Checking...", SendMessageOptions.DontRequireReceiver);
                    validateUI.SendMessage("ChangeImageColor", VariabilityModel.COLOR_CHANGED, SendMessageOptions.DontRequireReceiver);
                }

                checkingVariabilityModel = true;
                StartCoroutine(ValidationCoroutine(validateUI));
            }
            else {

                string status = model.GetLastValidationStatus() ? "valid" : "invalid!";

                if (validateUI) {
                    validateUI.SendMessage("ChangeText", "Still " + status, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        /// <summary>Coroutine to validate model in next frame.</summary>
        private IEnumerator ValidationCoroutine(GameObject statusMain) {

            yield return new WaitForEndOfFrame();
            yield return 0;

            // start validation process
            bool valid = ApplicationLoader.GetApplicationSettings().IsFeatureModelValid();

            if (statusMain) {
                Color statusColor = valid ? VariabilityModel.COLOR_VALID : VariabilityModel.COLOR_INVALID;
                statusMain.SendMessage("ChangeText", valid ? "Valid" : "Invalid!", SendMessageOptions.DontRequireReceiver);
                statusMain.SendMessage("ChangeImageColor", statusColor, SendMessageOptions.DontRequireReceiver);
            }

            if (valid && applyUI) {
                applyUI.SendMessage("ChangeText", "Update required", SendMessageOptions.DontRequireReceiver);
                applyUI.SendMessage("ChangeImageColor", VariabilityModel.COLOR_CHANGED, SendMessageOptions.DontRequireReceiver);
            }

            checkingVariabilityModel = false;
        }


        /// <summary>Simply update NFP values without previous validation check (can lead to wrong results).</summary>
        public void UpdateNFPValues() {
            ApplicationLoader.GetInstance().UpdateNFPValues(true);
        }


        /// <summary>
        /// Apply variability model configuration.<para/>
        /// This first checks if the configuration is valid
        /// and then leads to an NFP value update request.<para/>
        /// It will not do anything if there is currently a validation running.
        /// </summary>
        public void ApplyVariabilityModelConfiguration(GameObject applyUI) {

            string err_msg = "Failed to apply variability model configuration!";

            VariabilityModelLoader mLoader = ApplicationLoader.GetInstance().GetVariabilityModelLoader();
            if (mLoader == null || !mLoader.LoadedSuccessful()) {
                Debug.LogError(err_msg + " - Invalid state of model loader.");
                return;
            }

            VariabilityModel model = mLoader.GetModel();
            if (model.IsCurrentlyBeingValidated() || checkingVariabilityModel) {
                Debug.LogWarning(err_msg + " - There is already a validation running.");
                return;
            }

            // notify about previously required validation
            if (model.ChangedSinceLastValidation()) {

                Debug.LogWarning(err_msg + " - Applying model configuration requires previous validation.");

                // show that validation is missing
                foreach (UISpawner s in ApplicationLoader.GetInstance().GetAttachedUISpawners()) {
                    s.NFPUpdateFailed("Not validated!");
                }

                return;
            }

            // do not apply if invalid
            bool isValid = model.GetLastValidationStatus();
            Color color = isValid ? VariabilityModel.COLOR_VALID : VariabilityModel.COLOR_INVALID;
            applyUI.SendMessage("ChangeImageColor", color, SendMessageOptions.DontRequireReceiver);
            ApplyVMConfig(isValid);
        }

        /// <summary>Apply the variability model configuration if valid</summary>
        private void ApplyVMConfig(bool valid) {

            if (!valid) {

                /*
                // set failure message
                foreach (UISpawner s in ApplicationLoader.GetInstance().GetAttachedUISpawners()) {
                    s.NFPUpdateProcessChanged(0, true);
                }

                // remove all NFP code window regions
                FileSpawner fs = ApplicationLoader.GetInstance().GetFileSpawner();
                if (fs != null) { fs.RemoveSpawnedFileRegions(ARProperty.TYPE.NFP); }
                */

                // just show that it failed
                foreach (UISpawner s in ApplicationLoader.GetInstance().GetAttachedUISpawners()) {
                    s.NFPUpdateFailed("Invalid config!");
                }

                return;
            }

            // recalculate and apply values
            UpdateNFPValues();
        }

    }
}
