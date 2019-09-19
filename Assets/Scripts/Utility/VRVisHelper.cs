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

namespace VRVis.Utilities {

    /// <summary>
    /// Helper methods to access specific information
    /// stored by the framework easily.<para/>
    /// For instance, getting all regions of a file.<para/>
    /// Created: 19.09.2019 (Leon H.)<para/>
    /// Updated: 19.09.2019
    /// </summary>
    public class VRVisHelper {

        // -----------------------------------------------------------------------------------
        // CodeFile retrieval

        /// <summary>Get a code file for a given node. Returns null if not found.</summary>
        public static CodeFile GetCodeFile(SNode node) {
            return ApplicationLoader.GetInstance().GetStructureLoader().GetFile(node);
        }


        // -----------------------------------------------------------------------------------
        // Region Loading (Helps to retrieve regions of a file)

        /// <summary>Get a list of regions for a specific file.</summary>
        /// <param name="node">The structure node representing the file</param>
        /// <param name="nfp">For which type of NFP to get the regions (e.g. "performance"), pass empty or null for all</param>
        public static List<Region> GetFileRegions(CodeFile codeFile, string nfp = null) {

            // --------------------------------------------------------------------------
            // Part of RegionSpawner functionality.

            // use file path and property selection to only spawn regions of interest
            List<Region> regions = new List<Region>();
            if (codeFile == null) { return regions; }

            // use loader to find regions of that file
            RegionLoader regionLoader = ApplicationLoader.GetInstance().GetRegionLoader();
            foreach (Region region in regionLoader.GetFileRegions(codeFile.GetNode().GetPath())) {

                if (nfp != null && nfp.Length > 0) {
                    if (region.HasProperty(ARProperty.TYPE.NFP, nfp)) { regions.Add(region); }
                    continue;
                }

                regions.Add(region);
            }

            return regions;
        }

        /// <summary>
        /// Get the colors for each region of the passed list (regions are all of a single file),
        /// based on their current value and the setting of the according nfp.
        /// </summary>
        /// <param name="codeFile">Code file to which the regions belong to</param>
        /// <param name="regions">A list of regions to find colors for</param>
        /// <param name="nfp">The non-functional property name to use</param>
        /// <param name="defaultColor">Color that is used if the region has no value or similar</param>
        /// <returns>The list of colors or an empty list if, e.g., the value mappings loading was not successful.</returns>
        public static List<Color> GetRegionColors(CodeFile codeFile, List<Region> regions, string nfp, Color defaultColor) {

            // --------------------------------------------------------------------------
            // Part of RegionModifier functionality.

            List<Color> colors = new List<Color>();
            ValueMappingsLoader vml = ApplicationLoader.GetInstance().GetMappingsLoader();
            if (!vml.LoadedSuccessful()) { return colors; }
            NFPSetting setting = vml.GetNFPSetting(nfp);

            foreach (Region region in regions) {

                // get the according nfp property
                RProperty_NFP nfpProp = region.GetProperty(ARProperty.TYPE.NFP, nfp) as RProperty_NFP;
                if (nfpProp == null || !nfpProp.GotValue()) { colors.Add(defaultColor); continue; }

                // get color method and method min/max
                AColorMethod colMethod = setting.GetColorMethod(Settings.ApplicationSettings.NFP_VIS.NONE);
                MinMaxValue minMax = RegionModifier.GetMinMaxValues(nfpProp.GetName(), codeFile, setting.GetMinMaxValue());

                if (minMax == null) {
                    Debug.LogError("Failed to apply NFP mapping (" + nfpProp.GetName() +  ") - Missing min/max!");
                    colors.Add(defaultColor); continue;
                }

                // get absolute value and crop it to the bounds
                float absValue = minMax.CropToBounds(nfpProp.GetValue());

                // apply relative color
                float valuePercentage = minMax.GetRangePercentage(absValue);
                Color regionColor = colMethod.Evaluate(valuePercentage);
                colors.Add(regionColor);
            }

            return colors;
        }

    }
}
