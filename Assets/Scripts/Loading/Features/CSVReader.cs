using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace VRVis.IO {

    /// <summary>
    /// Reads file and adds each line to a list of strings. 
    /// </summary>
    public class CSVReader {

        public static List<string> ReadFile(string filePath) {
            StreamReader reader = new StreamReader(filePath);
            bool eof = false;

            List<string> csvLines = new List<string>();

            while (!eof) {
                string line = reader.ReadLine();
                if (line == null) {
                    eof = true;
                    break;
                }

                csvLines.Add(line);
            }

            return csvLines;
        }
    }
}