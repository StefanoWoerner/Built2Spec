/**
 * Original by AARO4130
 * (C) 2015 AARO4130
 * DO NOT USE PARTS OF, OR THE ENTIRE SCRIPT, AND CLAIM AS YOUR OWN WORK
 * 
 * Adapted by Stefano Woerner (Computer Vision and Geometry Group, ETH Zurich)
 */

using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjLoader
{
    public static bool splitByMaterial = false;
    public static string[] searchPaths = new string[] { "", "%FileName%_Textures" + Path.DirectorySeparatorChar };
    
    //structures
    struct OBJFace
    {
        public string materialName;
        public string meshName;
        public int[] indexes;
    }
    
    public static Vector3 ParseVectorFromCMPS(string[] cmps)
    {
        float x = float.Parse(cmps[1]);
        float y = float.Parse(cmps[2]);
        if (cmps.Length == 4)
        {
            float z = float.Parse(cmps[3]);
            return new Vector3(x, y, z);
        }
        return new Vector2(x, y);
    }

    public static GameObject LoadOBJFile(string meshName, Material defaultMaterial, Stream loadStream, GameObject parentObject = null)
    {
        
        bool hasNormals = false;
        //OBJ LISTS
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        //UMESH LISTS
        List<Vector3> uvertices = new List<Vector3>();
        List<Vector3> unormals = new List<Vector3>();
        List<Vector2> uuvs = new List<Vector2>();
        //MESH CONSTRUCTION
        List<string> materialNames = new List<string>();
        List<string> objectNames = new List<string>();
        Dictionary<string,int> hashtable = new Dictionary<string, int>();
        List<OBJFace> faceList = new List<OBJFace>();
        string cmaterial = "";
        materialNames.Add(cmaterial);
        string cmesh = "default";
        //CACHE
        Material[] materialCache = null;
         
        foreach (string ln in ReadAllLines(loadStream))
        {
            if (ln.Length > 0 && ln[0] != '#')
            {
                string l = ln.Trim().Replace("  "," ");
                string[] cmps = l.Split(' ');
                string data = l.Remove(0, l.IndexOf(' ') + 1);

                if (cmps[0] == "mtllib")
                {
                    //do nothing
                }
                else if ((cmps[0] == "g" || cmps[0] == "o") && splitByMaterial == false)
                {
                    cmesh = data;
                    if (!objectNames.Contains(cmesh))
                    {
                        objectNames.Add(cmesh);
                    }
                }
                else if (cmps[0] == "usemtl")
                {
                    cmaterial = data;
                    if (!materialNames.Contains(cmaterial))
                    {
                        materialNames.Add(cmaterial);
                    }

                    if (splitByMaterial)
                    {
                        if (!objectNames.Contains(cmaterial))
                        {
                            objectNames.Add(cmaterial);
                        }
                    }
                }
                else if (cmps[0] == "v")
                {
                    //VERTEX
                    vertices.Add(ParseVectorFromCMPS(cmps));
                }
                else if (cmps[0] == "vn")
                {
                    //VERTEX NORMAL
                    normals.Add(ParseVectorFromCMPS(cmps));
                }
                else if (cmps[0] == "vt")
                {
                    //VERTEX UV
                    uvs.Add(ParseVectorFromCMPS(cmps));
                }
                else if (cmps[0] == "f")
                {
                    int[] indexes = new int[cmps.Length - 1];
                    for (int i = 1; i < cmps.Length ; i++)
                    {
                        string felement = cmps[i];
                        int vertexIndex = -1;
                        int normalIndex = -1;
                        int uvIndex = -1;
                        if (felement.Contains("//"))
                        {
                            //doubleslash, no UVS.
                            string[] elementComps = felement.Split('/');
                            vertexIndex = int.Parse(elementComps[0]) - 1;
                            normalIndex = int.Parse(elementComps[2]) - 1;
                        }
                        else if (felement.Count(x => x == '/') == 2)
                        {
                            //contains everything
                            string[] elementComps = felement.Split('/');
                            vertexIndex = int.Parse(elementComps[0]) - 1;
                            uvIndex = int.Parse(elementComps[1]) - 1;
                            normalIndex = int.Parse(elementComps[2]) - 1;
                        }
                        else if (!felement.Contains("/"))
                        {
                            //just vertex inedx
                            vertexIndex = int.Parse(felement) - 1;
                        }
                        else
                        {
                            //vertex and uv
                            string[] elementComps = felement.Split('/');
                            vertexIndex = int.Parse(elementComps[0]) - 1;
                            uvIndex = int.Parse(elementComps[1]) - 1;
                        }
                        string hashEntry = vertexIndex + "|" + normalIndex + "|" +uvIndex;
                        if (hashtable.ContainsKey(hashEntry))
                        {
                            indexes[i - 1] = hashtable[hashEntry];
                        }
                        else
                        {
                            //create a new hash entry
                            indexes[i - 1] = hashtable.Count;
                            hashtable[hashEntry] = hashtable.Count;
                            uvertices.Add(vertices[vertexIndex]);
                            if (normalIndex < 0 || (normalIndex > (normals.Count - 1)))
                            {
                                unormals.Add(Vector3.zero);
                            }
                            else
                            {
                                hasNormals = true;
                                unormals.Add(normals[normalIndex]);
                            }
                            if (uvIndex < 0 || (uvIndex > (uvs.Count - 1)))
                            {
                                uuvs.Add(Vector2.zero);
                            }
                            else
                            {
                                uuvs.Add(uvs[uvIndex]);
                            }

                        }
                    }
                    if (indexes.Length < 5 && indexes.Length >= 3)
                    {
                        OBJFace f1 = new OBJFace();
                        f1.materialName = cmaterial;
                        f1.indexes = new int[] { indexes[0], indexes[1], indexes[2] };
                        f1.meshName = (splitByMaterial) ? cmaterial : cmesh;
                        faceList.Add(f1);
                        if (indexes.Length > 3)
                        {

                            OBJFace f2 = new OBJFace();
                            f2.materialName = cmaterial;
                            f2.meshName = (splitByMaterial) ? cmaterial : cmesh;
                            f2.indexes = new int[] { indexes[2], indexes[3], indexes[0] };
                            faceList.Add(f2);
                        }
                    }
                }
            }
        }

        if (objectNames.Count == 0)
            objectNames.Add("default");

        //build objects
        parentObject = parentObject ?? new GameObject(meshName);

        foreach (string obj in objectNames)
        {
            GameObject subObject = new GameObject(obj);
            subObject.transform.parent = parentObject.transform;
            subObject.transform.localScale = new Vector3(-1, 1, 1);
            //Create mesh
            Mesh m = new Mesh();
            m.name = obj;
            //LISTS FOR REORDERING
            List<Vector3> processedVertices = new List<Vector3>();
            List<Vector3> processedNormals = new List<Vector3>();
            List<Vector2> processedUVs = new List<Vector2>();
            List<int[]> processedIndexes = new List<int[]>();
            Dictionary<int,int> remapTable = new Dictionary<int, int>();
            //POPULATE MESH
            List<string> meshMaterialNames = new List<string>();

            OBJFace[] ofaces = faceList.Where(x =>  x.meshName == obj).ToArray();
            foreach (string mn in materialNames)
            {
                OBJFace[] faces = ofaces.Where(x => x.materialName == mn).ToArray();
                if (faces.Length > 0)
                {
                    int[] indexes = new int[0];
                    foreach (OBJFace f in faces)
                    {
                        int l = indexes.Length;
                        System.Array.Resize(ref indexes, l + f.indexes.Length);
                        System.Array.Copy(f.indexes, 0, indexes, l, f.indexes.Length);
                    }
                    meshMaterialNames.Add(mn);
                    if (m.subMeshCount != meshMaterialNames.Count)
                        m.subMeshCount = meshMaterialNames.Count;

                    for (int i = 0; i < indexes.Length; i++)
                    {
                        int idx = indexes[i];
                        //build remap table
                        if (remapTable.ContainsKey(idx))
                        {
                            //ezpz
                            indexes[i] = remapTable[idx];
                        }
                        else
                        {
                            processedVertices.Add(uvertices[idx]);
                            processedNormals.Add(unormals[idx]);
                            processedUVs.Add(uuvs[idx]);
                            remapTable[idx] = processedVertices.Count - 1;
                            indexes[i] = remapTable[idx];
                        }
                    }

                    processedIndexes.Add(indexes);
                }
                else
                {

                }
            }

            //apply stuff
            m.vertices = processedVertices.ToArray();
            m.normals = processedNormals.ToArray();
            m.uv = processedUVs.ToArray();

            for (int i = 0; i < processedIndexes.Count; i++)
            {
                m.SetTriangles(processedIndexes[i],i);   
            }

            if (!hasNormals)
            {
             m.RecalculateNormals();   
            }
            m.RecalculateBounds();
            ;

            MeshFilter mf = subObject.AddComponent<MeshFilter>();
            MeshRenderer mr = subObject.AddComponent<MeshRenderer>();

            Material[] processedMaterials = new Material[meshMaterialNames.Count];
            for (int i = 0; i < meshMaterialNames.Count; i++)
            {

                if (materialCache == null)
                {
                    processedMaterials[i] = defaultMaterial; //new Material(Shader.Find("Standard (Specular setup)"));
                }
                else
                {
                    Material mfn = Array.Find(materialCache, x => x.name == meshMaterialNames[i]); ;
                    if (mfn == null)
                    {
                        processedMaterials[i] = defaultMaterial; //new Material(Shader.Find("Standard (Specular setup)"));
                    }
                    else
                    {
                        processedMaterials[i] = mfn;
                    }

                }
                processedMaterials[i].name = meshMaterialNames[i];
            }

            mr.materials = processedMaterials;
            mf.mesh = m;

        }

        return parentObject;
    }

    public static string[] ReadAllLines(Stream loadStream)
    {
        List<string> lines = new List<string>();

        using (StreamReader r = new StreamReader(loadStream))
        {
            string line;
            while ((line = r.ReadLine()) != null)
            {
                lines.Add(line);
            }
        }

        return lines.ToArray();
    }
    
}