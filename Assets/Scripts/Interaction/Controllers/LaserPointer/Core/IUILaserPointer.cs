using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


/// <summary>
/// Initial code by github.com/wacki.<para/>
/// 
/// Modified and extended by github.com/S1r0hub.<para/>
/// Updated: 28.08.2019
/// </summary>
namespace VRVis.Interaction.LaserPointer {

    abstract public class IUILaserPointer : MonoBehaviour {

        public Transform laserOrigin;
        public Color laserColor;
        public Color hitColor;
        public float laserThickness = 0.005f;
        public float laserHitScale = 0.02f;
        public float maxRayDistance = 100f;

        [Tooltip("Is the laser pointer activated by default?")]
        public bool laserDefaultOn = false;
        public bool laserAlwaysOn = false;

        [Tooltip("Layer mask for physics ray hit (for others change InputModule!)")]
        public LayerMask rayLayerMask;

        [Tooltip("Override this layer mask with the one of the input module?")]
        public bool syncLayerMaskInputModule = false;

        [Tooltip("Let input module use this layermask for raycasting everything (not just physics)?")]
        public bool overrideInputModuleLayerMask = true;


        [Header("Laserpointer Event Callbacks")]
        public BoolEvent laserActiveStateChanged = new BoolEvent();
        [System.Serializable] public class BoolEvent : UnityEvent<bool> {}


        private GameObject hitPoint;
        private GameObject pointer;
        
        private bool laserActive = false;
        private float _distanceLimit;

        private static Material ptrMaterial;
        private MeshRenderer laser_mr;
        private MeshRenderer hitPoint_mr;

        /// <summary>Stores the last hit game object.</summary>
        private GameObject lastHitObject;

        

        void Start() {

            laserActive = laserAlwaysOn;
            laserActive = laserDefaultOn;

            // todo:    let the user choose a mesh for laser pointer ray and hit point
            //          or maybe abstract the whole menu control some more and make the 
            //          laser pointer a module.
            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.transform.SetParent(transform, false);
            pointer.transform.localScale = new Vector3(laserThickness, laserThickness, 100.0f);
            pointer.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);
            pointer.SetActive(laserActive);

            hitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitPoint.transform.SetParent(transform, false);
            hitPoint.transform.localScale = new Vector3(laserHitScale, laserHitScale, laserHitScale);
            hitPoint.transform.localPosition = new Vector3(0.0f, 0.0f, 100.0f);
            hitPoint.SetActive(false);

            // remove the colliders on our primitives
            DestroyImmediate(hitPoint.GetComponent<SphereCollider>());
            DestroyImmediate(pointer.GetComponent<BoxCollider>());
            
            // create pointer material
            if (!ptrMaterial) {
                ptrMaterial = new Material(Shader.Find("VRVis/LaserPointer"));
                ptrMaterial.SetColor("_Color", Color.white); // set default color
            }
            laser_mr = pointer.GetComponent<MeshRenderer>();
            hitPoint_mr = hitPoint.GetComponent<MeshRenderer>();
            laser_mr.material = ptrMaterial;
            hitPoint_mr.material = ptrMaterial;
            SetLaserColor(laserColor);
            SetHitColor(hitColor);

            // initialize concrete class
            Initialize();
            
            // register with the LaserPointerInputModule
            if (LaserPointerInputModule.instance == null) {
                new GameObject().AddComponent<LaserPointerInputModule>();
            }
            
            LaserPointerInputModule.instance.AddController(this);
        }

        private void OnDrawGizmos() {
         
            // show the laser origin
            if (laserOrigin) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(laserOrigin.position, 0.01f);
                Gizmos.DrawRay(laserOrigin.position, transform.forward * 0.1f);
            }
        }

        void OnDestroy() {

            if (LaserPointerInputModule.instance != null)
                LaserPointerInputModule.instance.RemoveController(this);
        }

        protected virtual void Initialize() { }

        public virtual void OnEnterControl(GameObject control) {
            lastHitObject = control;
        }

        public virtual void OnExitControl(GameObject control) {
            lastHitObject = null;
        }
        

        public abstract bool ButtonDown();

        public abstract bool ButtonUp();

        public abstract bool ButtonToggleClicked();

        /// <summary>Returns scroll delta.</summary>
        public virtual Vector2 GetScrollDelta() { return Vector2.zero; }

        public virtual bool IsScrolling() { return false; }


        protected virtual void Update() {
            UpdateCall();
        }

        /// <summary>
        /// Performs a laser update.
        /// Using raycast to detect hit and showing it.<para/>
        /// Moved to a separate method to be called from other methods as well.
        /// </summary>
        protected virtual void UpdateCall() {

            // check if user turns laser on/off and react accordingly
            // Disabled for the time being because with the Valve Index Controllers it was often triggered unintentionally! (qb19zexe, 03.06.2021) 
            //if (!laserAlwaysOn && ButtonToggleClicked()) {
            //    if (laserActive) { HideLaser(); }
            //    else { ShowLaser(); }
            //    Debug.Log("Laser pointer " + (laserActive ? "enabled" : "disabled"));
            //}

            // don't do anything if the laser is disabled
            if (!laserActive) { return; }

            // use the origin transform position if provided
            Vector3 origin_pos = transform.position;
            if (laserOrigin) { origin_pos = laserOrigin.position; }

            // create and cast the ray that hits colliders (does not hit UI elements)
            Ray ray = new Ray(origin_pos, transform.forward);
            RaycastHit hitInfo;
            bool bHit = Physics.Raycast(ray, out hitInfo, Mathf.Infinity, rayLayerMask);

            float distance = maxRayDistance;
            if (bHit) { distance = hitInfo.distance; }

            // limit ray distance
            if (_distanceLimit > 0.0f) {
                distance = Mathf.Min(distance, _distanceLimit);
                bHit = true;
            }

            // scale and position the laser "ray"
            pointer.transform.localScale = new Vector3(laserThickness, laserThickness, distance);
            pointer.transform.position = ray.origin + distance * 0.5f * ray.direction;

            // position the hit point
            if (bHit) {
                hitPoint.SetActive(true);
                hitPoint.transform.position = ray.origin + distance * ray.direction;
            }
            else {
                hitPoint.SetActive(false);
            }

            // reset the previous distance limit
            _distanceLimit = -1.0f;
        }

        // limits the laser distance for the current frame
        public virtual void LimitLaserDistance(float distance) {

            if (distance < 0.0f) { return; }

            if (_distanceLimit < 0.0f) { _distanceLimit = distance; }
            else { _distanceLimit = Mathf.Min(_distanceLimit, distance); }
        }


        // ==== ADDED BY S1r0hub ==== //

        public void SetLaserColor(Color laserColor) {
            if (laser_mr) { laser_mr.material.color = laserColor; }
        }

        public void ResetLaserColor() { SetLaserColor(laserColor); }


        public void SetHitColor(Color hitColor) {
            if (hitPoint_mr) { hitPoint_mr.material.color = hitColor; }
        }

        public void ResetHitColor() { SetHitColor(hitColor); }


        public virtual void ShowLaser() {
            pointer.SetActive(true);
            laserActive = true;
            laserActiveStateChanged.Invoke(laserActive);
        }

        public virtual void HideLaser() {
            hitPoint.SetActive(false);
            pointer.SetActive(false);
            laserActive = false;
            laserActiveStateChanged.Invoke(laserActive);
        }


        public bool IsLaserActive() { return laserActive; }


        public GameObject GetLastHitObject() { return lastHitObject; }

    }
} // end of namespace
