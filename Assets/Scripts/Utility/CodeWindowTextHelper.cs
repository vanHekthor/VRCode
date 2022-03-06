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

}
