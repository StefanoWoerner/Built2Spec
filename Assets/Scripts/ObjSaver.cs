using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_WSA
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

public static class ObjSaver
{
    /// <summary>
    /// The extension given to mesh files.
    /// </summary>
    private static string fileExtension = ".obj";

    /// <summary>
    /// Read-only property which returns the folder path where mesh files are stored.
    /// </summary>
    public static string MeshFolderName
    {
        get
        {
#if !UNITY_EDITOR && UNITY_WSA
                return ApplicationData.Current.RoamingFolder.Path;
#else
            return Application.persistentDataPath;
#endif
        }
    }

    public static int vertexCount;
    public static string Save(string fileName, IEnumerable<Mesh> meshes)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("Must specify a valid fileName.");
        }

        if (meshes == null)
        {
            throw new ArgumentNullException("Value of meshes cannot be null.");
        }

        // Create the mesh file.
        String folderName = MeshFolderName;
        Debug.Log(String.Format("Saving mesh file: {0}", Path.Combine(folderName, fileName + fileExtension)));

        vertexCount = 0;
        using (StreamWriter outputFile = new StreamWriter(OpenFileForWrite(folderName, fileName + fileExtension)))
        {

            int o = 0;
            foreach (Mesh theMesh in meshes)
            {
                o++;
                outputFile.WriteLine("o Object." + o);
                outputFile.Write(MeshToString(theMesh, vertexCount));
                outputFile.WriteLine("");
            }
        }

        Debug.Log("Mesh file saved.");

        return Path.Combine(folderName, fileName + fileExtension);
    }

    public static string Save(string fileName, IEnumerable<MeshFilter> meshFilters, String folderName = null, Stream stream = null)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("Must specify a valid fileName.");
        }

        if (meshFilters == null)
        {
            throw new ArgumentNullException("Value of meshFilters cannot be null.");
        }

        // Create the mesh file.
        folderName = folderName ?? MeshFolderName;
        Debug.Log(String.Format("Saving mesh file: {0}", Path.Combine(folderName, fileName + fileExtension)));

        vertexCount = 0;
        using (StreamWriter outputFile = new StreamWriter(stream ?? OpenFileForWrite(folderName, fileName + fileExtension)))
        {

            int o = 0;
            foreach (MeshFilter meshFilter in meshFilters)
            {
                o++;
                outputFile.WriteLine("o Object." + o);
                outputFile.Write(MeshToString(meshFilter.sharedMesh, vertexCount, meshFilter.transform));
                outputFile.WriteLine("");
            }
        }

        Debug.Log("Mesh file saved.");

        return Path.Combine(folderName, fileName + fileExtension);
    }

    public static string MeshToString(Mesh m, int lastVertexIndex = 0, Transform transform = null)
    {
        StringBuilder sb = new StringBuilder();

        vertexCount += m.vertices.Length;
        if (transform != null)
        {
            foreach (Vector3 v in m.vertices)
            {
                Vector3 vt = transform.TransformPoint(v);
                sb.Append(string.Format("v {0} {1} {2}\n", -vt.x, vt.y, vt.z));
            }
        }
        else
        {
            foreach (Vector3 v in m.vertices)
            {
                sb.Append(string.Format("v {0} {1} {2}\n", -v.x, v.y, v.z));
            }
        }
        
        //sb.Append("\n");
        //foreach (Vector3 v in m.normals)
        //{
        //    sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        //}
        //sb.Append("\n");
        //foreach (Vector3 v in m.uv)
        //{
        //    sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        //}
        for (int material = 0; material < m.subMeshCount; material++)
        {

            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                //faceCount += 3;
                sb.Append(string.Format("f {2} {1} {0}\n",
                    triangles[i] + 1 + lastVertexIndex, triangles[i + 1] + 1 + lastVertexIndex, triangles[i + 2] + 1 + lastVertexIndex));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Opens the specified file for reading.
    /// </summary>
    /// <param name="folderName">The name of the folder containing the file.</param>
    /// <param name="fileName">The name of the file, including extension. </param>
    /// <returns>Stream used for reading the file's data.</returns>
    private static Stream OpenFileForRead(string folderName, string fileName)
    {
        Stream stream = null;

#if !UNITY_EDITOR && UNITY_WSA
            Task<Task> task = Task<Task>.Factory.StartNew(
                            async () =>
                            {
                                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderName);
                                StorageFile file = await folder.GetFileAsync(fileName);
                                IRandomAccessStreamWithContentType randomAccessStream = await file.OpenReadAsync();
                                stream = randomAccessStream.AsStreamForRead();
                            });
            task.Wait();
            task.Result.Wait();
#else
        stream = new FileStream(Path.Combine(folderName, fileName), FileMode.Open, FileAccess.Read);
#endif
        return stream;
    }

    /// <summary>
    /// Opens the specified file for writing.
    /// </summary>
    /// <param name="folderName">The name of the folder containing the file.</param>
    /// <param name="fileName">The name of the file, including extension.</param>
    /// <returns>Stream used for writing the file's data.</returns>
    /// <remarks>If the specified file already exists, it will be overwritten.</remarks>
    private static Stream OpenFileForWrite(string folderName, string fileName)
    {
        Stream stream = null;

#if !UNITY_EDITOR && UNITY_WSA
            Task<Task> task = Task<Task>.Factory.StartNew(
                            async () =>
                            {
                                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderName);
                                StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                                IRandomAccessStream randomAccessStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                                stream = randomAccessStream.AsStreamForWrite();
                            });
            task.Wait();
            task.Result.Wait();
#else
        stream = new FileStream(Path.Combine(folderName, fileName), FileMode.Create, FileAccess.Write);
#endif
        return stream;
    }
}

