using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using VRVis.IO;

namespace VRVis.Utilities.Glimps {

    public class GlimpsChopsLoader {

        const string CHOPS_PATH = "glimps_chops/";

        public Dictionary<string, List<GlimpsChop>> Chops { get; private set; }

        public Dictionary<string, List<GlimpsChop>> LoadChops() {
            string mainPath = ApplicationLoader.GetInstance().mainPath;
            string jsonString = File.ReadAllText(mainPath + CHOPS_PATH + "influencing_lines.json");

            var chops = JsonConvert.DeserializeObject<Dictionary<string, List<GlimpsChop>>>(jsonString);

            var chopsWithModifiedKeys = new Dictionary<string, List<GlimpsChop>>();

            foreach (var chop in chops) {
                chopsWithModifiedKeys.Add("src/main/java/" + chop.Key, chop.Value);
            }

            Chops = chopsWithModifiedKeys;

            return Chops;
        }
    }
}