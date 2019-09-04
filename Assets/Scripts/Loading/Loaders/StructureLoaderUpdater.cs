using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRVis.IO.Features;
using VRVis.RegionProperties;
using VRVis.Spawner;
using VRVis.Utilities;

namespace VRVis.IO {

    /// <summary>
    /// The updater script is required to run the update coroutines for NFP values.<para/>
    /// 
    /// It is required to be a separate script because it needs to be a "MonoBehaviour" to use "StartCoroutine".<para/>
    /// 
    /// A reference to the StructureLoader instance is set in the Awake-Method
    /// of the ApplicationLoader if the structure could be loaded successfully.<para/>
    /// 
    /// The instance of this class can be retrieved from the ApplicationLoader
    /// and should be attached to the same game object!.
    /// </summary>
    public class StructureLoaderUpdater : MonoBehaviour {

        [Tooltip("[NFP Update] How many files to update per coroutine run")]
        public uint filesPerRun = 2;

        [Tooltip("[NFP Update] How long to wait after a coroutine run finished before starting the next one")]
        public float waitPerRun = 0.1f;

        public UnityEvent nfpUpdateStartedEvent = new UnityEvent();
        public UnityEvent nfpUpdateFinishedEvent = new UnityEvent();

        // reference to the structure loader instance
        private StructureLoader loader;

        // tells if there is currently an update process running
        private bool updateNFPValues = false;
        private bool updateFinished = true;
        private int filesTotal; // total number of files to update
        private int fileNo; // number of current updated file
        private float progress = 0;
        private float updateTime = 0; // how long the update took

        // code file with min/max NFP value for the specific nfp name
        private Dictionary<string, CodeFile> minCodeFile = new Dictionary<string, CodeFile>();
        private Dictionary<string, CodeFile> maxCodeFile = new Dictionary<string, CodeFile>();

        private bool updateFailure = false; // failure due to invalid vm config


        // GETTER AND SETTER

        /// <summary>Set structure loader reference</summary>
        public void SetStructureLoader(StructureLoader loader) {
            this.loader = loader;
        }

        /// <summary>Get the current update progess (between 0 and 1).</summary>
        public float GetProgress() { return progress; }

        /// <summary>Set current update progress (between 0 and 1).</summary>
        private void SetProgress(float progress, bool updateUI = false) {
            this.progress = progress;
            if (updateUI) { UpdateUIFeedback(); }
        }

        /// <summary>
        /// Tells if the whole update process (including window update) is done.
        /// </summary>
        public bool IsUpdateFinished() { return updateFinished; }

        /// <summary>
        /// Get code file with min. value for this nfp.<para/>
        /// Returns the instance or <code>null</code> if not found.
        /// </summary>
        public CodeFile GetMinCodefile(string nfpName) {
            if (!minCodeFile.ContainsKey(nfpName)) { return null; }
            return minCodeFile[nfpName];
        }

        /// <summary>
        /// Get code file with max. value for this nfp.<para/>
        /// Returns the instance or <code>null</code> if not found.
        /// </summary>
        public CodeFile GetMaxCodefile(string nfpName) {
            if (!maxCodeFile.ContainsKey(nfpName)) { return null; }
            return maxCodeFile[nfpName];
        }


        // FUNCTIONALITY

