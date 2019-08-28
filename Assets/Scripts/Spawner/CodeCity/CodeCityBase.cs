using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Interaction.CodeCity;

namespace VRVis.Spawner.CodeCity {

    /// <summary>
    /// Baseplate of code city.<para/>
    /// Is attached to the gameobject that contains the code city.<para/>
    /// Created: 23.08.2019 (by Leon H.)<para/>
    /// Updated: 28.08.2019
    /// </summary>
    public class CodeCityBase : MonoBehaviour {

        [Tooltip("Code city component to create base plate for")]
        public CodeCityV1 codeCityComponent;

        [Tooltip("Transform that holds all the spawned plates (should not be this one!)")]
        public Transform modelHolder;

        [Tooltip("Light to place above at middle of code city (do not attach it directly to the city, the script does it)")]
        public Transform middleLight;

        [Header("Prefabs")]
        public GameObject middlePart;
        public GameObject plateLift; // plate to lift the city
        public GameObject plateRotate; // plate to rotate the city


        // instance objects
        private Transform i_plateLift;
        private Transform i_plateRotate;
        private Transform i_middlePart;

        private Vector3 prev_cityPos;


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
            
            if (!isActiveAndEnabled) { return; }
            Debug.Log("Spawning code city base plates..");

            // remove all children (works bc. destroy is not executed immediately)
            foreach (Transform child in modelHolder) {
                if (child != modelHolder) { Destroy(child); }
            }
            
            Vector3 citySize = new Vector3(codeCityComponent.citySize.x, 0, codeCityComponent.citySize.y);
            Vector3 cityCenter = codeCityComponent.transform.position;

            // instantiate parts
            Vector3 platePos = (cityCenter.y - plateLift.transform.localScale.y) * Vector3.up;
            i_plateLift = Instantiate(plateLift, platePos, Quaternion.identity).transform;
            i_plateLift.SetParent(modelHolder, false);
            Vector3 plateScale = new Vector3(citySize.x * 0.5f, i_plateLift.localScale.y, citySize.z * 0.5f);
            i_plateLift.localScale = plateScale;

            CodeCityLift liftComponent = i_plateLift.GetComponent<CodeCityLift>();
            if (liftComponent) { liftComponent.codeCity = codeCityComponent; }
            
            platePos = i_plateLift.localPosition - Vector3.up * plateRotate.transform.localScale.y;
            i_plateRotate = Instantiate(plateRotate, platePos, Quaternion.identity).transform;
            i_plateRotate.SetParent(modelHolder, false);
            plateScale.y = i_plateRotate.localScale.y;
            i_plateRotate.localScale = plateScale;

            CodeCityRotate rotComponent = i_plateRotate.GetComponent<CodeCityRotate>();
            if (rotComponent) { rotComponent.codeCity = codeCityComponent; }
            
            i_middlePart = Instantiate(middlePart, Vector3.zero, Quaternion.identity).transform;
            i_middlePart.SetParent(modelHolder, false);
            i_middlePart.localScale = new Vector3(citySize.x * 0.25f, platePos.y, citySize.z * 0.25f);

            // place light if assigned
            if (middleLight) {
                middleLight.SetParent(codeCityComponent.transform, false);
                middleLight.localPosition = Vector3.up * codeCityComponent.cityHeightRange.y * 1.5f;
                middleLight.GetComponent<Light>().range = Mathf.Max(citySize.x, citySize.z);
            }

            // apply position and rotation of the code city
            modelHolder.position = new Vector3(cityCenter.x, modelHolder.position.y, cityCenter.z);
            modelHolder.rotation = codeCityComponent.parent.rotation;
        }

	
	    void Update() {

            if (!codeCityComponent) { return; }

            // only update position on change detection
            Transform cct = codeCityComponent.transform;
            if (prev_cityPos != cct.position) {
                prev_cityPos = cct.position;
                UpdateModel();
            }

            // always update model rotation for quick response
            if (modelHolder) { modelHolder.transform.rotation = codeCityComponent.transform.rotation; }
	    }


        /// <summary>
        /// Updates the base plate model (height and rotation).<para/>
        /// </summary>
        private void UpdateModel() {
            
            Vector3 cityPos = codeCityComponent.transform.position;
            Vector3 platePos = i_plateLift.localPosition;

            platePos.y = cityPos.y - i_plateLift.localScale.y;
            i_plateLift.localPosition = platePos;

            platePos.y -= i_plateRotate.localScale.y;
            i_plateRotate.localPosition = platePos;

            // scale middle part
            if (platePos.y <= 0) { i_middlePart.gameObject.SetActive(false); }
            else {
                i_middlePart.gameObject.SetActive(true);
                i_middlePart.localScale = new Vector3(i_middlePart.localScale.x, platePos.y, i_middlePart.localScale.z);
            }
        }

    }
}
