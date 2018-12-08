// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoloToolkit.Unity;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System.IO;

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
    public string meshFileName;
    public GameObject mapObject;

    void Start()
    {
        txt = textObjectState.GetComponentInChildren<TextMesh>();
    }

    protected override void InputDown(GameObject obj, InputEventData eventData)
    {
        Debug.Log(obj.name + " : InputDown");
        txt.text = obj.name + " : InputDown";

        switch (obj.name)
        {
            case "ToolbarButton1":
                Debug.Log("Saving...");
                //MeshSaver.Save(meshFileName, SpatialMappingManager.Instance.GetMeshFilters());
                ObjSaver.Save(meshFileName, SpatialMappingManager.Instance.GetMeshFilters());
                txt.text = "Mesh saved to file 1";
                break;

            case "ToolbarButton2":
                Debug.Log("Loading...");
                Debug.Log(Path.Combine(ObjSaver.MeshFolderName, meshFileName + ".obj"));
                txt.text = "1: " + Path.Combine(ObjSaver.MeshFolderName, meshFileName + ".obj");
                Material material2 = Resources.Load("defaultMat", typeof(Material)) as Material;
                OBJLoader.LoadOBJFile(Path.Combine(ObjSaver.MeshFolderName, meshFileName + ".obj"), material2, mapObject);
                txt.text = "2: " + Path.Combine(ObjSaver.MeshFolderName, meshFileName + ".obj");
                //int larifari = 0;
                //foreach (Transform child in go2.transform)
                //{
                //    MeshRenderer meshr = child.gameObject.GetComponent<MeshRenderer>();
                //    meshr.material = material2;
                //    larifari++;
                //}
                //txt.text = "Number: " + larifari;
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
