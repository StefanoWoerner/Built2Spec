// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System.IO;
using HoloToolkit.Unity.InputModule.Utilities.Interactions;

#if !UNITY_EDITOR && UNITY_WSA
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

public class ButtonReceiver : InteractionReceiver
{
    /// <summary>
    /// The status text game object
    /// </summary>
    public GameObject statusText;

    private TextMesh txt;

    /// <summary>
    /// The game object holding the loaded model
    /// </summary>
    public GameObject mapObject;

    /// <summary>
    /// The game object holding the mini map
    /// </summary>
    public GameObject miniMapObject;

    private Vector3 initialMiniMapScale;

    /// <summary>
    /// The spatial mapping game object (from HoloToolkit)
    /// </summary>
    public GameObject spatialMappingObject;

    /// <summary>
    /// The rendering script for difference reasoning
    /// </summary>
    public RenderDepthDifference rdd;

    /// <summary>
    /// The toolbar game object
    /// </summary>
    public GameObject toolbarObject;
    
    /// <summary>
    /// Holds the default name for saving spatial mappings.
    /// </summary>
    public string defaultMeshFileName;

    // Scripts for floating toolbar
    private SolverHandler sh;
    private SolverRadialView srv;

    // Scripts for mini map / manipulation
    private TwoHandManipulatable thm;
    private FollowTransformations ft;

    // Various toolbar buttons which need their visibility toggled.
    private GameObject tbManipulate_1_1;
    private GameObject tbManipulate_1_2;
    private GameObject tbManipulate_2_1;
    private GameObject tbManipulate_2_2;
    private GameObject tbManipulate_2_3;
    private GameObject tbManipulate_3_1;
    private GameObject tbModelOverlay_1_1;
    private GameObject tbModelOverlay_1_2;
    private GameObject tbModelOverlay_1_3;

    /// <summary>
    /// A game object holding the "About" dialog
    /// </summary>
    public GameObject aboutDialog;

    // Flags for the update method to perform actions on next frame
    private bool buildMinimap;
    private int manipulationModeCountdown = 0;
    private bool save;
    private bool load;

    // Holds file names and streams when saving or loading
    private string saveFileDisplayName;
    private Stream saveStream;
    private string loadFileDisplayName;
    private Stream loadStream;

    // Toggle statuses
    private bool spatialMappingActive;
    private bool yUp = true;

    /// <summary>
    /// Sets whether the spatial mapping game object is active.
    /// </summary>
    private bool SpatialMappingActive
    {
        get
        {
            return spatialMappingActive;
        }
        set
        {
            spatialMappingObject.SetActive(value);
            spatialMappingActive = value;
        }
    }

    /// <summary>
    /// Start method fills the fields.
    /// </summary>
    void Start()
    {
        // Find elements and fill fields
        txt = statusText.GetComponentInChildren<TextMesh>();
        initialMiniMapScale = miniMapObject.transform.localScale;
        sh = toolbarObject.GetComponent<SolverHandler>();
        srv = toolbarObject.GetComponent<SolverRadialView>();
        thm = miniMapObject.GetComponent<TwoHandManipulatable>();
        ft = mapObject.GetComponent<FollowTransformations>();
        tbManipulate_1_1 = interactables.Find(x => x.name == "ToolbarManipulate-1-1");
        tbManipulate_1_2 = interactables.Find(x => x.name == "ToolbarManipulate-1-2");
        tbManipulate_2_1 = interactables.Find(x => x.name == "ToolbarManipulate-2-1");
        tbManipulate_2_2 = interactables.Find(x => x.name == "ToolbarManipulate-2-2");
        tbManipulate_2_3 = interactables.Find(x => x.name == "ToolbarManipulate-2-3");
        tbManipulate_3_1 = interactables.Find(x => x.name == "ToolbarManipulate-3-1");
        tbModelOverlay_1_1 = interactables.Find(x => x.name == "ToolbarModelOverlay-1-1");
        tbModelOverlay_1_2 = interactables.Find(x => x.name == "ToolbarModelOverlay-1-2");
        tbModelOverlay_1_3 = interactables.Find(x => x.name == "ToolbarModelOverlay-1-3");

        // Start in model overlay mode
        leaveManipulate();
        enterModelOverlay();
        rdd.enabled = false;
        aboutDialog.SetActive(false);
    }

