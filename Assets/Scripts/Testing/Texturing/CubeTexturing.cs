using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Testing.Texturing {

    /// <summary>
    /// Add this component to a default cube.<para/>
    /// Each side should be rendered accordingly.<para/>
    /// 
    /// Written by Leon H. (July 2019)
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(Renderer))]
    public class CubeTexturing : MonoBehaviour {

        private MeshFilter mf;

        private bool done = false;

	    void Start () {
		
            // get mesh filter
            mf = GetComponent<MeshFilter>();
            if (!mf) {
                Debug.LogError("Could not find MeshFilter component!");
                return;
            }
	    }


        private void Update() {
            
            if (!done && Time.realtimeSinceStartup > 3) {
                done = true;

                // set the UV mesh and creates the texture
                mf.mesh.SetUVs(0, GetCubeUVs());
                Texture2D t = CreateCubeTexture(4, 4);
                GetComponent<Renderer>().material.mainTexture = t;
            }
        }


        /// <summary>
        /// Creates the cube texture.<para/>
        /// Uses a texture atlas like this:<para/>
        /// | - | K | - | - |<para/>
        /// | L | T | R | B |<para/>
        /// | - | F | - | - |<para/>
        /// L = Left, K = Back, T = Top, F = Front, ...
        /// </summary>
        /// <param name="width_side">Width per side</param>
        /// <param name="height_side">Height per side</param>
        private Texture2D CreateCubeTexture(int width_side, int height_side) {

            if (width_side < 1) { width_side = 1; }
            if (height_side < 1) { height_side = 1; }

            // width consists of 4 elements/sprites and height of 3
            int x_parts = 4;
            int y_parts = 3;

            Texture2D tex = new Texture2D(width_side * x_parts, height_side * y_parts, TextureFormat.RGB24, false) {
                filterMode = FilterMode.Point,
            };

            for (int y = 0; y < y_parts; y++) {
                for (int x = 0; x < x_parts; x++) {
                
                    Color c = Color.white;

                    // part color selection
                    if (y == 0 && x == 1) { c = Color.red; } // front
                    else if (y == 1 && x == 0) { c = Color.green; } // left
                    else if (y == 1 && x == 1) { c = Color.blue; } // top
                    else if (y == 1 && x == 2) { c = Color.magenta; } // right
                    else if (y == 1 && x == 3) { c = Color.cyan; } // bottom
                    else if (y == 2 && x == 1) { c = Color.yellow; } // back

                    ColorPart(tex, x * width_side, y * height_side, width_side, height_side, c);
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Colors a single part in the texture.
        /// </summary>
        /// <param name="x">Start position x</param>
        /// <param name="y">Start position y</param>
        private void ColorPart(Texture2D tex, int x, int y, int width, int height, Color c) {

            for (int w = 0; w < width; w++) {
                for (int h = 0; h < height; h++) {
                    tex.SetPixel(x + w, y + h, c);
                    //Debug.Log("Setting pixel: x=" + (x+w) + ", y=" + (y+h));
                }
            }
        }

        /// <summary>
        /// Get the UVs of the cube for the generated cube texture.<para/>
        /// Informative blog post:
        /// http://ilkinulas.github.io/development/unity/2016/04/30/cube-mesh-in-unity3d.html
        /// </summary>
        private List<Vector2> GetCubeUVs() {

            List<Vector2> uvs = new List<Vector2>();

            // div by 4
            float x0 = 0;
            float x1 = 1f / 4f;
            float x2 = 2f / 4f;
            float x3 = 3f / 4f;
            float x4 = 1;
            
            // div by 3
            float y0 = 0;
            float y1 = 1f / 3f;
            float y2 = 2f / 3f;
            float y3 = 1;

            // NOTE: order matters (F, T, K, B, R, L)

            // front
            uvs.Add(new Vector2(x1, y0));
            uvs.Add(new Vector2(x2, y0));
            uvs.Add(new Vector2(x1, y1));
            uvs.Add(new Vector2(x2, y1));
            
            // top
            uvs.Add(new Vector2(x1, y1));
            uvs.Add(new Vector2(x2, y1));
            uvs.Add(new Vector2(x1, y2));
            uvs.Add(new Vector2(x2, y2));

            // back
            uvs.Add(new Vector2(x1, y2));
            uvs.Add(new Vector2(x2, y2));
            uvs.Add(new Vector2(x1, y3));
            uvs.Add(new Vector2(x2, y3));

            // bottom
            uvs.Add(new Vector2(x3, y1));
            uvs.Add(new Vector2(x4, y1));
            uvs.Add(new Vector2(x3, y2));
            uvs.Add(new Vector2(x4, y2));

            // right
            uvs.Add(new Vector2(x2, y1));
            uvs.Add(new Vector2(x3, y1));
            uvs.Add(new Vector2(x2, y2));
            uvs.Add(new Vector2(x3, y2));

            // left
            uvs.Add(new Vector2(x0, y1));
            uvs.Add(new Vector2(x1, y1));
            uvs.Add(new Vector2(x0, y2));
            uvs.Add(new Vector2(x1, y2));

            return uvs;
        }

    }
}
