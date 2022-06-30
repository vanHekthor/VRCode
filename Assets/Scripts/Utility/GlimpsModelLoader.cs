using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.IO;

namespace VRVis.Utilities.Glimps {

    public class GlimpsModelLoader {

        const string LOCAL_MODEL_PATH = "glimps_models/";

        public static LocalModel loadModel(string fullQualifiedPath) {
            string mainPath = ApplicationLoader.GetInstance().mainPath;
            string jsonString = File.ReadAllText(mainPath + LOCAL_MODEL_PATH + fullQualifiedPath + ".json");

            var model = JsonConvert.DeserializeObject<LocalModel>(jsonString);
            return model;
        }        
    }    
}