using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;

namespace Siro {

    /**
     * Helper methods to convert data types and so on.
     */
    public class Utility {

        /**
         * Loads all the text from a file and returns it.
         * Returns null on errors!
         */
        public static string LoadTextFromFile(string filePath) {

            // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.-ctor?view=netframework-4.7.2
            FileInfo fi = new FileInfo(filePath);

            if (!fi.Exists) {
                Debug.LogError("File does not exist! (" + filePath + ")");
                return null;
            }

            Debug.Log("Reading file contents...");
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.opentext?view=netframework-4.7.2
            string textOut = "";
            StreamReader sr = fi.OpenText();
            textOut = sr.ReadToEnd();
            sr.Close();
            return textOut;
        }

        /// <summary>
        /// Tries to convert string to float and returns false on failure.<para/>
        /// Takes as separator the dot (".") sign -> CultureInfo.InvariantCulture!<para/>
        /// This is required for .NET-FW 4.x to work properly bc. otherwise 0.02 would end up being 2.
        /// </summary>
        public static bool StrToFloat(string str, out float value) {
            if (!float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) {
                Debug.LogError("Failed to convert string to float: " + value);
                return false;
            }
            return true;
        }

        /// <summary>Try to convert string to float. Returns 0 on failure.</summary>
        public static float StrToFloat(string str) {
            float val = 0;
            if (!StrToFloat(str, out val)) { return 0;}
            return val;
        }

        /**
         * Converts a string like "1, 2, 3" to a Vector3 and returns it.
         */
        public static Vector3 VectorFromString(string vecStr, char separator) {

            string[] split = vecStr.Split(separator);
            if (split.Length != 3) {
                Debug.LogError("Invalid amount of inputs! (excepted 3, got" + split.Length + ")");
                return Vector3.zero;
            }

            float x = StrToFloat(split[0]);
            float y = StrToFloat(split[1]);
            float z = StrToFloat(split[2]);

            return new Vector3(x, y, z);
        }

    }

}