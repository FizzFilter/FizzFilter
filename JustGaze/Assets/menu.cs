using UnityEngine;
using System.Collections;
using UnityEngine.VR.WSA.Input;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour {

    GestureRecognizer gestures;

	// Use this for initialization
	void Start () {
        gestures = new GestureRecognizer();
        gestures.TappedEvent += Gestures_TappedEvent;
        gestures.StartCapturingGestures();
	}

    private void Gestures_TappedEvent(InteractionSourceKind source, int tapCount, Ray headRay)
    {
        RaycastHit hitInfo;

        bool hit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 5.0f, -1);
        if (hit)
        {
            if(hitInfo.collider.tag == "ski")
            {
                SceneManager.LoadScene("Lodge");
            }
            else if( hitInfo.collider.tag == "beach")
            {
                SceneManager.LoadScene("Beach");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hitInfo;
        bool hit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 5.0f, -1);
        if (hit)
        {
            this.transform.position = hitInfo.point;
        }
    }
}
