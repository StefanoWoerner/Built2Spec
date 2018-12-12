// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System.IO;
using HoloToolkit.Unity.UX;
using HoloToolkit.Unity.InputModule.Utilities.Interactions;
using HoloToolkit.UX.Dialog;

#if WINDOWS_UWP

using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;

#endif

public class ButtonReceiver : InteractionReceiver
{
    public GameObject statusText;
    private TextMesh txt;

    public GameObject mapObject;
    public GameObject miniMapObject;
    private Vector3 initialMiniMapScale;
    public GameObject spatialMappingObject;
    public RenderDepthDifference rdd;
    public GameObject toolbarObject;
    public string defaultMeshFileName;
    private SolverHandler sh;
    private SolverRadialView srv;
    private TwoHandManipulatable thm;
    private FollowTransformations ft;
    private GameObject tbManipulate_1_1;
    private GameObject tbManipulate_1_2;
    private GameObject tbManipulate_2_1;
    private GameObject tbManipulate_2_2;
    private GameObject tbManipulate_2_3;
    private GameObject tbManipulate_3_1;
    private GameObject tbModelOverlay_1_1;
    private GameObject tbModelOverlay_1_2;
    private GameObject tbModelOverlay_1_3;

    public GameObject aboutDialog;

    private bool buildMinimap;
    private int manipulationModeCountdown = 0;

    private bool save;
    private string saveFolderName;
    private string saveFileDisplayName;
    private Stream saveStream;
    private bool load;
    private string loadFolderName;
    private string loadFileDisplayName;
    private Stream loadStream;

    private bool spatialMappingActive;
    private bool yUp = true;

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

    void Start()
    {
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
        leaveManipulate();
        enterModelOverlay();
        rdd.enabled = false;
        aboutDialog.SetActive(false);
    }

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
#if !UNITY_EDITOR && UNITY_WSA
            ObjSaver.Save(saveFileDisplayName, SpatialMappingManager.Instance.GetMeshFilters(), saveFolderName, saveStream);
#else
            ObjSaver.Save(saveFileDisplayName, SpatialMappingManager.Instance.GetMeshFilters(), saveFolderName);
#endif
            spatialMappingObject.SetActive(SpatialMappingActive);
            txt.text = "Spatial mapping saved to " + saveFileDisplayName;
        }

        if (load)
        {
            load = false;

            miniMapObject.transform.localScale = initialMiniMapScale;

            foreach (Transform child in mapObject.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in miniMapObject.transform)
            {
                Destroy(child.gameObject);
            }

            Material material = Resources.Load("defaultMat", typeof(Material)) as Material;

#if !UNITY_EDITOR && UNITY_WSA
            List<string> lines = new List<string>();

            using (StreamReader r = new StreamReader(loadStream))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            OBJLoader.LoadOBJFile(Path.Combine(loadFolderName, loadFileDisplayName + ".obj"), material, mapObject, lines.ToArray());
#else
            OBJLoader.LoadOBJFile(Path.Combine(loadFolderName, loadFileDisplayName + ".obj"), material, mapObject);
#endif
            buildMinimap = true;
            leaveManipulate();
            enterModelOverlay();
            rdd.enabled = false;
            txt.text = "Mesh \"" + loadFileDisplayName + "\" loaded.";
        }
    }

    protected override void InputClicked(GameObject obj, InputClickedEventData eventData)
    {
        Debug.Log(obj.name + " : InputDown");

        switch (obj.name)
        {
            case "ToolbarSave":
                saveFolderName = ObjSaver.MeshFolderName;
                saveFileDisplayName = defaultMeshFileName;

#if !UNITY_EDITOR && UNITY_WSA
                UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
                {
                    FileSavePicker savePicker = new FileSavePicker();
                    savePicker.SuggestedStartLocation = PickerLocationId.Objects3D;
                    savePicker.FileTypeChoices.Add("Wavefront OBJ", new List<string>() { ".obj" });
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
                            saveFolderName = Path.GetDirectoryName(file.Path);
                            saveFileDisplayName = Path.GetFileNameWithoutExtension(file.Path);
                            //txt.text = "Saving to " + file.Path + ", " + saveFolderName + ", " + saveFileDisplayName;
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
                Debug.Log("Saving to " + Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj"));
                txt.text = "Saving to " + Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj");
                save = true;
                spatialMappingObject.SetActive(true);
#endif
                break;

            case "ToolbarLoad":
                rdd.enabled = false;
                
                loadFolderName = ObjSaver.MeshFolderName;
                loadFileDisplayName = defaultMeshFileName;

#if !UNITY_EDITOR && UNITY_WSA
                UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
                {
                    FileOpenPicker openPicker = new FileOpenPicker();
                    openPicker.ViewMode = PickerViewMode.Thumbnail;
                    openPicker.SuggestedStartLocation = PickerLocationId.Objects3D;
                    openPicker.FileTypeFilter.Add(".obj");

                    StorageFile file = await openPicker.PickSingleFileAsync();
                    IRandomAccessStreamWithContentType randomAccessStream = await file.OpenReadAsync();
                    loadStream = randomAccessStream.AsStreamForRead();
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        if (file != null)
                        {
                            // Application now has read/write access to the picked file
                            Debug.Log("Loading from " + file.Path);
                            //txt.text = "Loading from " + file.Path;
                            loadFolderName = Path.GetDirectoryName(file.Path);
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
                Debug.Log("Loading from " + Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj"));
                txt.text = "Loading from " + Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj");
                load = true;
#endif
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

    private void enterModelOverlay()
    {
        tbModelOverlay_1_1.SetActive(true);
        tbModelOverlay_1_2.SetActive(false);
        tbModelOverlay_1_3.SetActive(false);
        SetLayerRecursively(mapObject, LayerMask.NameToLayer("Default"));
        SpatialMappingActive = true;
    }

    private void leaveModelOverlay()
    {
        tbModelOverlay_1_1.SetActive(false);
        tbModelOverlay_1_2.SetActive(false);
        tbModelOverlay_1_3.SetActive(false);
    }

    private static void SetLayerRecursively(GameObject go, int newLayer)
    {
        go.layer = newLayer;

        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

}
