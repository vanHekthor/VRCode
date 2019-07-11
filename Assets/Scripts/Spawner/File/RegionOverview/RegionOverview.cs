using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRVis.IO;

namespace VRVis.Spawner.File.Overview {

    /// <summary>
    /// Generates a texture that provides an overview of the file content
    /// as well as an overview about the regions and their colors.<para/>
    /// It also takes care of adding this texture to an according sprite image.
    /// </summary>
    public class RegionOverview : MonoBehaviour {

        [Tooltip("References of CodeFile this overview belongs to")]
        public CodeFileReferences fileRefs;

        [Tooltip("Image to apply texture")]
        public Image image;

        [Tooltip("Aspect ratio of the texture")]
        public Vector2 textureAspectRatio = new Vector2(1, 2);

        [Tooltip("Resolution of the texture in pixels (e.g. aspect ratio of 16:9 and res. of 80 results in HD")]
        public uint textureResolution = 100;

        public Color invisibleColor = Color.white;

        // this texture always stays the same and can be reused
        private Texture2D codeTexture = null;



        // !!!
        // TODO: generate code texture without waiting for regions to be spawned!



	    void Start () {
		
            // ensure that file references are assigned
            if (!fileRefs) {
                Debug.LogError("Got no file references!");
                enabled = false;
                return;
            }

            // register the region spawned event in the RegionSpawner accordingly
            ASpawner s = ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
            if (s != null && s is FileSpawner) {
                RegionSpawner rs = (RegionSpawner) s.GetSpawner((uint) FileSpawner.SpawnerList.RegionSpawner);
                rs.onNFPRegionsSpawned.AddListener(RegionsSpawned);
                Debug.Log("RegionOverview registered event in RegionSpawner.", this);
            }
            else {
                Debug.LogError("RegionSpawner or FileSpawner not found!", this);
                enabled = false;
                return;
            }
	    }
 

        /// <summary>
        /// Called as a callback method when regions where spawned.
        /// </summary>
        /// <param name="file">The affected code file</param>
        private void RegionsSpawned(CodeFile file, int amount) {

            if (file == null || file != fileRefs.GetCodeFile()) { return; }

            if (codeTexture == null) {
                int texWidth = (int) (textureAspectRatio.x * textureResolution);
                int texHeight = (int) (textureAspectRatio.y * textureResolution);
                codeTexture = GenerateCodeTexture(texWidth, texHeight);
            }

            if (amount > 0) {
                Debug.Log("RegionOverview: regions spawned!", this);
                // ToDo: regenerate the region texture, overlay on code texture and apply result
            }
            else {
                Rect rect = new Rect(Vector2.zero, new Vector2(codeTexture.width, codeTexture.height));
                // ToDo: set border to avoid sprite warning
                Sprite sprite = Sprite.Create(codeTexture, rect, new Vector2(0.5f, 0.5f));
                if (image) { image.sprite = sprite; }
            }
        }


        /// <summary>
        /// Calculate the patterns of the lines.<para/>
        /// These will be used for image creation.<para/>
        /// It uses the spawned text elements and information from TextMesh Pro.<para/>
        /// Therefore, it is necessary that this information is already completely calculated.
        /// </summary>
        private List<List<int>> CalculateLinePatterns() {

            // ToDo: do this only once bc. text should not change during runtime
            // Contains lists with following information (always 2 entries belong together and rep. 1 char):
            // - [0] = pattern made out of 4 quads in binary (min = 0, max = 11111111 (1 byte))
            // - [1] = color? converted to integer RGB (each 0 - 255 => 8 bits per color component)
            /*
             * The [0] place is always a byte (8 bit), telling about the pattern.
             * 
             * # | # | #
             * # | # | #
             * # | # | #
             * 
             * 6 | 7 | 8
             * 3 | 4 | 5
             * 0 | 1 | 2
             * 
             * e.g. the pattern to represent underscore "_" (e = empty):
             * 
             * e | e | e
             * e | e | e
             * # | # | #
             * 
             * number would be: 00000111
             */
            List<List<int>> linePatterns = new List<List<int>>();
            
            int charInfoStart = 0;

            foreach (TMP_TextInfo ti in fileRefs.GetTextElements()) {
                foreach (TMP_LineInfo li in ti.lineInfo) {
                    
                    List<int> linePattern = new List<int>(li.characterCount * 2);

                    for (int i = charInfoStart; i < charInfoStart + li.characterCount; i++) {

                        TMP_CharacterInfo ci = ti.characterInfo[i];

                        // for now, only distinguish between visible, capital or small
                        int pattern = 0;
                        if (ci.isVisible) {

                            // for other patterns, use: System.Convert.ToInt32("11111111", 2);
                            if (char.IsUpper(ci.character)) { pattern = 255; }
                            else { pattern = System.Convert.ToInt32("00111111", 2); }
                        }

                        // encode color
                        int color = 0;
                        color |= ci.color.r;
                        color |= ci.color.g << 8;
                        color |= ci.color.b << 16;

                        // add both entries for this character
                        linePattern.Add(pattern);
                        linePattern.Add(color);
                    }

                    linePatterns.Add(linePattern);
                }
            }

            return linePatterns;
        }


