using System;
using System.IO;
using System.Linq;
using UnityEngine;
using VRVis.IO.Structure;
using VRVis.Spawner.File;

public class CodeWindowTextHelper {

    public static string GetTextByLineIndex(CodeFileReferences codeFileInstance, int lineIdx) {

        var textElements = codeFileInstance.GetTextElements();

         int lineCounter = 0;
         foreach (var element in textElements) {
            string codeString = element.textComponent.text;
            var codeLines = codeString.Replace("\r", "").Split('\n');

            int relativeIdx = lineIdx - lineCounter;

            if (codeLines.Length <= relativeIdx) {
                lineCounter += codeLines.Length;
                continue;
            }

            return codeLines.Length > relativeIdx ? codeLines[relativeIdx] : null;
         }

        return null;
    }

    /// <summary>
    /// Loads the highlighted source code from the file.<para/>
    /// Returns true on success and false otherwise.
    /// </summary>
    public static string LoadLineFromFile(SNode fileNode, int lineIdx) {

        // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.-ctor?view=netframework-4.7.2
        string filePath = fileNode.GetFullPath();
        FileInfo fi = new FileInfo(filePath);

        if (!fi.Exists) {
            Debug.LogError("File does not exist! (" + filePath + ")");
            return null;
        }

        Debug.Log("Reading file contents...");
        // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.opentext?view=netframework-4.7.2
        using (StreamReader sr = fi.OpenText()) {
            // local and temp. information
            string curLine = "";
            string output = "";
            // string sourceCode = "";
            int linesRead = 0;

            while ((curLine = sr.ReadLine()) != null) {

                // update counts and source code
                // sourceCode += curLine + "\n";
                linesRead++;
                if (linesRead == lineIdx) {
                    // check for '{' at the end
                    if (IsDeclarationEndLine(curLine)) {
                        return curLine.Trim();
                    }

                    // count leading whitespace
                    int leadingSpaceCount = curLine.TakeWhile(Char.IsWhiteSpace).Count();
                    output = curLine.TrimStart();

                    //01public
                    //0123wegwj

                    int declarationLines = 1;
                    while ((curLine = sr.ReadLine()) != null) {
                        declarationLines++;
                        if (!curLine.Equals(string.Empty)) {
                            output += '\n' + curLine?.Substring(leadingSpaceCount).TrimEnd();
                        }
                        else {
                            output += '\n';
                        }

                        if (IsDeclarationEndLine(curLine) || declarationLines > 10) {
                            return output;
                        }
                    }

                    return output;
                }


            }
        }

        return null;
    }

    private static bool IsDeclarationEndLine(string line) {
        if (line.LastIndexOf('{') == -1) {
            return false;
        }
        return line.TrimEnd().Substring(line.LastIndexOf('{')).Equals("{</color>");
    }

}
