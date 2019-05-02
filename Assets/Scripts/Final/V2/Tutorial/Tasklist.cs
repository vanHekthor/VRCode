using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.Tutorial {

    /// <summary>
    /// For the tutorial task list.<para/>
    /// To check/uncheck the tasks.
    /// </summary>
    public class Tasklist : MonoBehaviour {

        [System.Serializable]
	    public class TaskEntry {

            [HideInInspector]
            public string name;
            public Toggle toggle;
            public GameObject go;
            public bool check;
            public bool checkPrevious;

            public TaskEntry(Toggle toggle, GameObject go, bool check) {
                this.toggle = toggle;
                this.go = go;
                this.check = check;
                checkPrevious = check;
                name = go.name;
            }
        }

        [Tooltip("Transform that holds all the toggle entries.")]
        public Transform toggleHolder;

        [Tooltip("Show the next task after the previous one finished.")]
        public bool oneAfterAnother = true;

        [Space]
        [Tooltip("Disable and hide the object specified objects if all tasks are finished.")]
        public bool disableAfterFinish = false;
        public GameObject[] objectsToDisable;

        [Space]
        [Tooltip("Enable and show the specified objects if all tasks are finished.")]
        public bool enableAfterFinish = false;
        public GameObject[] objectsToEnable;

        [Space]
        [Tooltip("Writes the times between completion of the tasks to a file.")]
        public bool storeTimesInFile = true;
        public string filenamePrefix = "userinfo";
        public string fileExtension = ".jsonl";

        [Tooltip("Leave empty to use the name of the game object this component is attached to.")]
        public string tasklistName = "";

        [Space]
        [Tooltip("Will be created automatically on startup using entries of the toggle holder.")]
        public TaskEntry[] taskList;


        private bool allFinished = false;
        private bool allFinishedPrev = false;

        private static string finalFilePath;
        private static bool finalFilePathSet = false;


        private void Awake() {

            // set task list name
            if (tasklistName == null || tasklistName.Length == 0) { tasklistName = gameObject.name; }

            // prepare file path to store times in
            if (!finalFilePathSet) {
                finalFilePathSet = true;
                string timeFormat = "ddMMyyyy-hhmmss";
                string timeStamp = DateTime.Now.ToString(timeFormat);
                finalFilePath = Application.persistentDataPath + '/' + filenamePrefix + '_' + timeStamp + fileExtension;
            }

            // store start if task
            StoreTaskTime("start");
        }



        void Start() {

            // get all the toggle entries
            if (!toggleHolder) { Debug.LogError("Missing toggle holder!", this); }
            else { CreateToggleList(toggleHolder); }
        }


        /// <summary>
        /// Creates the list of tasks using the assigned toggle holder.
        /// </summary>
        void CreateToggleList(Transform holder) {
            
            List<TaskEntry> list = new List<TaskEntry>();

            foreach (Transform child in holder) {
                Toggle t = child.GetComponent<Toggle>();
                list.Add(new TaskEntry(t, child.gameObject, false));
            }
            
            taskList = list.ToArray();
            UpdateToggleList();
        }


        void OnValidate() {

            // update if check in inspector changes
            if (taskList != null) { UpdateToggleList(); }
        }


        /// <summary>
        /// Updates the toggle components according to their "check" state.
        /// </summary>
        void UpdateToggleList() {

            bool oneActive = false;

            allFinishedPrev = allFinished;
            allFinished = true;

            for (int i = taskList.Length - 1; i >= 0; i--) {

                TaskEntry e = taskList[i];
                if (!e.check) { allFinished = false; }
                if (e.toggle) { e.toggle.isOn = e.check; }

                // this task state changed - so store the information
                if (storeTimesInFile && e.check != e.checkPrevious) { StoreTaskTime(e.name); }
                e.checkPrevious = e.check;

                // show always one after the current active task
                if (!oneAfterAnother) { continue; }

                bool prevActive = false;
                if (i > 0) { prevActive = taskList[i-1].check; }
                if (i == taskList.Length-1 && e.check) { oneActive = true; }

                GameObject go = e.toggle != null ? e.toggle.gameObject : e.go;

                if (prevActive || oneActive || i == 0) {
                    if (go) { go.SetActive(true); }
                    oneActive = true;
                }
                else if (go) { go.SetActive(false); }
            }

            // hide/show if list is completed
            if (taskList.Length > 0 && allFinished && !allFinishedPrev) {
                
                if (disableAfterFinish) {
                    foreach (GameObject o in objectsToDisable) {
                        if (!o) { continue; }
                        o.SetActive(false);
                    }
                }

                if (enableAfterFinish) {
                    foreach (GameObject o in objectsToEnable) {
                        if (!o) { continue; }
                        o.SetActive(true);
                    }
                }
            }
        }


        /// <summary>
        /// Mark the next available task as finished.
        /// </summary>
        public void NextTask() {

            if (taskList.Length == 0) { return; }

            foreach (TaskEntry e in taskList) {
                if (!e.check) { e.check = true; break; }
            }

            UpdateToggleList();
        }


        /// <summary>
        /// Store information that this tasks state changed.
        /// </summary>
        private void StoreTaskTime(string partName) {

            if (!finalFilePathSet) { return; }

            // for docs see here:
            // https://docs.microsoft.com/de-de/dotnet/api/system.io.file.open?view=netframework-4.7.2#System_IO_File_Open_System_String_System_IO_FileMode
            // https://docs.microsoft.com/de-de/dotnet/api/system.io.filemode?view=netframework-4.7.2
            //FileStream fs = File.Open(finalFilePath, FileMode.Append, FileAccess.Write);
            
            // add data as new line to the file
            using (StreamWriter sw = new StreamWriter(finalFilePath, true)) {
                sw.WriteLine(CreateTaskTimeEntryJSONL(tasklistName, partName));
            }
        }

        /// <summary>
        /// Creates and returns a task time entry for JSONL format.
        /// </summary>
        private string CreateTaskTimeEntryJSONL(string tasklistName, string partName) {

            string timeFormat = "dd.MM.yyyy-HH:mm:ss";
            string timeStamp = DateTime.Now.ToString(timeFormat);

            // create a JSONL entry
            string outJSONL = 
            '{' +
                "\"time\": \"" + timeStamp + "\", " +
                "\"task\": \"" + tasklistName + "\", " +
                "\"part\": \"" + partName + "\"" +
            '}';

            return outJSONL;
        }

    }
}
