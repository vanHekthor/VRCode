using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VRVis.UI.Structure {

    /// <summary>
    /// Script that is attached to the structure hover ui.<para/>
    /// It holds references to the required text elements.
    /// </summary>
    public class StructureHoverInfo : MonoBehaviour {

        public TMP_Text text_Name;
        public TMP_Text text_Path;
        public TMP_Text text_Info;

        public void SetName(string name) {
            if (text_Name != null) { text_Name.text = name; }
        }

        public void SetPath(string path) {
            if (text_Path != null) { text_Path.text = path;}
        }

        /// <summary>
        /// Set additional info (e.g. if a file is opened).
        /// </summary>
        public void SetInfo(string info) {
            if (text_Info) { text_Info.text = info; }
        }

    }
}
