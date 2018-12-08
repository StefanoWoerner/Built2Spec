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
    public GameObject textObjectState;
    private TextMesh txt;
    //public FileSurfaceObserver fso;
    public GameObject mapObject;
    public GameObject miniMapObject;
    public string defaultMeshFileName;

    private bool buildMinimap;

    void Start()
    {
        txt = textObjectState.GetComponentInChildren<TextMesh>();
        miniMapObject.SetActive(false);
    }

    void Update()
    {
        if (buildMinimap)
        {
            foreach (Transform child in mapObject.transform)
            {
                Transform miniChild = Instantiate(child, miniMapObject.transform);
                miniChild.gameObject.AddComponent<BoxCollider>();
            }

            buildMinimap = false;
        }
    }

    protected override void InputDown(GameObject obj, InputEventData eventData)
    {
        Debug.Log(obj.name + " : InputDown");

        switch (obj.name)
        {
            case "ToolbarButton1":
                Debug.Log("Saving spatial mapping...");
                txt.text = "Saving spatial mapping...";
                ObjSaver.Save(defaultMeshFileName, SpatialMappingManager.Instance.GetMeshFilters());
                txt.text = "Mesh saved to " + defaultMeshFileName;
                break;

            case "ToolbarButton2":
                Debug.Log("Loading from " + Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj"));
                txt.text = "Loading from " + Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj");

                foreach (Transform child in mapObject.transform)
                {
                    Destroy(child.gameObject);
                }
                foreach (Transform child in miniMapObject.transform)
                {
                    Destroy(child.gameObject);
                }

                Material material = Resources.Load("defaultMat", typeof(Material)) as Material;
                OBJLoader.LoadOBJFile(Path.Combine(ObjSaver.MeshFolderName, defaultMeshFileName + ".obj"), material, mapObject);
                buildMinimap = true;
                txt.text = "Mesh loaded from " + defaultMeshFileName;
                break;

            case "ToolbarButton3":
                miniMapObject.SetActive(true);
                txt.text = "Pinch the minimap with both hands to transform the mesh." + defaultMeshFileName;
                break;

            case "ToolbarButton4":
                Debug.Log("Loading...");
#if !UNITY_EDITOR && UNITY_WSA
                //Task<Task> task = Task<Task>.Factory.StartNew(
                //        async () =>
                //        {
                //            FileOpenPicker openPicker = new FileOpenPicker();
                //            openPicker.ViewMode = PickerViewMode.Thumbnail;
                //            openPicker.SuggestedStartLocation = PickerLocationId.Objects3D;
                //            openPicker.FileTypeFilter.Add(".obj");
                //            StorageFile file = await openPicker.PickSingleFileAsync();
                //            //StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderName);
                //            //StorageFile file = await folder.GetFileAsync(fileName);
                //            IRandomAccessStreamWithContentType randomAccessStream = await file.OpenReadAsync();
                //            stream = randomAccessStream.AsStreamForRead();
                //        });
                //task.Wait();
                //task.Result.Wait();
                //GameObject go3 = OBJLoader.LoadOBJFile(OpenFileAsync());
                OpenFileAsync();
#else
                Debug.Log("Hello");
#endif
                break;

            default:
                break;
        }
    }

#if !UNITY_EDITOR && UNITY_WSA
    private async void OpenFileAsync()
    {
        UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Objects3D;
            openPicker.FileTypeFilter.Add(".obj");

            StorageFile file = await openPicker.PickSingleFileAsync();
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                if (file != null)
                {
                    // Application now has read/write access to the picked file
                    Debug.Log("Picked file: " + file.DisplayName + ", Path: " + file.Path);
                    txt.text = "Picked file: " + file.DisplayName + ", Path: " + file.Path;
                }
                else
                {
                    // The picker was dismissed with no selected file
                    Debug.Log("File picker operation cancelled");
                    txt.text = "File picker operation cancelled";
                }


            }, true);


        }, false);

    }
#endif

}
