using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Attached to a code city element.<para/>
    /// Is called by the code city component when a texture should be generated.<para/>
    /// Created: 04.09.2019 (Leon H.)<para/>
    /// Updated: 04.09.2019
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class CodeCityTexture : MonoBehaviour {

        private Color baseColor;
        private Texture2D tex;
        private bool generated = false;
        private CodeCityElement cce;
        private MeshFilter mf;

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


        /// <summary>Generates the texture and tells if this was successful.</summary>
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
            long LOC = codeFile.GetLineCountQuick();

            //float ratio = transform.localScale.x / transform.localScale.y;
            int width = 4; // pixel
            tex = new Texture2D(width, (int) LOC, TextureFormat.RGB24, false) {
                filterMode = FilterMode.Point,
            };

            // color texture with base color
            Color[] colors = new Color[tex.width * tex.height];
            for (int i = 0; i < colors.Length; i++) { colors[i] = baseColor; }

            // add regions
            if (LOC > 0) {
                int left_spacing = 1;
                foreach (Info info in regionTexInfo) {
                    foreach (Region.Section s in info.region.GetSections()) {
                        
                        int from = Mathf.RoundToInt((float) s.start / LOC * tex.height);
                        int to = Mathf.RoundToInt((float) s.end / LOC * tex.height);
                        if (from > to || from < 0) { continue; }
                        if (to > tex.height) { to = tex.height; }

                        for (int y = from-1; y < to; y++) {
                            for (int x = left_spacing; x < tex.width; x++) {
                                colors[y * tex.width + x] = info.color;
                            }
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

            List<Vector2> uv = new List<Vector2>();
            for (int i = 0; i < mf.mesh.uv.Length; i++) { uv.Add(Vector2.zero); }

            float pf = 1f / tex.width; // relative pos of front image
            float fw = 3f / tex.width; // relative front image width

            // FRONT    2    3    0    1
            /*
            uv[2] = new Vector2(pf, 1f);
            uv[3] = new Vector2(pf + fw, 1f);
            uv[0] = new Vector2(pf, 0f);
            uv[1] = new Vector2(pf + fw, 0f);
            */

            // BACK    6    7   10   11
            uv[6] = new Vector2(pf, 1f);
            uv[7] = new Vector2(pf + fw, 1f);
            uv[10] = new Vector2(pf, 0f);
            uv[11] = new Vector2(pf + fw, 0f);

            /*
            // LEFT   19   17   16   18
            uv[19] = Vector2( uvsLeft.x, uvsLeft.y );
            uv[17] = Vector2( uvsLeft.x + uvsLeft.width, uvsLeft.y );
            uv[16] = Vector2( uvsLeft.x, uvsLeft.y - uvsLeft.height );
            uv[18] = Vector2( uvsLeft.x + uvsLeft.width, uvsLeft.y - uvsLeft.height );

            // RIGHT   23   21   20   22
            uv[23] = Vector2( uvsRight.x, uvsRight.y );
            uv[21] = Vector2( uvsRight.x + uvsRight.width, uvsRight.y );
            uv[20] = Vector2( uvsRight.x, uvsRight.y - uvsRight.height );
            uv[22] = Vector2( uvsRight.x + uvsRight.width, uvsRight.y - uvsRight.height );

            // TOP    4    5    8    9
            uv[4] = Vector2( uvsTop.x, uvsTop.y );
            uv[5] = Vector2( uvsTop.x + uvsTop.width, uvsTop.y );
            uv[8] = Vector2( uvsTop.x, uvsTop.y - uvsTop.height );
            uv[9] = Vector2( uvsTop.x + uvsTop.width, uvsTop.y - uvsTop.height );

            // BOTTOM   15   13   12   14
            uv[15] = Vector2( uvsBottom.x, uvsBottom.y );
            uv[13] = Vector2( uvsBottom.x + uvsBottom.width, uvsBottom.y );
            uv[12] = Vector2( uvsBottom.x, uvsBottom.y - uvsBottom.height );
            uv[14] = Vector2( uvsBottom.x + uvsBottom.width, uvsBottom.y - uvsBottom.height );
            */

            // apply UV coordinates
            mf.mesh.SetUVs(0, uv);
        }

    }
}