    /// <summary>
    /// Update method performs actions each frame.
    /// </summary>
    void Update()
    {
        if (buildMinimap)
        {
            buildMinimap = false;
            yUp = true;

            foreach (Transform child in mapObject.transform)
            {
                Transform miniChild = Instantiate(child, miniMapObject.transform);
                miniChild.gameObject.AddComponent<BoxCollider>();
            }
        }

        if (manipulationModeCountdown >= 1)
        {
            manipulationModeCountdown--;
            if (manipulationModeCountdown == 0)
            {
                enterManipulate();
            }
        }

        if (save)
        {
            save = false;

            ObjSaver.Save(SpatialMappingManager.Instance.GetMeshFilters(), saveStream);
            spatialMappingObject.SetActive(SpatialMappingActive);
            txt.text = "Spatial mapping saved to " + saveFileDisplayName;
        }

        if (load)
        {
            load = false;

            miniMapObject.transform.localScale = initialMiniMapScale;

            // empty map and mini map
            foreach (Transform child in mapObject.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in miniMapObject.transform)
            {
                Destroy(child.gameObject);
            }

            Material material = Resources.Load("defaultMat", typeof(Material)) as Material;
            
            ObjLoader.LoadOBJFile(loadFileDisplayName, material, loadStream, mapObject);

            buildMinimap = true;
            leaveManipulate();
            enterModelOverlay();
            rdd.enabled = false;
            txt.text = "Mesh \"" + loadFileDisplayName + "\" loaded.";
        }
    }

    /// <summary>
    /// InputClicked method listens for UI interactions.
    /// </summary>
    protected override void InputClicked(GameObject obj, InputClickedEventData eventData)
    {
        Debug.Log(obj.name + " : InputClicked");

        switch (obj.name)
        {
            case "ToolbarSave":
                OpenStreamForSave();
                break;

            case "ToolbarLoad":
                OpenStreamForLoad();
                break;

            case "ToolbarManipulate":
                Debug.Log("Entering manipulation mode...");
                rdd.enabled = false;
                leaveModelOverlay();
                miniMapObject.transform.position = toolbarObject.transform.position + new Vector3(0, 0.3f, 0);
                ft.movementMode = FollowTransformations.MovementMode.None;
                manipulationModeCountdown = 2;
                txt.text = "Pinch the minimap with both hands to transform the mesh.";
                break;

            case "ToolbarManipulate-1-1":
                Debug.Log("Turning scaling on...");
                tbManipulate_1_1.SetActive(false);
                tbManipulate_1_2.SetActive(true);
                thm.ManipulationMode = ManipulationMode.MoveScaleAndRotate;
                break;

            case "ToolbarManipulate-1-2":
                Debug.Log("Turning scaling off...");
                tbManipulate_1_1.SetActive(true);
                tbManipulate_1_2.SetActive(false);
                thm.ManipulationMode = ManipulationMode.MoveAndRotate;
                break;

            case "ToolbarManipulate-2-1":
                Debug.Log("Turning movement to scale...");
                tbManipulate_2_1.SetActive(false);
                tbManipulate_2_2.SetActive(true);
                tbManipulate_2_3.SetActive(false);
                ft.movementMode = FollowTransformations.MovementMode.ToScale;
                break;

            case "ToolbarManipulate-2-2":
                Debug.Log("Turning movement off...");
                tbManipulate_2_1.SetActive(false);
                tbManipulate_2_2.SetActive(false);
                tbManipulate_2_3.SetActive(true);
                ft.movementMode = FollowTransformations.MovementMode.None;
                break;

            case "ToolbarManipulate-2-3":
                Debug.Log("Turning movement 1:1...");
                tbManipulate_2_1.SetActive(true);
                tbManipulate_2_2.SetActive(false);
                tbManipulate_2_3.SetActive(false);
                ft.movementMode = FollowTransformations.MovementMode.Default;
                break;

            case "ToolbarManipulate-3-1":
                Debug.Log("Flipping Y and Z...");
                miniMapObject.transform.Rotate(yUp ? 270 : 90, 0, 0);
                yUp = !yUp;
                break;

            case "ToolbarModelOverlay":
                Debug.Log("Model Overlay view...");
                rdd.enabled = false;
                leaveManipulate();
                enterModelOverlay();
                txt.text = "";
                break;

            case "ToolbarModelOverlay-1-1":
                Debug.Log("Show Model only...");
                tbModelOverlay_1_1.SetActive(false);
                tbModelOverlay_1_2.SetActive(true);
                tbModelOverlay_1_3.SetActive(false);
                SetLayerRecursively(mapObject, LayerMask.NameToLayer("Default"));
                SpatialMappingActive = false;
                break;

            case "ToolbarModelOverlay-1-2":
                Debug.Log("Show Spatial Mapping only...");
                tbModelOverlay_1_1.SetActive(false);
                tbModelOverlay_1_2.SetActive(false);
                tbModelOverlay_1_3.SetActive(true);
                SetLayerRecursively(mapObject, LayerMask.NameToLayer("ReferenceLayer"));
                SpatialMappingActive = true;
                break;

            case "ToolbarModelOverlay-1-3":
                Debug.Log("Show Model and Spatial Mapping...");
                enterModelOverlay();
                break;

            case "ToolbarDifferenceReasoning":
                leaveManipulate();
                leaveModelOverlay();
                rdd.enabled = true;
                SetLayerRecursively(mapObject, LayerMask.NameToLayer("ReferenceLayer"));
                SpatialMappingActive = true;
                txt.text = "";
                break;

            case "ToolbarLockToggle":
                Debug.Log("Changing UI lock...");
                sh.enabled = !sh.enabled;
                srv.enabled = !srv.enabled;
                txt.text = sh.enabled ? "UI unlocked" : "UI locked";
                break;

            case "ToolbarAbout":
                Debug.Log("Opening about dialog...");
                aboutDialog.SetActive(true);
                statusText.SetActive(false);
                break;
                
            case "CloseButton":
                Debug.Log("Closing about dialog...");
                aboutDialog.SetActive(false);
                statusText.SetActive(true);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Performs necessary UI/view settings for starting manipulation mode.
    /// </summary>
    private void enterManipulate()
    {
        miniMapObject.SetActive(true);
        SetLayerRecursively(mapObject, LayerMask.NameToLayer("Default"));
        SpatialMappingActive = false;

        tbManipulate_1_1.SetActive(true);
        tbManipulate_1_2.SetActive(false);
        thm.ManipulationMode = ManipulationMode.MoveAndRotate;
        tbManipulate_2_1.SetActive(true);
        tbManipulate_2_2.SetActive(false);
        tbManipulate_2_3.SetActive(false);
        ft.movementMode = FollowTransformations.MovementMode.Default;
        tbManipulate_3_1.SetActive(true);
    }

    /// <summary>
    /// Performs necessary UI/view settings for leaving manipulation mode.
    /// </summary>
    private void leaveManipulate()
    {
        miniMapObject.SetActive(false);
        tbManipulate_1_1.SetActive(false);
        tbManipulate_1_2.SetActive(false);
        tbManipulate_2_1.SetActive(false);
        tbManipulate_2_2.SetActive(false);
        tbManipulate_2_3.SetActive(false);
        tbManipulate_3_1.SetActive(false);
    }

    /// <summary>
    /// Performs necessary UI/view settings for starting model overlay mode.
    /// </summary>
    private void enterModelOverlay()
    {
        tbModelOverlay_1_1.SetActive(true);
        tbModelOverlay_1_2.SetActive(false);
        tbModelOverlay_1_3.SetActive(false);
        SetLayerRecursively(mapObject, LayerMask.NameToLayer("Default"));
        SpatialMappingActive = true;
    }

    /// <summary>
    /// Performs necessary UI/view settings for leaving model overlay mode.
    /// </summary>
    private void leaveModelOverlay()
    {
        tbModelOverlay_1_1.SetActive(false);
        tbModelOverlay_1_2.SetActive(false);
        tbModelOverlay_1_3.SetActive(false);
    }

    /// <summary>
    /// Assigns a game object and all it's children to a layer.
    /// </summary>
    private static void SetLayerRecursively(GameObject go, int newLayer)
    {
        go.layer = newLayer;

        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// Opens a file for loading and sets the load flag for the next frame.
    /// </summary>
    private void OpenStreamForLoad()
    {
        loadFileDisplayName = defaultMeshFileName;

#if !UNITY_EDITOR && UNITY_WSA
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Objects3D;
            openPicker.FileTypeFilter.Add(ObjSaver.fileExtension);

            StorageFile file = await openPicker.PickSingleFileAsync();
            IRandomAccessStreamWithContentType randomAccessStream = await file.OpenReadAsync();
            loadStream = randomAccessStream.AsStreamForRead();
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                if (file != null)
                {
                    // Application now has read/write access to the picked file
                    Debug.Log("Loading from " + file.Path);
                    loadFileDisplayName = Path.GetFileNameWithoutExtension(file.Path);
                    load = true;
                }
                else
                {
                    // The picker was dismissed with no selected file
                    Debug.Log("File picker operation cancelled");
                    txt.text = "File picker operation cancelled";
                }

            }, true);

        }, true);
#else
        string path = Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ObjSaver.fileExtension);
        loadStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        Debug.Log("Loading from " + path);
        load = true;