        /// <summary>
        /// Generates the overview texture for the pure code and returns it.<para/>
        /// Does it like e.g. Sublime, an overflow in character count per line will be cut of.<para/>
        /// But instead of scrolling as soon as we have too many lines, we simply squeeze it.<para/>
        /// This needs to be improved in future versions!
        /// </summary>
        private Texture2D GenerateCodeTexture(int width, int height) {
            
            Texture2D tex = new Texture2D(width, height) {
                filterMode = FilterMode.Point
            };

            // calculate text/code patterns
            List<List<int>> linePatterns = CalculateLinePatterns();

            // render text/code patterns
            float lineHeight = 3;
            float maxHeight = lineHeight * linePatterns.Count;
            if (maxHeight > height) { lineHeight = height / maxHeight; }
            if (lineHeight < 3) { Debug.LogWarning("Overview problem! Line height is less than one pixel!", this); }

            float curPos_y = 0;
            int x_increment = lineHeight < 3 ? 1 : 3;
            int previous_pp_y = -1; // previous pixel position

            // ToDo: invert generation of pixels to start at top and end at bottom!

            foreach (List<int> linePattern in linePatterns) {

                int pixelPos_x = 0;
                int pixelPos_y = Mathf.RoundToInt(curPos_y);
                if (pixelPos_y >= height) { pixelPos_y = height-1; }

                // avoid overlapping and just skip the lines
                if (pixelPos_y == previous_pp_y) { continue; }
                previous_pp_y = pixelPos_y;

                for (int i = 0; i < linePattern.Count; i += 2) {

                    // get pattern and color for this character
                    int pattern = linePattern[i];
                    int color = linePattern[i+1];

                    // decode color
                    Color c = new Color(
                        (color & 255) / 255f,
                        (color >> 8 & 255) / 255f,
                        (color >> 16 & 255) / 255f
                    );

                    // draw as many pixels as possible
                    // (e.g. if available size (height) is less than 3, use single pixels)
                    if (lineHeight < 3) {

                        // check if any pixel is visible
                        if (pattern > 0) { tex.SetPixel(pixelPos_x, pixelPos_y, c); }
                    }
                    else {

                        // create the block representing a character on the texture
                        for (int y = pixelPos_y; y < pixelPos_y + 3; y++) {
                            for (int x = pixelPos_x; x < pixelPos_x + 3; x++) {
                                if ((pattern & 1) == 1) { tex.SetPixel(x, y, c); }
                                else { tex.SetPixel(x, y, invisibleColor); }
                                pattern = pattern >> 1;
                            }
                        }
                    }

                    pixelPos_x += x_increment;
                }

                curPos_y += lineHeight;
            }

            // apply changed pixels
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates the overview texture for the NFP regions and returns it.
        /// </summary>
        private Texture2D GenerateRegionTexture(int width, int height) {

            Texture2D tex = new Texture2D(width, height);

            // ToDo: calculate nfp regions
            // ToDo: render nfp regions (simulate alpha value for overlapping)

            // apply changed pixels
            tex.Apply();
            return tex;
        }

    }
}
