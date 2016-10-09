// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using System.Collections;
using HoloToolkit.Unity;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

public class LevelSolver : MonoBehaviour
{
    // Singleton
    public static LevelSolver Instance;

    // Enums
    public enum QueryStates
    {
        None,
        Processing,
        Finished
    }

    public class ObjectToInstantiate
    {
        public ObjectToInstantiate(GameObject input, Vector3 position, Quaternion rotation)
        {
            Object = input;
            Position = position;
            Rotation = rotation;
        }

        public GameObject Object;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public List<ObjectToInstantiate> objectsToMake = new List<ObjectToInstantiate>();

    // Structs
    private struct QueryStatus
    {
        public void Reset()
        {
            State = QueryStates.None;
            Name = "";
            CountFail = 0;
            CountSuccess = 0;
            QueryResult = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult>();
        }

        public QueryStates State;
        public string Name;
        public int CountFail;
        public int CountSuccess;
        public List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult> QueryResult;
    }

    private struct PlacementQuery
    {
        public PlacementQuery(
            GameObject input,
            SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition,
            List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> placementRules = null,
            List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint> placementConstraints = null)
        {
            Input = input;
            PlacementDefinition = placementDefinition;
            PlacementRules = placementRules;
            PlacementConstraints = placementConstraints;
        }

        public GameObject Input;
        public SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition PlacementDefinition;
        public List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> PlacementRules;
        public List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint> PlacementConstraints;
    }

    private class PlacementResult
    {
        public PlacementResult(GameObject input, float timeDelay, SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult result)
        {
            Input = input;
            Position = result.Position;
            Rotation = Quaternion.LookRotation(result.Forward, result.Up);
            Result = result;
        }
        public GameObject Input;
        public LineDrawer.AnimatedBox Box;
        public Vector3 Position;
        public Quaternion Rotation;
        public SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult Result;
    }

    // Properties
    public bool IsSolverInitialized { get; private set; }

    // Privates
    private List<PlacementResult> placementResults = new List<PlacementResult>();
    private QueryStatus queryStatus = new QueryStatus();

    // Functions
    private void Awake()
    {
        Instance = this;
    }

    private bool PlaceObjectAsync(
        GameObject input,
        string placementName,
        SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition,
        List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> placementRules = null,
        List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint> placementConstraints = null,
        bool clearObjectsFirst = true)
    {
        return PlaceObjectAsync(
            placementName,
            new List<PlacementQuery>() { new PlacementQuery(input, placementDefinition, placementRules, placementConstraints) },
            clearObjectsFirst);
    }

    private bool PlaceObjectAsync(
        string placementName,
        List<PlacementQuery> placementList,
        bool clearObjectsFirst = true)
    {
        // If we already mid-query, reject the request
        if (queryStatus.State != QueryStates.None)
        {
            return false;
        }

        // Mark it
        queryStatus.Reset();
        queryStatus.State = QueryStates.Processing;
        queryStatus.Name = placementName;

        // Tell user we are processing
        AppState.Instance.ObjectPlacementDescription = placementName + " (processing)";

            // Go through the queries in the list
            for (int i = 0; i < placementList.Count; ++i)
            {
                // Do the query
                bool success = PlaceObject(
                    placementName,
                    placementList[i].Input,
                    placementList[i].PlacementDefinition,
                    placementList[i].PlacementRules,
                    placementList[i].PlacementConstraints, 
                    clearObjectsFirst, 
                    true);

                // Mark the result
                queryStatus.CountSuccess = success ? (queryStatus.CountSuccess + 1) : queryStatus.CountSuccess;
                queryStatus.CountFail = !success ? (queryStatus.CountFail + 1) : queryStatus.CountFail;
            }

            // Done
            queryStatus.State = QueryStates.Finished;

        return true;
    }

