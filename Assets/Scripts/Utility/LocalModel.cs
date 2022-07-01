using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using VRVis.IO.Features;

namespace VRVis.Utilities.Glimps {

    public class LocalModel {
        [JsonProperty("models")]
        public List<Model> Models { get; set; }

        public Dictionary<string, double> EvaluateConfigurations(Configuration config1, Configuration config2) {
            Dictionary<string, double> evaluation = new Dictionary<string, double>();

            var defaultModel = Models.Find(model => model.Name == "default");

            double performanceValue = 0.0;
            defaultModel.Terms.ForEach(term =>
            {
                // Check if the option change defined in the local model can be found between config1 and config2
                bool definedChangeOfOptionsHappened = term.Options.TrueForAll(option =>
                {
                    if (!CheckIfOptionExists(config1, config2, option.OptionName)) return false;

                    if (config1.GetOptionType(option.OptionName) == Configuration.OptionType.select) {
                        return CheckSelectOptionChange(config1, config2, option);
                    }

                    if (config1.GetOptionType(option.OptionName) == Configuration.OptionType.binary) {
                        return CheckBinaryOptionChange(config1, config2, option);
                    }

                    if (config1.GetOptionType(option.OptionName) == Configuration.OptionType.numeric) {
                        return CheckNumericOptionChange(config1, config2, option);
                    }

                    return false;
                });

                if (definedChangeOfOptionsHappened) {
                    performanceValue += term.Time;
                }
            });

            evaluation.Add("performance", performanceValue);

            return evaluation;
        }

        private bool CheckIfOptionExists(Configuration config1, Configuration config2, string optionName) {
            return config1.HasOption(optionName) && config2.HasOption(optionName);
        }

        private bool CheckSelectOptionChange(Configuration config1, Configuration config2, Option option) {
            string optionFromKey = $"{option.OptionName}_{option.From.ToUpper()}";
            string optionToKey = $"{option.OptionName}_{option.To.ToUpper()}";

            bool config1OptionFromValue = config1.GetBinaryOptionValue(optionFromKey);
            bool config1OptionToValue = config1.GetBinaryOptionValue(optionToKey);

            bool config2OptionFromValue = config2.GetBinaryOptionValue(optionFromKey);
            bool config2OptionToValue = config2.GetBinaryOptionValue(optionToKey);

            bool optionChangedAsDefined = config1OptionFromValue == true && config1OptionToValue == false
                && config2OptionFromValue == false && config2OptionToValue == true;
           
            return optionChangedAsDefined;
        }

        private bool CheckBinaryOptionChange(Configuration config1, Configuration config2, Option option) {
            string config1OptionValueString = config1.GetOptionValue(option.OptionName, out bool optionExists) == 1 ? "true" : "false";
            if (option.From != config1OptionValueString) {
                return false;
            }

            string config2OptionValueString = config2.GetOptionValue(option.OptionName, out optionExists) == 1 ? "true" : "false";
            if (option.To != config2OptionValueString) {
                return false;
            }

            return true;
        }

        private bool CheckNumericOptionChange(Configuration config1, Configuration config2, Option option) {
            string config1OptionValueString = config1.GetOptionValue(option.OptionName, out bool optionExists).ToString(new CultureInfo("en-US"));
            if (option.From != config1OptionValueString) {
                return false;
            }

            string config2OptionValueString = config2.GetOptionValue(option.OptionName, out optionExists).ToString(new CultureInfo("en-US"));
            if (option.To != config2OptionValueString) {
                return false;
            }

            return true;
        }
    }

    public partial class Model {
        [JsonProperty("terms")]
        public List<Term> Terms { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class Term {
        [JsonProperty("options")]
        public List<Option> Options { get; set; }

        [JsonProperty("time")]
        public double Time { get; set; }
    }

    public partial class Option {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("option")]
        public string OptionName { get; set; }
    }
}
