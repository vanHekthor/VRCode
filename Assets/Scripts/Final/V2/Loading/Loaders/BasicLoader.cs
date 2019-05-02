using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.IO {

    /**
     * Abstract base class for "loaders".
     * Those can be file loaders as well as the structure loader.
     */
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

        /**
         * Load the information from the file.
         * Returns true if successful.
         */
        public abstract bool Load();

    }
}
