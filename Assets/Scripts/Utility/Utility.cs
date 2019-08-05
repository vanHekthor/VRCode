using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using System.Globalization;

namespace VRVis.Utilities {

    /// <summary>
    /// Helper methods that can for instance,
    /// convert data types and load file contents.
    /// </summary>
    public class Utility {

        /// <summary>
        /// Ensure that all paths have the same format (e.g. using "/" instead of "\").<para/>
        /// It also turns the path into lowercase.
        /// </summary>
        public static string GetFormattedPath(string path) {
            return path.Replace("\\", "/").ToLower();
        }

        /// <summary>
        /// Check if the path leads to an existing directory.<para/>
        /// Returns a DirectoryInfo instance or null if the path does not exist.
        /// </summary>
        public static DirectoryInfo GetDirectoryInfo(string dirPath) {
            DirectoryInfo dirInf = new DirectoryInfo(dirPath);
            if (dirInf.Exists) { return dirInf; }
            return null;
        }

        /// <summary>
        /// Loads all the text from a file and returns it.
        /// Returns null on errors!
        /// </summary>
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
        /// Tries to convert an object to float.<para/>
        /// Returns true if successful.<para/>
        /// The "out" floatValue will be 0 if formatting fails!
        /// </summary>
        public static bool ObjectToFloat(object value, out float floatValue) {
            try {
                float converted = Convert.ToSingle(value);
                floatValue = converted;
                return true;
            }
            catch (FormatException fe) {
                Debug.LogWarning("Converting to float failed: " + value + " (" + fe.Message + ")");
                floatValue = 0;
                return false;
            }
        }

        /// <summary>
        /// Tries to convert string to float and returns false on failure.<para/>
        /// Takes as separator the dot (".") sign -> CultureInfo.InvariantCulture!<para/>
        /// This is required for .NET-FW 4.x to work properly bc. otherwise 0.02 would end up being 2.
        /// </summary>
        public static bool StrToFloat(string str, out float value, bool logOnFailure = false) {
            if (!float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) {
                if (logOnFailure) { Debug.LogError("Failed to convert string (" + str + ") to float: " + value); }
                return false;
            }
            return true;
        }

        /// <summary>Try to convert string to float. Returns 0 on failure.</summary>
        public static float StrToFloat(string str, bool logOnFailure = false) {
            float val = 0;
            if (!StrToFloat(str, out val, logOnFailure)) { return 0;}
            return val;
        }

        /// <summary>Converts a string like "1, 2, 3" to a Vector3 and returns it.</summary>
        public static Vector3 Vector3FromString(string vecStr, char separator) {

            string[] split = vecStr.Split(separator);
            if (split.Length == 0) { return Vector3.zero; }

            float x = 0;
            if (split.Length > 0) { x = StrToFloat(split[0]); }

            float y = 0;
            if (split.Length > 1) { y = StrToFloat(split[1]); } 
            
            float z = 0; 
            if (split.Length > 2) { z = StrToFloat(split[2]); } 

            return new Vector3(x, y, z);
        }

        /// <summary>Converts a string like "1, 2, 3, 4" to a Vector4 and returns it.</summary>
        public static Vector4 Vector4FromString(string vecStr, char separator) {

            string[] split = vecStr.Split(separator);
            if (split.Length == 0) { return Vector4.zero; }

            float x = 0;
            if (split.Length > 0) { x = StrToFloat(split[0]); }

            float y = 0;
            if (split.Length > 1) { y = StrToFloat(split[1]); } 
            
            float z = 0; 
            if (split.Length > 2) { z = StrToFloat(split[2]); } 

            float w = 0;
            if (split.Length > 3) { w = StrToFloat(split[3]); } 

            return new Vector4(x, y, z, w);
        }

        /// <summary>Converts a string like "0.5, 0.1, 0.3" to a Color and returns it.</summary>
        public static Color ColorFromString(string vecStr, char separator, float defaultAlpha) {

            Vector4 vec = Vector4FromString(vecStr, separator);
            vec.x = vec.x > 1 ? 1 : vec.x < 0 ? 0 : vec.x;
            vec.y = vec.y > 1 ? 1 : vec.y < 0 ? 0 : vec.y;
            vec.z = vec.z > 1 ? 1 : vec.z < 0 ? 0 : vec.z;
            vec.w = vec.w > 1 ? 1 : vec.w < 0 ? 0 : vec.w;

            // check if the user entered an alpha value or not and rect accordingly
            string[] split = vecStr.Split(separator);
            if (split.Length < 4) { vec.w = defaultAlpha; }
            return new Color(vec.x, vec.y, vec.z, vec.w);
        }

