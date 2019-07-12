using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.Spawner;

namespace VRVis.Tutorial {

    /// <summary>
    /// Script to spawn code windows in a default scene.
    /// This positions and rotates code windows.
    /// </summary>
    public class SpawnCodeWindows : MonoBehaviour {

        [System.Serializable]
        public class CWEntry {
            public string relativeFilePath;
            public Vector3 position;
            public Vector3 rotation;
        }


        [Header("Spawn Settings")]
        [Tooltip("Time in seconds to spawn the windows after application startup")]
        public float spawnAfterSecond = 3;

        [Tooltip("How long to wait until spawning another window")]
        public float spawnTimeWindow = 0.5f;

        [Tooltip("Rotation offset to add to code windows")]
        public Vector3 windowRotationOffset = new Vector3(0, 180, 0);

        [Header("Gizmos")]
        [Tooltip("Show spawn positions in editor scene")]
        public bool showGizmos = true;
        public float gizmoSize = 0.2f;


        [Header("Code Windows")]
        public CWEntry[] entries;

        private bool startedSpawning = false;
        private bool finished = false;

        private bool windowSpawning = false;
        private bool windowSpawned = false;


        void Start() {

            if (entries.Length == 0) {
                Debug.LogWarning("No entries assigned.", this);
                enabled = false;
                return;
            }
        }


        void Update() {
            
            // start spawn coroutine
            if (!startedSpawning && Time.timeSinceLevelLoad > spawnAfterSecond) {
                startedSpawning = true;
                StartCoroutine(SpawnCoroutine());
            }
        }


        void OnDrawGizmos() {
            
            if (!showGizmos) { return; }

            uint num = 0;
            foreach (CWEntry e in entries) {

                num++;
                float perc = num / (float) entries.Length;
                Gizmos.color = Color.black * (1f - perc) + Color.white * perc;

                Vector3 pos = e.position + gizmoSize * 0.5f * Vector3.up;
                Gizmos.DrawSphere(pos, gizmoSize);
                Gizmos.DrawLine(pos, pos + Quaternion.Euler(e.rotation) * Vector3.forward);
            }
        }


        /// <summary>
        /// Coroutine to spawn the windows-
        /// </summary>
        private IEnumerator SpawnCoroutine() {

            ApplicationLoader loader = ApplicationLoader.GetInstance();
            if (loader == null) {
                Debug.LogError("Failed to spawn code windows! Missing application loader instance.");
                yield break;
            }

            Debug.Log("Started spawning code windows...");

            finished = false;
            uint winNo = 0;
            uint spawned = 0;

            foreach (CWEntry e in entries) {

                // wait before spawning another window
                if (winNo > 0) { yield return new WaitForSecondsRealtime(spawnTimeWindow); }
                winNo++;

                // validate path
                string filePath = e.relativeFilePath.ToLower();
                if (filePath.Length == 0) {
                    Debug.LogError("Failed to spawn window " + winNo + " - no file path given!");
                    continue;
                }

                // get full file path from relative one
                Debug.Log("Spawning window " + winNo + ": " + filePath);
                CodeFile cf = loader.GetStructureLoader().GetFileByRelativePath(filePath);
                if (cf == null) {
                    Debug.LogError("Failed to spawn code window " + winNo + " - file not found!");
                    continue;
                }

                // spawn window
                windowSpawning = true;
                Quaternion spawnRot = Quaternion.Euler(e.rotation + windowRotationOffset);

                FileSpawner fs = (FileSpawner) loader.GetSpawner("FileSpawner");
                if (fs) { fs.SpawnFile(cf.GetNode(), e.position, spawnRot, WindowSpawnedCallback); }
                else { WindowSpawnedCallback(false, null, "Missing FileSpawner!"); }
                
                // wait until spawning is finished
                yield return new WaitUntil(() => windowSpawning == false);
                if (windowSpawned) { spawned++; }
            }
            
            Debug.Log("Finished spawning " + spawned + "/" + entries.Length + " code windows.");
            finished = true;
        }


        /// <summary>
        /// Called after the window placement finished.
        /// </summary>
        private void WindowSpawnedCallback(bool success, CodeFile file, string msg) {

            windowSpawned = success;
            windowSpawning = false;

            if (!success) {
                string name = "";
                if (file != null && file.GetNode() != null) { name = "(" + file.GetNode().GetName() + ") "; }
                Debug.LogError("Failed to place window! " + name + msg);
                return;
            }
        }


        /// <summary>Tells if spawning the windows already finished.</summary>
        public bool IsSpawningFinished() { return finished; }

    }
}
