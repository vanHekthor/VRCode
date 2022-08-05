using Newtonsoft.Json;

namespace VRVis.Utilities.Glimps {

    public partial class GlimpsChop {
        [JsonProperty("startLineNumber")]
        public long StartLineNumber { get; set; }

        [JsonProperty("endLineNumber")]
        public long EndLineNumber { get; set; }
    }
}
