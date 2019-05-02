using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBlock : MonoBehaviour {

    private float timeStart = -1;
    private float timeEnd = -1;
    private bool activeTimer = false;

    public float timer = 3;
    public float timer_reduce = 2;
    public float timer_cur = 0;

    public float perc = 0;
    private Color color_start;
    public Color color_end = Color.red;
    private Color color_diff;

    public GameObject explosionEffectPrefab;

    public bool coolDownOnExit = true;
    public bool destroy = true;


	// Use this for initialization
	void Start () {
		color_start = gameObject.GetComponent<Renderer>().material.color;
	}
	

	// Update is called once per frame
	void Update () {
		
        if (timeStart >= 0) {
            timerUpdate();
        }
	}


    void OnTriggerEnter(Collider col) {

        if (col.tag.ToLower() == "playerhand") {
            timeStart = Time.time - timer * perc;
            timer_cur = timer;
            activeTimer = true;
            Debug.Log("Exploding started...");
        }
    }


    void OnTriggerExit(Collider col) {

        if (timeStart < 0) { return; }

        if (coolDownOnExit && col.tag.ToLower() == "playerhand") {
            timeEnd = Time.time + timer_reduce * perc;
            timer_cur = timer_reduce;
            activeTimer = false;
            Debug.Log("Exploding stopped...");
        }
    }


    void timerUpdate() {

        if (activeTimer) {
            timeEnd = Time.time;
        }
        else {
            timeStart = Time.time;
        }
 
        float timeDiff = timeEnd - timeStart;
        if (timeDiff < 0) { timeDiff = 0; }
        perc = timeDiff / timer_cur;
        perc = perc > 1 ? 1 : perc < 0 ? 0 : perc; // set percentage range [0,1]
        Color newColor = color_start * (1-perc) + color_end * perc;

        if (perc >= 0 && perc <= 1) {
            gameObject.GetComponent<Renderer>().material.color = newColor;

            // explode
            if (perc == 1) {
                destroyMe();
            }
            else if (!activeTimer && perc == 0) {
                timeStart = -1;
                timeEnd = -1;
            }
        }
    }


    // destroy or reset this object
    void destroyMe() {
        if (destroy) {

            if (explosionEffectPrefab) {
                GameObject o = (GameObject) Instantiate(explosionEffectPrefab);

                // apply start color of particle system if possible
                ParticleSystem effectPS = o.GetComponent<ParticleSystem>();
                if (effectPS) {
                    var main = effectPS.main;
                    var thisColor = gameObject.GetComponent<Renderer>().material.color;
                    main.startColor = thisColor;
                    o.GetComponent<Renderer>().material.color = thisColor;
                    effectPS.Play();
                }

                o.transform.position = transform.position;
                DeleteOnFinish c = (DeleteOnFinish) o.AddComponent<DeleteOnFinish>();
                c.activate(); // delete this object if particle system finished
            }

            Destroy(gameObject);
        }
        else {
            activeTimer = false;
            timeStart = -1;
            timeEnd = -1;
            perc = 0;
            gameObject.GetComponent<Renderer>().material.color = color_start;
        }
    }

}
