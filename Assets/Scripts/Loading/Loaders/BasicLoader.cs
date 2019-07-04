using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.IO {

    /// <summary>
    /// Abstract base class for "loaders".<para/>
    /// Those can be file loaders as well as the structure loader.
    /// </summary>
    public abstract class BasicLoader {

        protected bool loadingSuccessful = false;


	    // CONSTRUCTOR

        public BasicLoader() {
            // ...
        }


        // GETTER AND SETTER

        public bool LoadedSuccessful() {
            return loadingSuccessful;
        }


        // FUNCTIONALITY
        
        /// <summary>
        /// Load the information from the file.<para/>
        /// Returns true if successful.
        /// </summary>
        public abstract bool Load();

    }
}
