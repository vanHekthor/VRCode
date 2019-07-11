using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRVis.IO;

namespace VRVis.Spawner.File.Overview {

    public class RegionOverview : MonoBehaviour {

        [Tooltip("References of CodeFile this overview belongs to")]
        public CodeFileReferences fileRefs;


	    void Start () {
		
            if (!fileRefs) {
                Debug.LogError("Got no file references!");
                enabled = false;
                return;
            }

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

            if (amount > 0) {
                Debug.Log("RegionOverview: regions spawned!", this);
            }
        }


        /// <summary>
        /// Generates the overview texture and returns it.
        /// </summary>
        private Texture2D GenerateTexture() {
            
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
            List<List<int>> linePattern = new List<List<int>>();
            
            int charInfoStart = 0;

            foreach (TMP_TextInfo ti in fileRefs.GetTextElements()) {
                foreach (TMP_LineInfo li in ti.lineInfo) {
                    
                    List<int> chars = new List<int>(li.characterCount * 2);

                    for (int i = charInfoStart; i < charInfoStart + li.characterCount; i++) {

                        TMP_CharacterInfo ci = ti.characterInfo[i];

                        // encode color
                        int color = 0;
                        color |= ci.color.r;
                        color |= ci.color.g << 8;
                        color |= ci.color.b << 16;

                        // for now, only distinguish between visible, capital or small
                        int pattern = 0;
                        if (ci.isVisible) {

                            // for other patterns, use: System.Convert.ToInt32("11111111", 2);
                            if (char.IsUpper(ci.character)) { pattern = 255; }
                            else { pattern = System.Convert.ToInt32("00111111", 2); }
                        }
                    }
                }
            }

            // ToDo: !
            return new Texture2D(0, 0);
        }

    }
}