    private bool PlaceObject(
        string placementName,
        GameObject input,
        SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition,
        List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> placementRules = null,
        List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint> placementConstraints = null,
        bool clearObjectsFirst = true,
        bool isASync = false)
    {
        isASync = false;
        // Clear objects (if requested)
        if (!isASync && clearObjectsFirst)
        {
        }
        if (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
        {
            return false;
        }

        // New query
        if (SpatialUnderstandingDllObjectPlacement.Solver_PlaceObject(
                placementName,
                SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementDefinition),
                (placementRules != null) ? placementRules.Count : 0,
                ((placementRules != null) && (placementRules.Count > 0)) ? SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementRules.ToArray()) : IntPtr.Zero,
                (placementConstraints != null) ? placementConstraints.Count : 0,
                ((placementConstraints != null) && (placementConstraints.Count > 0)) ? SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementConstraints.ToArray()) : IntPtr.Zero,
                SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResultPtr()) > 0)
        {
            SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult placementResult = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResult();
            if (!isASync)
            {
                // Add to instantiation list here

                // If not running async, we can just add the results to the draw list right now
                AppState.Instance.ObjectPlacementDescription = placementName + " (1)";

                placementResults.Add(new PlacementResult(input, 1.0f, placementResult.Clone() as SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult));
                Quaternion thisRotation = Quaternion.LookRotation(placementResult.Forward, placementResult.Up);
                //print("Space Found");
                objectsToMake.Add(new ObjectToInstantiate(input, placementResult.Position, thisRotation));
                //print(objectsToMake.Count);
            }
            else
            {
                queryStatus.QueryResult.Add(placementResult.Clone() as SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult);
            }
            return true;
        }
        if (!isASync)
        {
            print("No Spaces Found!");
            AppState.Instance.ObjectPlacementDescription = "Placement Failed";
        }
        return false;
    }

    public void Query_OnFloor(GameObject input)
    {

        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 4; ++i)
        {
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloor(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                    new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(1.5f*input.transform.lossyScale.x),
                                    }));
        }
        PlaceObjectAsync("OnFloor", placementQuery);
    }

    public void Query_OnWall(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 6; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.3f, 0.6f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnWall(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 4, 0.05f), 0.5f, 3.0f),
                                    new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.y/2 * 4.0f),
                                    }));
        }
        PlaceObjectAsync("OnWall", placementQuery);
    }

    public void Query_OnCeiling(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 2; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.3f, 0.4f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnCeiling(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                    new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(1.5f*input.transform.lossyScale.x),
                                    }));
        }
        PlaceObjectAsync("OnCeiling", placementQuery);
    }

    public void Query_OnEdge(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 8; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.05f, 0.1f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnEdge(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2),
                                                                                                                  new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                    new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.x * 1.5f),
                                    }));
        }
        PlaceObjectAsync("OnEdge", placementQuery);
    }

    public void Query_OnFloorAndCeiling(GameObject input)
    {
        SpatialUnderstandingDll.Imports.QueryPlayspaceAlignment(SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceAlignmentPtr());
        SpatialUnderstandingDll.Imports.PlayspaceAlignment alignment = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceAlignment();
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 4; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.1f, 0.2f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloorAndCeiling(new Vector3(input.transform.lossyScale.x / 2, (alignment.CeilingYValue - alignment.FloorYValue) * 0.5f, input.transform.lossyScale.z / 2),
                                                                                                                         new Vector3(input.transform.lossyScale.x / 2, (alignment.CeilingYValue - alignment.FloorYValue) * 0.5f, input.transform.lossyScale.z / 2)),
                                    new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.x * 1.5f),
                                    }));
        }
        PlaceObjectAsync("OnFloorAndCeiling", placementQuery);
    }

    public void Query_RandomInAir_AwayFromMe(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 8; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.1f, 0.2f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_RandomInAir(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                    new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.x*1.5f),
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromPosition(Camera.main.transform.position, 2.5f),
                                    }));
        }
        PlaceObjectAsync("RandomInAir - AwayFromMe", placementQuery);
    }

    public void Query_OnEdge_NearCenter(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 4; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.05f, 0.1f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnEdge(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2), new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                   new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                            SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.x * 1.5f),
                                   },
                                   new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint>() {
                                           SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint.Create_NearCenter(),
                                   }));
        }
        PlaceObjectAsync("OnEdge - NearCenter", placementQuery);

    }

    public void Query_OnFloor_AwayFromMe(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 4; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.05f, 0.15f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloor(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                   new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                           SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromPosition(Camera.main.transform.position, 2.0f),
                                           SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.x*1.5f),
                                   }));
        }
        PlaceObjectAsync("OnFloor - AwayFromMe", placementQuery);
    }

    public void Query_OnFloor_NearMe(GameObject input)
    {
        List<PlacementQuery> placementQuery = new List<PlacementQuery>();
        for (int i = 0; i < 4; ++i)
        {
            float halfDimSize = UnityEngine.Random.Range(0.05f, 0.2f);
            placementQuery.Add(
                new PlacementQuery(input, SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloor(new Vector3(input.transform.lossyScale.x / 2, input.transform.lossyScale.y / 2, input.transform.lossyScale.z / 2)),
                                   new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>() {
                                           SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(input.transform.lossyScale.x * 1.5f),
                                   },
                                   new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint>() {
                                           SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint.Create_NearPoint(Camera.main.transform.position, 0.5f, 2.0f)
                                   }));
        }
        PlaceObjectAsync("OnFloor - NearMe", placementQuery);
    }

    public bool InitializeSolver()
    {
        if (IsSolverInitialized ||
            !SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
        {
            return IsSolverInitialized;
        }

        if (SpatialUnderstandingDllObjectPlacement.Solver_Init() > 1)
        {
            IsSolverInitialized = true;
        }
        return IsSolverInitialized;
    }

    private void Update()
    {
        // Can't do any of this till we're done with the scanning phase
        if (SpatialUnderstanding.Instance.ScanState != SpatialUnderstanding.ScanStates.Done)
        {
            return;
        }

        // Make sure the solver has been initialized
        if (!IsSolverInitialized &&
            SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
        {
            InitializeSolver();
        }
    }
    public void PlaceAllObjects()
    {
        foreach (ObjectToInstantiate result in objectsToMake)
        {
            print("Instantiating Object" + result.Object.name);
            GameObject temp = (GameObject)Instantiate(result.Object, result.Position, result.Rotation);
            if (temp.CompareTag("FloorObj"))
            {
                temp.transform.LookAt(Camera.main.transform);
                temp.transform.up = Vector3.up;
            }
            print("Instantiated");
        }

    }
}
