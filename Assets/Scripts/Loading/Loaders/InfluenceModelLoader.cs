using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VRVis.IO.Features;

namespace VRVis.IO {

    /// <summary>
    /// Creates an Influence Model object by parsing the csv string array from the CSVReader.  
    /// </summary>
    public class InfluenceModelLoader : FileLoader {

        private const int IdColumn = 0;

        public InfluenceModel Model { get; private set; }
        public IEnumerable<string> XmlOptionList { get; private set; }

        /// <summary>
        /// Constructor for initializing the file path and the configuration options list.
        /// </summary>
        /// <param name="filePath">Path to the influence model in csv format</param>
        /// <param name="xmlOptionList">List of software configuration options from the variability model xml file</param>
        public InfluenceModelLoader(string filePath, IEnumerable<string> xmlOptionList)
            : base(filePath) {
            this.XmlOptionList = xmlOptionList;
        }

        /// <summary>
        /// Start loading the influence model.
        /// </summary>
        /// <returns>True when loading was successful, otherwise false.</returns>
        public override bool Load() {
            Model = ParseInfluenceModel(CSVReader.ReadFile(GetFilePath()));

            if (Model != null) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses the array of strings in csv format and creates the Influence Model.
        /// </summary>
        /// <param name="csvLines">Lines of the csv file containing the influence model</param>
        /// <returns></returns>
        private InfluenceModel ParseInfluenceModel(List<string> csvLines) {
            HashSet<Influence> influences = new HashSet<Influence>();

            if (csvLines.Count < 2) {
                Debug.LogError("Could not read model! Too few lines: " + GetFilePath() + " is not a valid influence model!");
                return null;
            }

            for (int i = 0; i < csvLines.Count - 1; i++) {
                if (SplitLine(csvLines[i + 1]).Length != SplitLine(csvLines[i]).Length) {
                    Debug.LogError(GetFilePath() + " is not a valid influence model! Varying amount of values per line!");
                    return null;
                }
            }

            Dictionary<int, string> optionDict = MapOptions(csvLines[0], XmlOptionList);
            Dictionary<int, string> propertyDict = MapProperties(csvLines[0]);
            HashSet<string> properties = new HashSet<string>(propertyDict.Values);

            HashSet<string> optionsOutOfXML = new HashSet<string>(XmlOptionList);
            HashSet<string> optionsOutOfCSVHeadline = new HashSet<string>(optionDict.Values);

            if (!optionsOutOfXML.SetEquals(optionsOutOfCSVHeadline)) {
                Debug.LogError("CSV influence model and XML constraints model do not have the same set of config options!");
                return null;
            }

            return new InfluenceModel(properties, CreateInfluenceDict(csvLines, optionDict, propertyDict));
        }

        private Dictionary<int, string> MapOptions(string headLine, IEnumerable<string> xmlOptionList) {
            string[] wordsInHeadLine = SplitLine(headLine);
            Dictionary<int, string> optionDict = new Dictionary<int, string>();

            for (int i = 1; i < wordsInHeadLine.Length; i++) {
                if (!wordsInHeadLine[i].Contains(";")) {
                    string optionName = wordsInHeadLine[i];

                    if (!xmlOptionList.Contains(optionName)) {
                        Debug.LogError("CSV influence model and XML constraints model do not have the same set of config options!");
                    }

                    optionDict.Add(i, optionName);
                }
            }

            return optionDict;
        }

        private Dictionary<int, string> MapProperties(string headLine) {
            string[] wordsInHeadLine = SplitLine(headLine);
            Dictionary<int, string> propertyDict = new Dictionary<int, string>();

            for (int i = 1; i < wordsInHeadLine.Length; i++) {
                if (wordsInHeadLine[i].Contains(";")) {
                    string[] propertyAttributes = SplitPropertyString(wordsInHeadLine[i]);

                    if (propertyAttributes.Length != 3) {
                        Debug.LogError("Incorrect property header format! Header has to contain exactly 3 Attributes: '<property name>(<unit>;<optimization direction>)'");
                    }

                    if (!(propertyAttributes[2].Equals("<") || propertyAttributes[2].Equals(">"))) {
                        Debug.LogError("Incorrect property optimization direction symbol! Either '<' or '>' needed!");
                    }

                    propertyDict.Add(i, propertyAttributes[0]);
                }
            }
            return propertyDict;
        }


        /// <summary>
        /// Creates a dictionary for easy access to region specific influences using a region id. That becomes useful when calculating the effect a region has
        /// on the different properties of a software system given a specific configuration.   
        /// </summary>
        /// <param name="csvLines">Lines of the influence model csv file</param>
        /// <param name="optionDict">Dictionary mapping the column index to an option</param>
        /// <param name="propertyDict">Dictionary mapping the column index to a property</param>
        /// <returns></returns>
        private Dictionary<string, HashSet<Influence>> CreateInfluenceDict(List<string> csvLines, Dictionary<int, string> optionDict, Dictionary<int, string> propertyDict) {
            Dictionary<string, HashSet<Influence>> influenceDict = new Dictionary<string, HashSet<Influence>>();

            foreach (string line in csvLines.Skip(1)) {
                string[] lineCells = SplitLine(line);

                if (influenceDict.ContainsKey(lineCells[IdColumn])) {
                    influenceDict[lineCells[IdColumn]].Add(CreateInfluence(lineCells, optionDict, propertyDict));
                }
                else {
                    HashSet<Influence> regionInfluences = new HashSet<Influence>
                    {
                        CreateInfluence(lineCells, optionDict, propertyDict)
                    };
                    influenceDict.Add(lineCells[IdColumn], regionInfluences);
                }
            }

            return influenceDict;
        }

        /// <summary>
        /// Creates an influence out of a csv-line by going through each cell of the line. The created influence contains the options
        /// that need to be turned on in order for the influence to exist and it contains the effect it has on each of the properties.
        /// </summary>
        /// <param name="lineCells">Lines of the influence model csv file</param>
        /// <param name="optionDict">Dictionary mapping the column index to an option</param>
        /// <param name="propertyDict">Dictionary mapping the column index to a property</param>
        /// <returns></returns>
        private Influence CreateInfluence(string[] lineCells, Dictionary<int, string> optionDict, Dictionary<int, string> propertyDict) {
            HashSet<string> relatedOptions = new HashSet<string>();
            Dictionary<string, double> effectOnProperties = new Dictionary<string, double>();

            // Iterating through the cells. The first cell containing the region ID is skipped.
            for (int i = 1; i < lineCells.Length; i++) {

                // should this cell contain an option value?
                if (optionDict.ContainsKey(i)) {
                    if (!(lineCells[i].Equals("0") || lineCells[i].Equals("1"))) {
                        Debug.LogError("Influence Model contains incorrect option values (neither 1 nor 0)!");
                    }

                    // add option to related options
                    if (lineCells[i].Equals("1")) {
                        relatedOptions.Add(optionDict[i]);
                    }
                }

                if (propertyDict.ContainsKey(i)) {
                    if (!double.TryParse(lineCells[i], System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out double effect)) {
                        Debug.LogError("Influence Model contains incorrect property values (not double)!");
                    }
                    effectOnProperties.Add(propertyDict[i], effect);
                }
            }

            return new Influence(relatedOptions, effectOnProperties);
        }

        private static string[] SplitLine(string line) {
            string[] splitted = line.Split(',');
            for (int i = 0; i < splitted.Length; i++) {
                splitted[i] = splitted[i].Trim();
            }
            return splitted;
        }

        private static string[] SplitPropertyString(string propertyString) {
            string[] propertyAttributes = propertyString.Split(new char[] { ')', '(', ';', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < propertyAttributes.Length; i++) {
                propertyAttributes[i] = propertyAttributes[i].Trim();
            }
            return propertyAttributes;
        }


    }
}