        /// <summary>
        /// Tries to find the UI Image component
        /// and change its color value to the passed one.
        /// Returns true on success, false if the component is missing.
        /// </summary>
        public static bool ChangeImageColorTo(GameObject gObj, Color color) {
            Image img = gObj.GetComponent<Image>();
            if (!img) { return false; }
            img.color = color;
            return true;
        }

        /// <summary>
        /// Parses an input string like "5-30" to an int array "[5, 6, ... , 29, 30]" and returns it.<para/>
        /// Simply returns an empty array if parsing fails.<para/>
        /// If the second number is greater than the first, the second will be the same as the first.
        /// </summary>
        /// <param name="separator">The separator to use (default is "-")</param>
        /// <param name="onlyPositive">Each negative value will be converted to zero (i.e. the first value will always be positive)</param>
        public static int[] StringFromToArray(string inputString, char separator = '-', bool onlyPositive = false) {

            int[] resultArray = new int[0];
            string[] split = inputString.Split(new char[]{separator}, StringSplitOptions.RemoveEmptyEntries);
            
            if (split.Length == 2) {
                
                int first = 0;
                int second = 0;
                int.TryParse(split[0], out first);
                int.TryParse(split[1], out second);

                if (onlyPositive && first < 0) { first = 0; }
                if (second < first) { second = first; }
                
                resultArray = new int[second - first + 1];
                for (int i = 0; i < resultArray.Length; i++) { resultArray[i] = first + i; }
            }

            return resultArray;
        }

        /// <summary>Convert the given degree to radians.</summary>
        public static float DegreeToRadians(float degree) { return degree / 180f * Mathf.PI; }

        /// <summary>Converts float to str and ensures to use dot instead of comma.</summary>
        public static string ToStr(object f) {
            return Convert.ToString(f, new CultureInfo("en"));
        }


        // ---------------------------------------------------------------------------------------------------------------
        // I/O Related

        /// <summary>
        /// Returns the lines of code or 0 if the file does not exist.<para/>
        /// Implemented with respect to the optimization approach of:<para/>
        /// https://nima-ara-blog.azurewebsites.net/counting-lines-of-a-text-file/
        /// </summary>
        public static long GetLOC(string file) {

            if (!File.Exists(file)) { return 0; }

            char LF = '\n'; // line feed
            char CR = '\r'; // carriage return

            FileStream stream = null;
            try { stream = new FileStream(file, FileMode.Open); }
            catch (Exception ex) {
                Debug.LogError("Failed to open file: " + file);
                Debug.LogError(ex.StackTrace);
                return 0;
            }

            byte[] buff = new byte[1024 * 1024];
            int bytesRead = 0;
            char prev = (char) 0;
            bool pending = false;
            long cnt = 0;

            while ((bytesRead = stream.Read(buff, 0, buff.Length)) > 0) {

                for (int i = 0; i < bytesRead; i++) {

                    char cur = (char) buff[i];
                    
                    // MAC: \r
                    // UNIX: \n
                    if (cur == CR || cur == LF) {

                        // WIN case: \r\n
                        // (we already detected \r and cnt+1 so skip this char)
                        if (prev == CR && cur == LF) { continue; }

                        pending = false;
                        cnt++;
                    }
                    else if (!pending) { pending = true; }

                    prev = cur;
                }
            }

            if (pending) { cnt++; }
            return cnt;
        }

        /// <summary>Get the size of a file in bytes.</summary>
        public static long GetFileSizeBytes(string file) {

            if (!File.Exists(file)) { return 0; }
            
            FileInfo fi = null;
            try { fi = new FileInfo(file); }
            catch (Exception e) {
                Debug.LogWarning("Failed to get size of file: " + file + "!");
                Debug.LogWarning(e.StackTrace);
                return 0;
            }

            return fi.Length;
        }

    }
}
