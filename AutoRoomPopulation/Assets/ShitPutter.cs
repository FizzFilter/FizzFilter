using UnityEngine;
using System.Collections;
using System;
using UnityEngine.VR.WSA.Input;
using HoloToolkit.Unity;
using System.Collections.Generic;

public class ShitPutter : MonoBehaviour
{

    public enum WhereToPut
    {
        OnFloor,
        OnWall,
        OnCeiling,
        OnEdge,
        OnFloorAndCeiling,
        InAirFar,
        OnEdgeNearCenter,
        OnFloorFar,
        OnFloorNear
    }

    public GestureRecognizer gestures;

    public LevelSolver levelSolver;

    void Start()
    {
        levelSolver = LevelSolver.Instance;
    }

    [Serializable]
    public class ObjectToPlace
    {
        ObjectToPlace(WhereToPut input, GameObject whatToPut)
        {
            location = input;
            objToPlace = whatToPut;
        }
        public WhereToPut location;
        public GameObject objToPlace;
    }

    public ObjectToPlace[] sceneObjects;


    // Use this for initialization


    public void makeAllObjects()
    {
        foreach (ObjectToPlace element in sceneObjects)
        {
            print(element.objToPlace.name);
            doQuery(element);
        }
    }

    public void doQuery(ObjectToPlace input)
    {
        if (input.location == WhereToPut.OnFloor)
        {
            levelSolver.Query_OnFloor(input.objToPlace);
        }
        if (input.location == WhereToPut.OnWall)
        {
            levelSolver.Query_OnWall(input.objToPlace);
        }
        if (input.location == WhereToPut.OnCeiling)
        {
            levelSolver.Query_OnCeiling(input.objToPlace);
        }
        if (input.location == WhereToPut.OnEdge)
        {
            levelSolver.Query_OnEdge(input.objToPlace);
        }
        if (input.location == WhereToPut.OnFloorAndCeiling)
        {
            levelSolver.Query_OnFloorAndCeiling(input.objToPlace);
        }
        if (input.location == WhereToPut.InAirFar)
        {
            levelSolver.Query_RandomInAir_AwayFromMe(input.objToPlace);
        }
        if (input.location == WhereToPut.OnEdgeNearCenter)
        {
            levelSolver.Query_OnEdge_NearCenter(input.objToPlace);
        }
        if (input.location == WhereToPut.OnFloorFar)
        {
            levelSolver.Query_OnFloor_AwayFromMe(input.objToPlace);
        }
        if (input.location == WhereToPut.OnFloorNear)
        {
            levelSolver.Query_OnFloor_NearMe(input.objToPlace);
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameObject[] list = GameObject.FindGameObjectsWithTag("MainThing");
        if (list.Length > 1)
        {
            for (int i = 1; i < list.Length; i++)
            {
                Destroy(list[i]);
            }
        }
    }

    public void finalPlacement()
    {
        levelSolver.PlaceAllObjects();
    }

}
