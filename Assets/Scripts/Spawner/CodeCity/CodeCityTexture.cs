using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Attached to a code city element.<para/>
    /// Is called by the code city component when a texture should be generated.<para/>
    /// Created: 04.09.2019 (Leon H.)<para/>
    /// Updated: 04.09.2019
    /// </summary>
    public class CodeCityTexture : MonoBehaviour {

        private Texture2D tex;
        private bool generated = false;
        private CodeCityElement cce;


        /// <summary>Tells if the texture has been generated.</summary>
        public bool IsGenerated() { return generated; }


        /// <summary>Generates the texture and tells if this was successful.</summary>
	    public bool GenerateTexture() {

            generated = false;
            cce = GetComponent<CodeCityElement>();
            if (!cce) { return false; }

            // ToDo

            generated = true;
            return true;
        }


        /// <summary>Applies the generated texture to this element.</summary>
        public void ApplyTexture() {

            if (!generated || tex == null) { return; }

            // ToDo
        }

    }
}
