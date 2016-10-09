using UnityEngine;
using System.Collections;
using System;
using UnityEngine.VR.WSA.Input;
using HoloToolkit.Unity;
using System.Collections.Generic;

public class ShitPutter : MonoBehaviour
{


    public GestureRecognizer gestures;

    public LayerMask layerMask = -1;

    public GameObject menu;

    public List<GameObject> sceneObjects;

    public GameObject cursor;


    // Use this for initialization

    void Start()
    {
        gestures = new GestureRecognizer();
        gestures.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.DoubleTap);
        gestures.TappedEvent += Gestures_TappedEvent;
        gestures.StartCapturingGestures();
    }

    private void Gestures_TappedEvent(InteractionSourceKind source, int tapCount, Ray headRay)
    {
        print("Saw Tap");
        placeObjectFromList();
    }

    void openMenu()
    {
        menu.SetActive(!menu.activeInHierarchy);
    }

    void Update()
    {
        RaycastHit hitInfo;
        bool hit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 5.0f, layerMask);
        if (hit)
        {
            cursor.transform.position = hitInfo.point;
        }
    }

    void placeObjectFromList()
    {
        print(sceneObjects.Count);
        if (sceneObjects.Count > 0)
        {
            print("Placing Object");
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 5.0f, layerMask);
            if (hit)
            {
                GameObject temp = (GameObject)Instantiate(sceneObjects[0], hitInfo.point, Quaternion.identity);
                temp.transform.LookAt(Camera.main.transform.position);
                temp.transform.up = Vector3.up;
            }
            sceneObjects.RemoveAt(0);
        }
    }


}