        /// <summary>
        /// This method should be called on application startup after loading the structure
        /// and otherwise only after the feature configuration was modified.<para/>
        /// Returns true if the process started.
        /// </summary>
        /// <param name="stopRunningUpdate">Set true to stop a possible currently running update process</param>
        public bool UpdateNFPValues(bool stopRunningUpdate) {

            if (loader == null) {
                Debug.LogError("Failed to start NFP value update process! - Missing StructureLoader reference!");
                return false;
            }

            if (!loader.LoadedSuccessful()) { return false; }

            // check if there is currently an update process running
            if (updateNFPValues) {

                // no "permission" to stop the current update process
                if (!stopRunningUpdate) {
                    Debug.LogWarning("There is already a NFP value update process running!");
                    return false;
                }
                
                // stop the currently running update process
                updateNFPValues = false;
                StopCoroutine(UpdateNFPValuesCoroutine());
                Debug.LogWarning("Stopped currently running update process after " + (Time.time - updateTime) + " seconds!");
            }

            // reset progress information
            fileNo = 0;
            filesTotal = loader.GetFileCount();
            updateFailure = false;
            SetProgress(0, true);

            // don't start the coroutine if there are no files to update
            if (filesTotal == 0) {
                Debug.LogWarning("Not starting NFP value update process! - No files available.");
                return false;
            }

            // start the update process
            Debug.LogWarning("Starting NFP value update process!");
            updateNFPValues = true; // starts update process in next update loop
            updateFinished = false;
            updateTime = Time.time;


            // check if variability model is used and valid
            VariabilityModelLoader vml = ApplicationLoader.GetInstance().GetVariabilityModelLoader();
            if (vml != null && vml.LoadedSuccessful()) {

                VariabilityModel vm = vml.GetModel();
                if (vm != null) {

                    bool validationRequired = vm.ChangedSinceLastValidation();
                    bool invalid = !vm.GetLastValidationStatus();

                    if (validationRequired) {
                        Debug.LogWarning("Skipping NFP value update process! Variability Model not validated!");
                    }
                    else if (invalid) {
                        Debug.LogWarning("Skipping NFP value update process! Variability Model is invalid!");
                    }

                    // set that the update process is done
                    if (invalid || validationRequired) {
                        updateFailure = true;
                        NFPValuesUpdateFinished(true);
                        return true;
                    }

                    // set that there were values applied at least once
                    if (!vm.GetValuesAppliedOnce()) {
                        vm.SetValuesAppliedOnce(true);
                    }
                }
            }


            // starts the next step of the update process
            StartCoroutine(UpdateNFPValuesCoroutine());
            return true;
        }

