using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;

public class CodeWindowTextHelper {

    public static string GetTextByLineIndex(CodeFile codeFile, int lineIdx) {

        var textElements = codeFile.GetReferences().GetTextElements();

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

}
