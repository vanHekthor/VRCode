using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Visualization.Heightmap {

    /// <summary>
    /// Script attached to the main component of the heightmap
    /// and used to change the text of its labels accordingly.<para/>
    /// It calls the "ChangeTextHelper" on the applied text objects.
    /// </summary>
    public class SetHeightmapLabelText : MonoBehaviour {

        [Tooltip("GameObject of label with ChangeTextHelper")]
	    public GameObject textFrom;
        public GameObject textTo;

        /// <summary>Can be used with "SendMessage" to change the text of the "from" label.</summary>
        public void SetHeightmapLabel_from(string text) {

            if (!textFrom) { return; }
            textFrom.SendMessage("ChangeText", text);
        }

        /// <summary>Can be used with "SendMessage" to change the text of the "to" label.</summary>
        public void SetHeightmapLabel_to(string text) {

            if (!textTo) { return; }
            textTo.SendMessage("ChangeText", text);
        }

    }
}
