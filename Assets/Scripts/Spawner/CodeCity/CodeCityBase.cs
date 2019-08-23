using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Baseplate of code city.<para/>
    /// Is attached to the gameobject that contains the code city.<para/>
    /// Created: 23.08.2019 (by Leon H.)<para/>
    /// Updated: 23.08.2019
    /// </summary>
    public class CodeCityBase : MonoBehaviour {

        public CodeCityV1 codeCityComponent;

        [Tooltip("Transform that holds all the spawned plates (should not be this one!)")]
        public Transform modelHolder;

        [Tooltip("Light to place above at middle of code city")]
        public Transform middleLight;

        [Header("Prefabs")]
        public GameObject middlePart;
        public GameObject plateLift; // plate to lift the city
        public GameObject plateRotate; // plate to rotate the city


        // instance objects
        private Transform i_plateLift;
        private Transform i_plateRotate;
        private Transform i_middlePart;

        private float codeCityHeight;


        private void Awake() {
            
            // register event listeners on code city
            if (!codeCityComponent) { Debug.LogError("Code city component not assigned!", this); return; }
            if (!modelHolder) { Debug.LogError("Model holder not assigned!", this); return; }
            if (modelHolder == transform) { Debug.LogError("Model holder can not be this object!", this); return; }
            codeCityComponent.cityVisibilityChanged.AddListener(CityVisibilityChangeEvent);
            codeCityComponent.citySpawnedEvent.AddListener(CitySpawnedEvent);
        }
        

        /// <summary>Called when the visibility of the visualization changed.</summary>
        private void CityVisibilityChangeEvent(bool visible) {
            modelHolder.gameObject.SetActive(visible);
        }

        /// <summary>Called when the city was recently spawned.</summary>
        private void CitySpawnedEvent() { SpawnCityBase(); }


        /// <summary>Spawns the base plates of the code city.</summary>
        private void SpawnCityBase() {
            
            Debug.Log("Spawning code city base plates..");

            // remove all children (works bc. destroy is not executed immediately)
            foreach (Transform child in modelHolder) {
                if (child != modelHolder) { Destroy(child); }
            }
            
            Vector3 citySize = new Vector3(codeCityComponent.citySize.x, 0, codeCityComponent.citySize.y);
            Vector3 cityCenter = codeCityComponent.parent.transform.position;
            Vector3 invScale = new Vector3(1f / modelHolder.localScale.x, 1f / modelHolder.localScale.y, 1f / modelHolder.localScale.z);

            // instantiate parts
            Vector3 platePos = (cityCenter.y - plateLift.transform.localScale.y * 0.5f) * invScale.y * Vector3.up;
            i_plateLift = Instantiate(plateLift, platePos, Quaternion.identity).transform;
            i_plateLift.SetParent(modelHolder, false);
            Vector3 plateScale = new Vector3(citySize.x, i_plateLift.localScale.y, citySize.z);
            i_plateLift.localScale = plateScale;

            platePos = i_plateLift.localPosition - Vector3.up * plateRotate.transform.localScale.y;
            i_plateRotate = Instantiate(plateRotate, platePos, Quaternion.identity).transform;
            i_plateRotate.SetParent(modelHolder, false);
            i_plateRotate.localScale = plateScale;
            
            i_middlePart = Instantiate(middlePart, Vector3.zero, Quaternion.identity).transform;
            i_middlePart.SetParent(modelHolder, false);
            i_middlePart.localScale = new Vector3(citySize.x * 0.25f, platePos.y, citySize.z * 0.25f);

            // place light if assigned
            if (middleLight) {
                middleLight.position = cityCenter + Vector3.up * (i_plateLift.position.y + 1);
                middleLight.GetComponent<Light>().range = Mathf.Max(citySize.x, citySize.z);
            }

            // apply position and rotation of the code city
            modelHolder.position = new Vector3(cityCenter.x, modelHolder.position.y, cityCenter.z);
            modelHolder.rotation = codeCityComponent.parent.rotation;
        }

	
	    // Update is called once per frame
	    void Update () {
		
            // ToDo: adjust models based on rotation and so on and allow rotation + lift
	    }

    }
}
