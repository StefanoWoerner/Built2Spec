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
    //public FileSurfaceObserver fso;
    public GameObject mapObject;
    public GameObject miniMapObject;
    public GameObject spatialMappingObject;
    public RenderDepthDifference rdd;
    public GameObject toolbarObject;
    public string defaultMeshFileName;
    private SolverHandler sh;
    private SolverRadialView srv;
    private TwoHandManipulatable thm;
    private GameObject tbButton_3_1_1;
    private GameObject tbButton_3_1_2;

    public GameObject aboutDialog;

    private bool buildMinimap;

    private bool save;
    private string saveFolderName;
    private string saveFileDisplayName;
    private Stream saveStream;
    private bool load;
    private string loadFolderName;
    private string loadFileDisplayName;
    private Stream loadStream;

    void Start()
    {
        txt = statusText.GetComponentInChildren<TextMesh>();
        rdd.enabled = false;
        sh = toolbarObject.GetComponent<SolverHandler>();
        srv = toolbarObject.GetComponent<SolverRadialView>();
        thm = miniMapObject.GetComponent<TwoHandManipulatable>();
        tbButton_3_1_1 = interactables.Find(x => x.name == "ToolbarButton3-1-1");
        tbButton_3_1_2 = interactables.Find(x => x.name == "ToolbarButton3-1-2");
        miniMapObject.SetActive(false);
        tbButton_3_1_1.SetActive(false);
        tbButton_3_1_2.SetActive(false);
        aboutDialog.SetActive(false);
    }

    void Update()
    {
        if (buildMinimap)
        {
            buildMinimap = false;

            foreach (Transform child in mapObject.transform)
            {
                Transform miniChild = Instantiate(child, miniMapObject.transform);
                miniChild.gameObject.AddComponent<BoxCollider>();
            }
        }

        if(load)
        {
            load = false;
            
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
            txt.text = "Mesh \"" + loadFileDisplayName + "\" loaded.";
        }

        if (save)
        {
            save = false;
#if !UNITY_EDITOR && UNITY_WSA
            ObjSaver.Save(saveFileDisplayName, SpatialMappingManager.Instance.GetMeshFilters(), saveFolderName, saveStream);
#else
            ObjSaver.Save(saveFileDisplayName, SpatialMappingManager.Instance.GetMeshFilters(), saveFolderName);
#endif
            txt.text = "Spatial mapping saved to " + saveFileDisplayName;
        }
    }

    protected override void InputDown(GameObject obj, InputEventData eventData)
    {
        Debug.Log(obj.name + " : InputDown");

        switch (obj.name)
        {
            case "ToolbarButton1":
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
#endif
                break;

            case "ToolbarButton2":
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

            case "ToolbarButton3":
                Debug.Log("Entering manipulation mode...");
                rdd.enabled = false;
                SetLayerRecursively(mapObject, LayerMask.NameToLayer("Default"));
                miniMapObject.SetActive(true);
                tbButton_3_1_1.SetActive(true);
                tbButton_3_1_2.SetActive(false);
                thm.ManipulationMode = ManipulationMode.MoveAndRotate;
                txt.text = "Pinch the minimap with both hands to transform the mesh.";
                break;

            case "ToolbarButton3-1-1":
                Debug.Log("Turning scaling on...");
                tbButton_3_1_1.SetActive(false);
                tbButton_3_1_2.SetActive(true);
                thm.ManipulationMode = ManipulationMode.MoveScaleAndRotate;
                break;

            case "ToolbarButton3-1-2":
                Debug.Log("Turning scaling off...");
                tbButton_3_1_2.SetActive(false);
                tbButton_3_1_1.SetActive(true);
                thm.ManipulationMode = ManipulationMode.MoveAndRotate;
                break;

            case "ToolbarButton4":
                rdd.enabled = true;
                miniMapObject.SetActive(false);
                tbButton_3_1_1.SetActive(false);
                tbButton_3_1_2.SetActive(false);
                SetLayerRecursively(mapObject, LayerMask.NameToLayer("ReferenceLayer"));
                txt.text = "";
                break;

            case "ToolbarButton5":
                Debug.Log("Changing UI lock...");
                sh.enabled = !sh.enabled;
                srv.enabled = !srv.enabled;
                txt.text = sh.enabled ? "UI unlocked" : "UI locked";
                break;

            case "ToolbarButton6":
                Debug.Log("Opening about dialog...");
                aboutDialog.SetActive(true);
                statusText.SetActive(false);
                break;
                
            case "CloseButton":
                Debug.Log("Opening about dialog...");
                aboutDialog.SetActive(false);
                statusText.SetActive(true);
                break;

            default:
                break;
        }
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