        /// <summary>
        /// IEnumerator to use the functionality of the update method as a coroutine
        /// so that we can for instance say, only one file per frame or every ... ms.<para/>
        /// 
        /// A "non functional property value update" consists of the following steps:<para/>
        /// 1. Recalculate PIM value for each NFP property of each region.<para/>
        /// 2. Recalculate min/max value for each NFP property of each region.<para/>
        /// 3. Recalculate min/max value for each NFP property of each file.<para/>
        /// 
        /// To perform all of the previously mentioned steps, we do the following:<para/>
        /// - Reset global min/max for each NFP property<para/>
        /// - For each file<para/>
        /// -- Reset min/max for each of the files NFP properties<para/>
        /// -- For each region<para/>
        /// ---- For each NFP property<para/>
        /// ------ calculate PIM value based on current feature configuration<para/>
        /// ------ compare with current file min/max for this property and set accordingly<para/>
        /// ------ compare with current global min/max for this property and set accordingly
        /// </summary>
        private IEnumerator UpdateNFPValuesCoroutine() {

            // set that the update process is running
            updateNFPValues = true;
            updateFinished = false;
            
            // reset min/max for each property name (otherwise values wrong after changing configuration!)
            foreach (var entry in loader.GetNFPMinMaxValues()) { entry.Value.ResetMinMax(); }

            // reset min/max code files
            minCodeFile.Clear();
            maxCodeFile.Clear();

            uint filesProcessed = 0;

            IEnumerable<CodeFile> files = loader.GetFiles();
            foreach (CodeFile file in files) {
                
                // recalculate PIM values for each region and each of its NFPs and min/max of the file
                file.UpdateNFPValues();

                // update the global min/max values for NFPs depending on the file min/max values
                foreach (KeyValuePair<string, MinMaxValue> entry in file.GetNFPMinMaxValues()) {
                    
                    string propName = entry.Key;
                    MinMaxValue minMax = loader.GetNFPMinMaxValue(propName); // null if missing
                    if (minMax == null) {
                        minMax = new MinMaxValue();
                        loader.AddNFPMinMaxValue(propName, minMax);
                    }

                    float minVal = entry.Value.GetMinValue();
                    float maxVal = entry.Value.GetMaxValue();

                    // store min/max code file
                    if (minVal < minMax.GetMinValue() || !minMax.IsMinValueSet()) { minCodeFile[propName] = file; }
                    if (maxVal > minMax.GetMaxValue() || !minMax.IsMaxValueSet()) { maxCodeFile[propName] = file; }

                    minMax.Update(minVal);
                    minMax.Update(maxVal);
                }

                // update the progress value accordingly (add +1 to total files so that 100 is reached if explicitly 1 is set)
                fileNo++;
                if (filesTotal != 0) { SetProgress(fileNo / (float) (filesTotal + 1), true); }
                //Debug.Log("File " + fileNo + " updated. (progress: " + progress + ")");
                
                // run next step later
                if (++filesProcessed == filesPerRun) {
                    filesProcessed = 0;
                    yield return new WaitForSecondsRealtime(waitPerRun);
                }
            }

            // ToDo: disable if not required!
            // TESTING: print min/max code file
            string selectedNFP = ApplicationLoader.GetApplicationSettings().GetSelectedNFP();
            CodeFile mincf = GetMinCodefile(selectedNFP);
            CodeFile maxcf = GetMaxCodefile(selectedNFP);
            MinMaxValue mm = loader.GetNFPMinMaxValue(selectedNFP);
            string nfpMinVal = mm == null ? "/" : mm.GetMinValue().ToString();
            string nfpMaxVal = mm == null ? "/" : mm.GetMaxValue().ToString();
            if (mincf != null) { Debug.Log("Codefile with min value: " + mincf.GetNode().GetPath() + ", value: " + nfpMinVal); }
            if (maxcf != null) {
                Debug.Log("Codefile with max value: " + maxcf.GetNode().GetPath() + ", value: " + nfpMaxVal);

                // find max region
                Elements.Region maxRegion = null;
                float curMax = Mathf.NegativeInfinity;
                foreach (Elements.Region r in maxcf.GetRegions()) {

                    ARProperty prop = r.GetProperty(ARProperty.TYPE.NFP, selectedNFP);
                    if (prop == null) { continue; }
                    float thisVal = ((RProperty_NFP) prop).GetValue();
                    if (thisVal > curMax) {
                        curMax = thisVal;
                        maxRegion = r;
                    }
                }

                if (maxRegion != null) { Debug.Log("Max nfp value region: " + maxRegion.GetID()); }
            }

            // set that the update process is done
            NFPValuesUpdateFinished(true);
        }

        /// <summary>
        /// Called when the updating process finished.
        /// Will notify all spawned code windows to refresh their content accordingly.
        /// </summary>
        private void NFPValuesUpdateFinished(bool refreshNFPRegions) {
            
            updateNFPValues = false;
            Debug.Log("NFP value update process finished after " +
                (Time.time - updateTime) + " seconds... " +
                (refreshNFPRegions ? "Updating spawned windows." : ""));
            
            // update spawned code windows
            if (refreshNFPRegions) {

                // refresh just the representation of spawned regions (color, scale, ...)
                FileSpawner fspawner = FileSpawner.GetInstance();
                if (fspawner == null) { return; }
                //fspawner.RefreshSpawnedFileRegionValues(ARProperty.TYPE.NFP);
                fspawner.RefreshSpawnedFileRegions(ARProperty.TYPE.NFP); // we must re-create them now (bc. of validation)
            }

            updateFinished = true;
            SetProgress(1, true);
        }


        /// <summary>
        /// Takes care of updating the UI feedback while updating NFP values.<para/>
        /// Searched for available UISpawner instances in this the components of this gameobject.
        /// </summary>
        private void UpdateUIFeedback() {

            foreach (UISpawner s in gameObject.GetComponents<UISpawner>()) {
                s.NFPUpdateProcessChanged(GetProgress(), updateFailure);
            }
        }

    }
}

