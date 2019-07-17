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
    /// It also takes care of adding this texture to an according sprite image.<para/>
    /// 
    /// Add this component itself to the according section of the Code Window prefab
    /// that deals with the overview of the file content.<para/>
    /// Then apply the correct settings and assign important references.<para/>
    /// Ensure to update this script if FileSpawner or RegionSpawner access changes.<para/>
    /// The script will automatically register to events of both of these spawners.<para/>
    /// 
    /// Created: 12.07.2019 (by Leon H.)<para/>
    /// Last Updated: 12.07.2019
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

        [Tooltip("Component to control scroll handles accordingly")]
        public LinkScrollHandles scrollHandlesLink;

        [Tooltip("Color for invisible character parts")]
        public Color invisibleColor = new Color(0, 0, 0, 0);

        // this texture always stays the same and can be reused
        private Texture2D codeTexture = null;

        // in percentage how many pixels are occupied on the y-axis
        private float pixelsOccupiedPercentage = 0;

        // if code regions are applied to the last shown texture
        private bool regionsApplied = false;

        // An index to store already converted byte => int patterns
        // because it takes long for unity to convert the string to int using Convert.
        // Using this index, we store already converted bit strings for a faster lookup on next call.
        private static Dictionary<string, int> bitStringIndex = new Dictionary<string, int>();
        private static Dictionary<char, int> patternIndex = new Dictionary<char, int>();
        private static readonly int fullPattern = BitStringToInt("111111111");


        // !!!
        // TODO: Put generation of line patterns and code texture in one procedure for less memory consumption.
        // TODO: Use a curoutine to generate the texture for minimum performance impact!


	    void Start () {
		
            // ensure that file references are assigned
            if (!fileRefs) {
                Debug.LogError("Got no file references!");
                enabled = false;
                return;
            }

            // warn about missing reference
            if (!scrollHandlesLink) {
                Debug.LogWarning("You may have forgotten to assign the scroll handles link!");
            }

            // register the file spawned event in the FileSpawner accordingly
            // + register the region spawned event in the RegionSpawner accordingly
            ASpawner s = ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
            if (s != null && s is FileSpawner) {

                FileSpawner fs = (FileSpawner) s;
                fs.onFileSpawned.AddListener(FileSpawnedEvent);
                string what = "FileSpawner";
                
                ASpawner s2 = fs.GetSpawner((uint) FileSpawner.SpawnerList.RegionSpawner);
                if (s2 != null && s2 is RegionSpawner) {
                    RegionSpawner rs = (RegionSpawner) s2;
                    rs.onNFPRegionsSpawned.AddListener(RegionsSpawnedEvent);
                    what += " and RegionSpawner";
                }
                else { Debug.LogError("RegionSpawner not found!", this); }

                Debug.Log("RegionOverview registered event in " + what, this);
            }
            else {
                Debug.LogError("FileSpawner not found!", this);
                enabled = false;
                return;
            }
	    }
 


        // -----------------------------------------------------------------------------------------
        // Event Listener Section

        /// <summary>
        /// Called as a callback method when a file was spawned.<para/>
        /// Called after the layout calculation of TextMesh Pro is finished.
        /// </summary>
        private void FileSpawnedEvent(CodeFile file) {

            if (file == null || file != fileRefs.GetCodeFile()) { return; }
            if (codeTexture == null) { GenerateCodeTexture(); }
        }


        /// <summary>
        /// Called as a callback method when regions where spawned.
        /// </summary>
        /// <param name="file">The affected code file</param>
        private void RegionsSpawnedEvent(CodeFile file, int amount) {

            if (file == null || file != fileRefs.GetCodeFile()) { return; }
            if (codeTexture == null) { GenerateCodeTexture(); }

            if (amount > 0) {
                
                // ToDo: regenerate the region texture, overlay on code texture and apply result
                // ToDo: is it posible & faster to assign 2 textures and overlay them in the shader?

                // don't forget:
                // ApplyTextureToUIImage(compositeTexture)
                // regionsApplied = true;
            }
            else if (regionsApplied) {
                
                // replace the currently shown texture if the last included regions
                ApplyTextureToUIImage(codeTexture);
                regionsApplied = false;
            }
        }



        // -----------------------------------------------------------------------------------------
        // Texture Generation Section

        /// <summary>
        /// Generates the texture of the content (code) of a file.<para/>
        /// Should only be done once because it is a heavy operation.
        /// </summary>
        /// <param name="apply">If it should be applied to the UI image or not.</param>
        private void GenerateCodeTexture(bool apply = true) {

            // generate the code texture
            int texWidth = (int) (textureAspectRatio.x * textureResolution);
            int texHeight = (int) (textureAspectRatio.y * textureResolution);
            codeTexture = GenerateCodeTextureV2(texWidth, texHeight);

            if (apply) { ApplyTextureToUIImage(codeTexture); }
        }

        /// <summary>
        /// Applies a texture to the UI image.
        /// </summary>
        private void ApplyTextureToUIImage(Texture2D texture) {

            if (!image) {
                Debug.LogError("Could not apply texture! Image component not assigned.", this);
                return;
            }

            // apply to UI image
            float ppu = 50; // pixels per unit
            Rect rect = new Rect(Vector2.zero, new Vector2(texture.width, texture.height));
            Sprite sprite = Sprite.Create(
                texture, rect, new Vector2(0.5f, 0.5f), // tex, rect and pivot
                ppu, 0, SpriteMeshType.FullRect, // ppu, extrude and mesh type
                new Vector4(0, 0, texture.width, texture.height) // sprite border
            );
            
            image.sprite = sprite;
            image.color = Color.white;

            // apply correct scaling properties to the scrollbar
            if (!scrollHandlesLink) { Debug.LogWarning("Missing scroll handles component!"); }
            else { scrollHandlesLink.SetScrollbar2ScaleY(pixelsOccupiedPercentage); }
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
            // - [0] = pattern made out of 4 quads in binary (min = 0, max = 111111111 (9 bit))
            // - [1] = color, converted to integer RGB (each 0 - 255 => 8 bits per color component)
            List<List<int>> linePatterns = new List<List<int>>();
            
            int charInfoStart = 0;

            foreach (TMP_TextInfo ti in fileRefs.GetTextElements()) {
                for (int n = 0; n < ti.lineCount; n++) {
                    
                    TMP_LineInfo li = ti.lineInfo[n];
                    List<int> linePattern = new List<int>(li.characterCount * 2);
                    
                    //for (int i = charInfoStart; i < charInfoStart + li.characterCount - 1; i++) {
                    for (int i = li.firstCharacterIndex; i < li.lastCharacterIndex; i++) {

                        TMP_CharacterInfo ci = ti.characterInfo[i];

                        // for now, only distinguish between visible, capital or small
                        int pattern = GetCharacterPattern(ci);

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
                    charInfoStart += li.characterCount;
                }
            }

            return linePatterns;
        }


        /// <summary>
        /// Generates the character pattern based on the character information.<para/>
        /// 
        /// PATTERN DESCRIPTION:<para/>
        /// 
        /// The pattern is always (9 bit).<para/>
        /// _ <para/>
        /// # | # | #<para/>
        /// # | # | #<para/>
        /// # | # | #<para/>
        /// _ <para/>
        /// 6 | 7 | 8<para/>
        /// 3 | 4 | 5<para/>
        /// 0 | 1 | 2<para/>
        /// _ <para/>
        /// e.g. the pattern to represent underscore "_" (e = empty):<para/>
        /// 
        /// e | e | e<para/>
        /// e | e | e<para/>
        /// # | # | #<para/>
        /// 
        /// => pattern would be: 000000111
        /// </summary>
        public static int GetCharacterPattern(TMP_CharacterInfo ci) {

            if (!ci.isVisible) { return 0; }
            int pattern = 0;

            char c = ci.character;
            if (patternIndex.ContainsKey(c)) { return patternIndex[c]; }

            // for other patterns, use: System.Convert.ToInt32("111111111", 2);
            int patfull = fullPattern;
            if (char.IsLetterOrDigit(c)) {
                if (char.IsUpper(c)) { pattern = patfull; }
                else { pattern = BitStringToInt("000111111"); }
            }
            else if (c == '{') { pattern = BitStringToInt("110011110"); }
            else if (c == '(') { pattern = BitStringToInt("011001011"); }
            else if (c == '|') { pattern = BitStringToInt("010010010"); }
            else if (c == '-') { pattern = BitStringToInt("000111000"); }
            else if (c == '+') { pattern = BitStringToInt("010111010"); }
            else if (c == ',' || c == '.') { pattern = BitStringToInt("000000010"); }
            else { pattern = patfull; }

            // store in index
            patternIndex[c] = pattern;
            return pattern;
        }

        /// <summary>
        /// Converts the string of bits to its integer representation.<para/>
        /// Uses an index for faster lookup in case a bit string is passed multiple times.
        /// </summary>
        private static int BitStringToInt(string bitString) {

            if (bitStringIndex.ContainsKey(bitString)) { return bitStringIndex[bitString]; }
            int num = System.Convert.ToInt32(bitString, 2);
            bitStringIndex[bitString] = num;
            return num;
        }


        /// <summary>
        /// Generates the overview texture for the pure code and returns it.<para/>
        /// Does it like e.g. Sublime, an overflow in character count per line will be cut of.<para/>
        /// But instead of scrolling as soon as we have too many lines, we simply squeeze it.<para/>
        /// This needs to be improved in future versions!
        /// </summary>
        private Texture2D GenerateCodeTexture(int width, int height) {
            
            Debug.LogWarning("Code texture generation...");

            Texture2D tex = new Texture2D(width, height) {
                filterMode = FilterMode.Point,
                alphaIsTransparency = true
            };

            // make texture transparent
            /*
            Color[] transparentLine = new Color[width];
            for (int x_ = 0; x_ < width; x_++) { transparentLine[x_] = new Color(1, 1, 1, 0); }
            for (int y_ = 0; y_ < height; y_++) { tex.SetPixels(0, y_, width, 1, transparentLine); }
            */
            int pixels_total = tex.width * tex.height;
            Color[] colors = new Color[pixels_total];
            Color c_transparent = new Color(1, 1, 1, 0);
            for (int y_ = 0; y_ < width; y_++) {
                for (int x_ = 0; x_ < height; x_++) {
                    colors[y_ * width + x_] = c_transparent;
                }
            }

            // calculate text/code patterns
            List<List<int>> linePatterns = CalculateLinePatterns();

            // render text/code patterns
            float lineHeight = 3;
            float maxHeight = lineHeight * linePatterns.Count;

            if (maxHeight > height) {
                
                // try using a single pixel instead
                lineHeight = 1;
                maxHeight = linePatterns.Count;

                if (maxHeight > height) { lineHeight = height / maxHeight; }
            }
            if (lineHeight < 1) {
                Debug.LogWarning("Overview problem! Line height is less than one pixel!", this);
                Debug.LogWarning("Lines: " + linePatterns.Count);
            }

            float curPos_y = 0;
            int x_increment = lineHeight < 3 ? 1 : 3;
            int previous_pp_y = -1; // previous pixel position

            foreach (List<int> linePattern in linePatterns) {

                int pixelPos_x = 0;
                int pixelPos_y = Mathf.RoundToInt(curPos_y);
                if (pixelPos_y >= height) { pixelPos_y = height-1; }
                curPos_y += lineHeight;

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

                        // check if any pixel is visible (uses inverted texture generation)
                        if (pattern > 0) { tex.SetPixel(pixelPos_x, height - pixelPos_y, c); }
                    }
                    else {

                        // create the block representing a character on the texture
                        // (uses inverted texture generation)
                        /*
                        int y_s = height - pixelPos_y - 3;
                        for (int y = y_s; y < y_s + 3; y++) {
                            for (int x = pixelPos_x; x < pixelPos_x + 3; x++) {
                                if ((pattern & 1) == 1) { tex.SetPixel(x, y, c); }
                                else { tex.SetPixel(x, y, invisibleColor); }
                                pattern = pattern >> 1;
                            }
                        }
                        */
                        int y_s = height - pixelPos_y - 3;
                        for (int y = y_s; y < y_s + 3; y++) {
                            for (int x = pixelPos_x; x < pixelPos_x + 3; x++) {
                                int pos = y * width + x;
                                if ((pattern & 1) == 1) { colors[pos] = c; }
                                else { colors[pos] = invisibleColor; }
                                pattern = pattern >> 1;
                            }
                        }
                    }

                    pixelPos_x += x_increment;
                    if (pixelPos_x >= width) { break; }
                }
            }

            // how many pixels on the y-axis are occupied at the end
            // (this value is then used to calculate the overview scrollbar height)
            pixelsOccupiedPercentage = (previous_pp_y + lineHeight) / height;
            if (pixelsOccupiedPercentage > 1) { pixelsOccupiedPercentage = 1; }
            else if (pixelsOccupiedPercentage < 0) { pixelsOccupiedPercentage = 0; }

            // apply all pixel colors at once instead of single calls & apply
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }


        /// <summary>
        /// Generates the overview texture for the pure code and returns it.<para/>
        /// Does it like e.g. Sublime, an overflow in character count per line will be cut of.<para/>
        /// But instead of scrolling as soon as we have too many lines, we simply squeeze it.<para/>
        /// This needs to be improved in future versions!<para/>
        /// Notable in V2 of this method:<para/>
        /// - pattern generation and texture generation are no longer separated and happen in one step (no speedup)<para/>
        /// - colors of pixels are first stored in an array and at the end applied using setPixels() (significant speedup!)<para/>
        /// Evaluation: Significant speedup (about half the time of previous version) [17.07.2019 - Leon]
        /// </summary>
        private Texture2D GenerateCodeTextureV2(int width, int height) {

            Debug.LogWarning("Code texture generation...");

            Texture2D tex = new Texture2D(width, height) {
                filterMode = FilterMode.Point,
                alphaIsTransparency = true
            };

            // make initial texture transparent
            int pixels_total = tex.width * tex.height;
            Color[] colors = new Color[pixels_total];
            Color c_transparent = new Color(1, 1, 1, 0);
            for (int y_ = 0; y_ < width; y_++) {
                for (int x_ = 0; x_ < height; x_++) {
                    colors[y_ * width + x_] = c_transparent;
                }
            }

            // get amount of lines
            int lines = 0;
            foreach (TMP_TextInfo ti in fileRefs.GetTextElements()) { lines += ti.lineCount; }

            // render text/code patterns
            float lineHeight = 3;
            float maxHeight = lineHeight * lines;

            if (maxHeight > height) {
                
                // try using a single pixel instead
                lineHeight = 1;
                maxHeight = lines;

                if (maxHeight > height) { lineHeight = height / maxHeight; }
            }
            if (lineHeight < 1) {
                Debug.LogWarning("Overview problem! Line height is less than one pixel!", this);
                Debug.LogWarning("Lines: " + lines);
            }

            float curPos_y = 0;
            int x_increment = lineHeight < 3 ? 1 : 3;
            int previous_pp_y = -1; // previous pixel position

            foreach (TMP_TextInfo ti in fileRefs.GetTextElements()) {
                for (int n = 0; n < ti.lineCount; n++) {
                    
                    TMP_LineInfo li = ti.lineInfo[n];

                    int pixelPos_x = 0;
                    int pixelPos_y = Mathf.RoundToInt(curPos_y);
                    if (pixelPos_y >= height) { pixelPos_y = height-1; }
                    curPos_y += lineHeight;

                    // avoid overlapping and just skip the lines
                    if (pixelPos_y == previous_pp_y) { continue; }
                    previous_pp_y = pixelPos_y;

                    // iterate over all characters
                    for (int i = li.firstCharacterIndex; i < li.lastCharacterIndex; i++) {

                        TMP_CharacterInfo ci = ti.characterInfo[i];

                        // for now, only distinguish between visible, capital or small
                        int pattern = GetCharacterPattern(ci);

                        // draw as many pixels as possible
                        // (e.g. if available size (height) is less than 3, use single pixels)
                        if (lineHeight < 3) {

                            // check if any pixel is visible (uses inverted texture generation)
                            if (pattern > 0) { tex.SetPixel(pixelPos_x, height - pixelPos_y, ci.color); }
                        }
                        else {

                            // create the block representing a character on the texture
                            // (uses inverted texture generation)
                            int y_s = height - pixelPos_y - 3;
                            for (int y = y_s; y < y_s + 3; y++) {
                                for (int x = pixelPos_x; x < pixelPos_x + 3; x++) {
                                    int pos = y * width + x;
                                    if ((pattern & 1) == 1) { colors[pos] = ci.color; }
                                    else { colors[pos] = invisibleColor; }
                                    pattern = pattern >> 1;
                                }
                            }
                        }

                        pixelPos_x += x_increment;
                        if (pixelPos_x >= width) { break; }
                    }
                }
            }

            // how many pixels on the y-axis are occupied at the end
            // (this value is then used to calculate the overview scrollbar height)
            pixelsOccupiedPercentage = (previous_pp_y + lineHeight) / height;
            if (pixelsOccupiedPercentage > 1) { pixelsOccupiedPercentage = 1; }
            else if (pixelsOccupiedPercentage < 0) { pixelsOccupiedPercentage = 0; }

            // apply all pixel colors at once instead of single calls & apply
            tex.SetPixels(colors);
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
