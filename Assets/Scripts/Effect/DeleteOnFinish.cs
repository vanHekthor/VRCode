using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteOnFinish : MonoBehaviour {

    ParticleSystem pSystem;

    [SerializeField]
    private bool active = false;

	// Use this for initialization
	void Start () {
        pSystem = gameObject.GetComponent<ParticleSystem>();
	}
	
	// Update is called once per frame
	void Update () {
		
        // Destroy this gameobject if the particle system finished
        if (active) {
            if (pSystem && !pSystem.IsAlive()) {
                Destroy(gameObject);
            }
        }
	}

    public void activate() {
        if (pSystem) {
            active = true;
        }
    }

}
