using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.RegionProperties;
using VRVis.Settings;

namespace VRVis.Spawner.Regions {

    /// <summary>
    /// Will be attached to each GameObject that represents a region.<para/>
    /// It holds basic information about this region like:<para/>
    /// 
    /// - CodeFile (this region belongs to)<para/>
    /// - Property Type<para/>
    /// - Property Name<para/>
    /// 
    /// Additional:<para/>
    /// - nfpVisType (type of nfp visualization)<para/>
    /// 
    /// This script will be used when trying to apply coloring and scaling
    /// depending on the current user selection and the resulting observed values.<para/>
    /// It it also used by the Visual Property Methods to get access to the code file and to min/max values.
    /// </summary>
    public class RegionGameObject : MonoBehaviour {

        public string propertyName; // just to show name in editor

        // code file and region this region game object belongs to
        private CodeFile codeFile = null;
        private Region region = null;
	    private ARProperty property = null;

        // type of visualization this region object belongs to (e.g. code marking)
        private ApplicationSettings.NFP_VIS nfpVisType = ApplicationSettings.NFP_VIS.NONE;

        public void SetInfo(CodeFile codeFile, Region region, ARProperty property) {
            this.codeFile = codeFile;
            this.region = region;
            this.property = property;
            propertyName = property.GetName();
        }
        
        public CodeFile GetCodeFile() { return codeFile; }
        //public void SetCodeFile(CodeFile codeFile) { this.codeFile = codeFile; }

        public Region GetRegion() { return region; }
        //public void SetRegion(Region region) { this.region = region;}

        public ARProperty GetProperty() { return property; }

        public ApplicationSettings.NFP_VIS GetNFPVisType() { return nfpVisType; }
        public void SetNFPVisType(ApplicationSettings.NFP_VIS visType) { nfpVisType = visType; }

    }
}
