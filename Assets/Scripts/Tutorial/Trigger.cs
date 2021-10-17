using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Trigger : MonoBehaviour
{
    public UnityEvent onTriggerEnter;

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.name == "BodyCollider") {
            onTriggerEnter.Invoke();
        }
    }
}
