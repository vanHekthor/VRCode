using Newtonsoft.Json;

namespace VRVis.Utilities.Glimps {

    public partial class GlimpsChop {
        [JsonProperty("startLineNumber")]
        public int StartLineNumber { get; set; }

        [JsonProperty("endLineNumber")]
        public int EndLineNumber { get; set; }
    }
}