#endif
    }

    /// <summary>
    /// Opens a file for saving and sets the save flag for the next frame.
    /// </summary>
    private void OpenStreamForSave()
    {
        saveFileDisplayName = defaultMeshFileName;

#if !UNITY_EDITOR && UNITY_WSA
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Objects3D;
            savePicker.FileTypeChoices.Add("Wavefront OBJ", new List<string>() { ObjSaver.fileExtension });
            savePicker.SuggestedFileName = "SpatialMappingMesh";

            StorageFile file = await savePicker.PickSaveFileAsync();
            IRandomAccessStream randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite);
            saveStream = randomAccessStream.AsStreamForWrite();
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                if (file != null)
                {
                    // Application now has read/write access to the picked file
                    Debug.Log("Saving to " + file.Path);
                    saveFileDisplayName = Path.GetFileNameWithoutExtension(file.Path);
                    save = true;
                    spatialMappingObject.SetActive(true);
                }
                else
                {
                    // The picker was dismissed with no selected file
                    Debug.Log("File picker operation cancelled");
                    txt.text = "File picker operation cancelled";
                }

            }, true);

        }, true);
#else
        string path = Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ObjSaver.fileExtension);
        saveStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        Debug.Log("Saving to " + path);
        save = true;
        spatialMappingObject.SetActive(true);
#endif
    }
}
