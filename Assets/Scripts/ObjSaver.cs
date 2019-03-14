using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_WSA
using Windows.Storage;
#endif

/// <summary>
/// Class that handles saving of meshes to .obj files.
/// </summary>
public static class ObjSaver
{
    /// <summary>
    /// The extension given to mesh files.
    /// </summary>
    public static string fileExtension = ".obj";

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

    // global counter
    public static int vertexCount;

    /// <summary>
    /// Saves a collection of mesh filters to a file.
    /// </summary>
    public static void Save(IEnumerable<MeshFilter> meshFilters, Stream saveStream)
    {
        if (saveStream == null)
        {
            throw new ArgumentException("Must pass a valid stream.");
        }

        if (meshFilters == null)
        {
            throw new ArgumentNullException("Value of meshFilters cannot be null.");
        }

        // Create the mesh file
        vertexCount = 0;
        using (StreamWriter outputFile = new StreamWriter(saveStream))
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
    }

    /// <summary>
    /// Serializes a mesh.
    /// </summary>
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
    
}

