using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Attached to the code city elements.<para/>
    /// Is called by the code city component when a texture should be generated.<para/>
    /// Created: 04.09.2019 (Leon H.)<para/>
    /// Updated: 06.09.2019
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class CodeCityTexture : MonoBehaviour {

        [Tooltip("Show areas of the file where regions are missing (shows where in the file the regions are)")]
        public bool showMissingRegions = true;

        [Tooltip("If \"showMissingRegions\" is disabled, enable this to separate regions using a line")]
        public bool showSepLine = true;

        [Tooltip("Width of the separation line")]
        public int sepLineWidth = 2;

        [Tooltip("Color of the separation line")]
        public Color sepLineColor = Color.black;

        private Color baseColor;
        private Texture2D tex;
        private bool generated = false;
        private CodeCityElement cce;
        private MeshFilter mf;

        // spacing to store base color in texture
        private readonly int base_spacing = 1;

        /// <summary>Region texture information.</summary>
        public class Info {

            public Region region;
            public Color color;

            public Info(Region region, Color color) {
                this.region = region;
                this.color = color;
            }
        }


        private void Awake() {
            mf = GetComponent<MeshFilter>();
        }


        /// <summary>Tells if the texture has been generated.</summary>
        public bool IsGenerated() { return generated; }


        /// <summary>
        /// Generates the texture and tells if this was successful.<para/>
        /// 
        /// CURRENT TEXTURE LOOKS LIKE THIS:<para/>
        /// [1] (pixel 1 = base color of the cube)<para/>
        /// [2]<para/>
        /// [...] (rest of the pixels represent the region texture)<para/>
        /// [n]<para/>
        /// </summary>
	    public bool GenerateTexture(List<Info> regionTexInfo) {

            generated = false;
            cce = GetComponent<CodeCityElement>();
            if (!cce) { return false; }

            // store base color of city element to use it in the texture
            Color curColor = GetComponent<Renderer>().material.color;
            if (curColor != Color.white) { baseColor = curColor; }

            // get the total line count
            CodeFile codeFile = ApplicationLoader.GetInstance().GetStructureLoader().GetFile(cce.GetSNode());
            if (codeFile == null) { return false; }
            
            long LOC = 0;
            int sepLine = (showSepLine ? sepLineWidth : 0);
            if (!showMissingRegions) { regionTexInfo.ForEach(info => LOC += info.region.GetLOCs() + sepLine); }
            else { LOC = codeFile.GetLineCountQuick(); }
            Debug.Log(codeFile.GetNode().GetName() + ": " + LOC);

            int width = 1; // pixel
            int height = base_spacing + (int) LOC;

            tex = new Texture2D(width, height, TextureFormat.RGB24, false) {
                filterMode = FilterMode.Point,
            };

            // color texture with base color
            Color[] colors = new Color[tex.width * tex.height];
            for (int i = 0; i < colors.Length; i++) { colors[i] = baseColor; }

            // add regions
            if (LOC > 0 && showMissingRegions) {
                
                foreach (Info info in regionTexInfo) {
                    foreach (Region.Section s in info.region.GetSections()) {

                        int from = Mathf.RoundToInt((float) s.start / LOC * (tex.height - base_spacing)) + base_spacing;
                        int to = Mathf.RoundToInt((float) s.end / LOC * (tex.height - base_spacing)) + base_spacing;
                        if (from > to || from < 0) { continue; }
                        if (to > tex.height) { to = tex.height; }

                        for (int y = from-1; y < to; y++) {
                            for (int x = 0; x < tex.width; x++) {
                                colors[y * tex.width + x] = info.color;
                            }
                        }
                    }
                }
            }
            else if (LOC > 0 && !showMissingRegions) {

                // sort sections based on their "start" value
                List<KeyValuePair<Region.Section, int>> sections = new List<KeyValuePair<Region.Section, int>>();

                for (int i = 0; i < regionTexInfo.Count; i++) {

                    Info info = regionTexInfo[i];
                    foreach (Region.Section s in info.region.GetSections()) {

                        // very simple insertion sort (ToDo: improve if required)
                        int index = -1;
                        for (int k = 0; k < sections.Count; k++) {
                            if (s.start < sections[k].Key.start) {
                                index = k;
                                break;
                            }
                        }

                        if (index > -1) { sections.Insert(index, new KeyValuePair<Region.Section, int>(s, i)); }
                        else { sections.Add(new KeyValuePair<Region.Section, int>(s, i)); }
                    }
                }

                System.Text.StringBuilder strb = new System.Text.StringBuilder("[");
                foreach (var s in sections) {
                    strb.Append(s.Key.start);
                    strb.Append(",");
                }
                strb.Append("]");
                Debug.Log("Sort: " + strb.ToString());

                float curPos = 0;
                foreach (var se in sections) {

                    float start = curPos + 1;
                    float end = start + se.Key.GetLOCs();
                    curPos = end + sepLine;

                    int from = Mathf.RoundToInt(start / LOC * (tex.height - base_spacing)) + base_spacing;
                    int to = Mathf.RoundToInt(end / LOC * (tex.height - base_spacing)) + base_spacing;
                    if (from > to || from < 0) { continue; }
                    if (to > tex.height) { to = tex.height; }
                    
                    // draw region color and separation line
                    for (int y = from-1; y < to + sepLine; y++) {
                        for (int x = 0; x < tex.width; x++) {
                            colors[y * tex.width + x] = y < to ? regionTexInfo[se.Value].color : sepLineColor;
                        }
                    }
                }
            }

            // assign pixels
            tex.SetPixels(colors);
            tex.Apply();

            generated = true;
            return true;
        }


        /// <summary>Applies the generated texture to this element.</summary>
        public void ApplyTexture() {

            if (!generated) { return; }

            // set UV coordinates accordingly
            SetUVCoordinates();

            // set color of element material to white and apply texture
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = Color.white;
            renderer.material.mainTexture = tex;
        }


        /// <summary>Applies the UV coordinates to the MeshFilter.</summary>
        private void SetUVCoordinates() {

            // UV coordinates sources:
            // https://answers.unity.com/questions/63088/uv-coordinates-of-primitive-cube.html
            // https://answers.unity.com/questions/542787/change-texture-of-cube-sides.html
            // https://answers.unity.com/questions/306959/uv-mapping.html

            // by assigning all others to (0,0), we make sure that the block is colored in its default color
            List<Vector2> uv = new List<Vector2>();
            for (int i = 0; i < mf.mesh.uv.Length; i++) { uv.Add(Vector2.zero); }


            // FRONT    2    3    0    1

            // BACK    6    7   10   11

            // relative spacing from base color
            float st = (float) base_spacing / tex.height;

            uv[6] = new Vector2(0, 1f);
            uv[7] = new Vector2(1, 1f);
            uv[10] = new Vector2(0, st);
            uv[11] = new Vector2(1, st);

            // LEFT   19   17   16   18

            // RIGHT   23   21   20   22
            uv[23] = new Vector2(1, 1f);
            uv[21] = new Vector2(1, st);
            uv[20] = new Vector2(0, 1f);
            uv[22] = new Vector2(0, st);

            // TOP    4    5    8    9

            // BOTTOM   15   13   12   14

            // apply UV coordinates
            mf.mesh.SetUVs(0, uv);
        }

    }
}
