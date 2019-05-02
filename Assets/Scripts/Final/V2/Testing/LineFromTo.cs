using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.Testing.CodeWindow {

    /**
     * This script is to test how well we can detect the currently shown lines
     * using the ScrollRect as well as the information about line heights
     * that we can get from Text Mesh Pro.
     * The test script only works for a single TextMeshPro instance.
     * For multiple ones, the height, spacing and other text properties are assumed to be the same
     * so that the calculation is still correct.
     * The only point to consider is the error that is created by "stacking" multiple text instances!
     * 
     * Findings:
     * - max_advance and length are almost equal but length is always a bit smaller than max_advance
     * - max_advance seems to be the "correct"/rendered width of the line
     * - "height" of the lines is always the same
     * - "height" does not contain the possible line spacing - so consider that into calculations!
     * - width for each line does not represent the length of it, use max_advance to get this value
     * - width is the maximum line width of the whole text mesh object
     * - there are often pointless and empty entries after the last line using the "lineInfo" array, so use lineCount instead
     * - ascender, descender and baseline change depending on line spacing
     * - using the current ascender, the next ascender and the height values can yield spacing information
     *   (e.g.: spacing = next_a - current_a - height)
     *   (so two lines are all it takes to calculate this information)
     */
    public class LineFromTo : MonoBehaviour {

        public TextMeshProUGUI textMesh;
        public ScrollRect scrollRect;
        public Scrollbar verticalScrollbar;
        public bool invertScrollPercentage = true;

        [Tooltip("Tells if line information was already printed once to console")]
        public bool printedLineInformation = false;

        [Space]
        [Header("Information Output")]
        public float showingFrom = 0;
        public int showingFromLine = 0;
        public float showingTo = 0;
        public int showingToLine = 0;
        public int lines = 0;
        public float scrollPercentageFrom = 0;
        public float scrollPercentageTo = 0;
        public float scrollPercentage = 0;
        public float scrollPercentageVerticalScrollbar = 0;
        public float spacing = 0;
        public float generalHeight = 0;
        public float finalHeight = 0;
        public float maxLineWidth = 0;
	

	    void Update () {
		
            // update as soon as the required components are available
            if (textMesh && scrollRect && verticalScrollbar) {
                lines = textMesh.textInfo.lineCount;
                scrollPercentage = scrollRect.verticalNormalizedPosition;
                scrollPercentageVerticalScrollbar = verticalScrollbar.value;

                if (invertScrollPercentage) {
                    scrollPercentage = 1 - scrollPercentage;
                    scrollPercentageVerticalScrollbar = 1 - scrollPercentageVerticalScrollbar;
                }

                // reset output information if no lines exist
                if (lines <= 0) {
                    showingFromLine = 0;
                    showingToLine = 0;
                    spacing = 0;
                    generalHeight = 0;
                    finalHeight = 0;
                    maxLineWidth = 0;
                    scrollPercentageFrom = 0;
                    scrollPercentageTo = 0;
                    showingFrom = 0;
                    showingTo = 0;
                    return;
                }
                
                // print line information once
                TMP_LineInfo[] lineInfo = textMesh.textInfo.lineInfo;
                if (!printedLineInformation) {
                    printedLineInformation = true;
                    for (int i = 0; i < lineInfo.Length; i++) {
                        Debug.Log("Line Info " + i + ": height=" + lineInfo[i].lineHeight +
                            ", ascender=" + lineInfo[i].ascender + ", descender=" + lineInfo[i].descender + ", baseline=" + lineInfo[i].baseline +
                            ", width=" + lineInfo[i].width + ", max_advance=" + lineInfo[i].maxAdvance +
                            ", length=" + lineInfo[i].length
                        );
                    }
                }

                // get general line dimension information
                if (lines < 2 || lineInfo.Length < lines) { return; }
                generalHeight = lineInfo[0].lineHeight;
                finalHeight = Mathf.Abs(lineInfo[1].ascender);
                spacing = finalHeight - generalHeight;
                maxLineWidth = lineInfo[0].width;

                // get information about which lines we currently see in the code window
                float adjustedScroll = scrollPercentageVerticalScrollbar * (1 - verticalScrollbar.size);
                scrollPercentageFrom = adjustedScroll;
                scrollPercentageFrom = scrollPercentageFrom < 0 ? 0 : scrollPercentageFrom > 1 ? 1 : scrollPercentageFrom;

                scrollPercentageTo = scrollPercentageFrom + verticalScrollbar.size;
                scrollPercentageTo = scrollPercentageTo < 0 ? 0 : scrollPercentageTo > 1 ? 1 : scrollPercentageTo;

                showingFrom = scrollPercentageFrom * (float) (lines + 1) + 1;
                showingFromLine = Mathf.RoundToInt(showingFrom); // this shows if half of the line is visible
                //showingFromLine = Mathf.FloorToInt(showingFromLineDec); // this shows the next line if the previous is no longer visible

                showingTo = scrollPercentageTo * (float) lines;
                showingToLine = Mathf.RoundToInt(showingTo);
            }

	    }

    }
}
