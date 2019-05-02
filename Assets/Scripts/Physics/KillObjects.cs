using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Kills object that hit the collider of this object.
public class KillObjects : MonoBehaviour {

    bool kill = true;

    void OnTriggerEnter(Collider col) {
        if (kill && col.gameObject) {
            Destroy(col.gameObject);
        }
    }

}
