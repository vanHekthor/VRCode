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
    public class CodeCityTexture : MonoBehaviour {

        private Color baseColor;
        private Texture2D tex_regions;
        private bool generated = false;
        private CodeCityElement cce;

        /// <summary>Region texture information.</summary>
        public class Info {

            public Region region;
            public Color color;

            public Info(Region region, Color color) {
                this.region = region;
                this.color = color;
            }
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

            int width = 800; // pixel
            float ratio = transform.localScale.y / transform.localScale.x;
            tex_regions = new Texture2D(width, Mathf.RoundToInt(width * ratio), TextureFormat.RGB24, false) {
                filterMode = FilterMode.Point,
            };

            // color texture with base color
            Color[] colors = new Color[tex_regions.width * tex_regions.height];
            for (int i = 0; i < colors.Length; i++) { colors[i] = baseColor; }

            foreach (Info info in regionTexInfo) {
                foreach (Region.Section s in info.region.GetSections()) {
                    // ToDo: place regions on texture
                }
            }

            // assign pixels
            tex_regions.SetPixels(colors);

            generated = true;
            return true;
        }


        /// <summary>Applies the generated texture to this element.</summary>
        public void ApplyTexture() {

            if (!generated) { return; }

            // ToDo: set UV coordinates accordingly

            //  set color of element material to white and apply texture
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = Color.white;
            renderer.material.mainTexture = tex_regions;
        }

    }
}
