using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Mappings;
using VRVis.Mappings.Methods;
using VRVis.RegionProperties;
using VRVis.Spawner.Regions;
using VRVis.Utilities;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Takes care of spawning the texture of code city elements.<para/>
    /// Spawning is triggered by specific events like city spawning or NFP update.<para/>
    /// Needs to be attached to a gameobject that contains a CodeCity component.<para/>
    /// Created: 04.09.2019 (Leon H.)<para/>
    /// Updated: 04.09.2019
    /// </summary>
    [RequireComponent(typeof(CodeCityV1))]
    public class CodeCityTextureGenerator : MonoBehaviour {
        
        [Tooltip("How many textures to generate per frame")]
        public int generateTexturesPerFrame = 10;

        [Tooltip("Color applied to regions in case the feature model is invalid")]
        public Color regionDefaultColor = Color.gray;

        private CodeCityV1 codeCity;
        
        private Coroutine textureGenCoroutine = null;
        private bool textureGenRunning = false;


        private void Start() {

            codeCity = GetComponent<CodeCityV1>();
            if (!codeCity) { Debug.LogError("Missing CodeCityV1 component!"); return; }

            // register for event callbacks
            codeCity.citySpawnedEvent.AddListener(CitySpawnedEvent);

            StructureLoaderUpdater slu = ApplicationLoader.GetInstance().GetStructureLoaderUpdater();
            slu.nfpUpdateStartedEvent.AddListener(NFPUpdateStartedEvent);
            slu.nfpUpdateFinishedEvent.AddListener(NFPUpdateFinishedEvent);

            ApplicationLoader.GetApplicationSettings().nfpRelativityChangedEvent.AddListener(NFPUpdateFinishedEvent);
            ApplicationLoader.GetApplicationSettings().selectedNFPChangedEvent.AddListener(NFPUpdateFinishedEvent);

            // run initial update
            if (codeCity.IsCitySpawned()) { GenerateElementTextures(generateTexturesPerFrame); }
        }


        // ---------------------------------------------------------------------------------------
        // Event callbacks.

        private void CitySpawnedEvent() {
            GenerateElementTextures(generateTexturesPerFrame);
        }

        private void NFPUpdateStartedEvent() {
            // ToDo: remove texture and set base color
        }

        private void NFPUpdateFinishedEvent() {
            GenerateElementTextures(generateTexturesPerFrame);
        }


        // ---------------------------------------------------------------------------------------
        // Generating and spawning the textures of city elements.

        /// <summary>
        /// Triggers the texture generation process for the city elements.<para/>
        /// Also stops a process that is currently/still running.
        /// </summary>
        private void GenerateElementTextures(int elementsPerFrame) {

            if (textureGenCoroutine != null && textureGenRunning) {
                Debug.LogWarning("Stopping unfinished texture generation!");
                StopCoroutine(textureGenCoroutine);
            }

            if (!isActiveAndEnabled) { return; } // don't run if component is disabled
            textureGenCoroutine = StartCoroutine(ElementTextureGenerator(elementsPerFrame));
        }

        private IEnumerator ElementTextureGenerator(int perFrame) {

            Debug.Log("Starting city texture generation... (" + perFrame + " per frame)");
            textureGenRunning = true;
            int generated = 0; // to control how many per frame
            int skipped = 0;
            int failed = 0;
            int total = 0;

            foreach (CodeCityElement element in codeCity.GetSpawnedElements()) {

                total++;
                if (element == null) { skipped++; continue;}
                CodeCityTexture cct = element.GetComponent<CodeCityTexture>();
                if (!cct) { skipped++; continue; }

                if (!cct.GenerateTexture(GetRegionTexInfo(element.GetSNode()))) { failed++; continue; }
                cct.ApplyTexture();

                generated++;
                if (generated >= perFrame) {
                    generated = 0;
                    yield return new WaitForEndOfFrame();
                }
            }

            textureGenRunning = false;
            Debug.Log("Code city texture generation finished " + 
                "(total: " + total + ", skipped: " + skipped + ", failed: " + failed + ")");
        }

        /// <summary>
        /// Get the currently relevant regions for this node
        /// together with settings like their colors based on the current NFP settings.<para/>
        /// The functionality of this method should always be in sync with the RegionSpawner and RegionModifier.
        /// </summary>
        private List<CodeCityTexture.Info> GetRegionTexInfo(SNode node) {
            
            // ---------------------------------------------------------------------------------------- [1]
            // Part of RegionSpawner functionality.

            // use file path and property selection to only spawn regions of interest
            List<CodeCityTexture.Info> regionsToSpawn = new List<CodeCityTexture.Info>();

            CodeFile codeFile = ApplicationLoader.GetInstance().GetStructureLoader().GetFile(node);
            if (codeFile == null) { return regionsToSpawn; }

            // get the currently selected property to show
            string activeNFP = ApplicationLoader.GetInstance().GetAppSettings().GetSelectedNFP().ToLower();

            // use loader to find regions of that file
            RegionLoader regionLoader = ApplicationLoader.GetInstance().GetRegionLoader();
            foreach (Region region in regionLoader.GetFileRegions(node.GetPath())) {
                if (region.HasProperty(ARProperty.TYPE.NFP, activeNFP)) {
                    regionsToSpawn.Add(new CodeCityTexture.Info(region, regionDefaultColor));
                }
            }


            // ---------------------------------------------------------------------------------------- [2]
            // Part of RegionModifier functionality.

            ValueMappingsLoader vml = ApplicationLoader.GetInstance().GetMappingsLoader();
            NFPSetting setting = vml.GetNFPSetting(activeNFP);

            foreach (CodeCityTexture.Info info in regionsToSpawn) {

                // get the according nfp property
                RProperty_NFP nfpProp = info.region.GetProperty(ARProperty.TYPE.NFP, activeNFP) as RProperty_NFP;
                if (nfpProp == null || !nfpProp.GotValue()) { continue; }

                // get color method and method min/max                
                MinMaxValue minMax = RegionModifier.GetMinMaxValues(nfpProp.GetName(), codeFile, setting.GetMinMaxValue());
                AColorMethod colMethod;
                if (ApplicationLoader.GetApplicationSettings().ComparisonMode) {
                    colMethod = setting.GetMinMaxColorMethod(Settings.ApplicationSettings.NFP_VIS.CODE_CITY, minMax.GetMinValue(), minMax.GetMaxValue(), regionDefaultColor);
                }
                else {
                    colMethod = setting.GetColorMethod(Settings.ApplicationSettings.NFP_VIS.CODE_CITY);
                }

                if (minMax == null) {
                    Debug.LogError("Failed to apply NFP mapping (" + nfpProp.GetName() +  ") - Missing min/max!");
                    continue;
                }

                // get absolute value and crop it to the bounds
                float absValue = minMax.CropToBounds(nfpProp.GetValue());

                // apply relative color
                float valuePercentage = minMax.GetRangePercentage(absValue);
                Color regionColor = colMethod.Evaluate(valuePercentage);
                info.color = regionColor;
            }

            return regionsToSpawn;
        }

    }
}
