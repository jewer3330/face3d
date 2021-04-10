#if !UNITY_EDITOR && !TEXTRACKER_ENABLED_RUNTIME
#undef TEXTRACKER_ENABLED
#endif
#if TEXTRACKER_ENABLED_RUNTIME
#define TEXTRACKER_ENABLED
#endif

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConditionIsAlwaysTrueOrFalse

[PublicAPI]
public class TexTracker : MonoBehaviour
{
    private static TexTracker instance;

    private static TexTracker Instance
    {
        get { if (!instance) instance = new GameObject("<TexTracker>").AddComponent<TexTracker>(); return instance; }
        set { instance = value; }
    }

    private List<Texture> textures = new List<Texture>();
    private static readonly Dictionary<string, Texture> texturesDict = new Dictionary<string, Texture>();

    [Conditional("TEXTRACKER_ENABLED")]
    public static void Track(Texture tex, string name = null)
    {
        if (name == null) name = tex.name;
        if (name == null) name = "Quad";
        
        if (tex)
        {
            tex = SetupTexture(tex);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(Instance.transform);
            quad.transform.localScale = new Vector3(1, tex.height / (float) tex.width, 1);
            quad.GetComponent<MeshRenderer>().material.mainTexture = tex;
            quad.GetComponent<MeshRenderer>().material.shader = Shader.Find("Debug/PreviewPlane");
            quad.SetActive(false);
            quad.name = string.Format("<{0}>{1:x6}", name, quad.GetInstanceID() & 0xffffff);
            SaveToPNG(tex, quad.name);
        }
        else
        {
            new GameObject(name);
        }

        texturesDict[name] = tex;
        Debug.Log("TexTracker: " + name);
    }
    [Conditional("TEXTRACKER_ENABLED")]
    public static void Track(Texture tex, Mesh mesh, string name = "")
    {
        tex = SetupTexture(tex);
        var go = new GameObject();
        go.transform.SetParent(Instance.transform);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().material.mainTexture = tex;
        go.GetComponent<MeshRenderer>().material.shader = Shader.Find("Unlit/Texture");
        go.SetActive(false);
        go.name = string.Format("<{0}>{1:x6}", name, go.GetInstanceID() & 0xffffff);
        //Tools.SaveModel(go);

        SaveToPNG(tex, go.name);
        SaveMesh(mesh, go.name);
        Debug.Log("TexTracker: " + name);
    }
    [Conditional("TEXTRACKER_ENABLED")]
    public static void Track(Mesh mesh, string name = "", Matrix4x4 transformMatrix = default(Matrix4x4),Texture texture = null)
    {
        if (transformMatrix != default(Matrix4x4))
        {
            var tMesh = Instantiate(mesh);
            tMesh.name = mesh.name;
            var vertices = tMesh.vertices;
            MeshUtils.ApplyTransform(vertices, transformMatrix);
            tMesh.vertices = vertices;
            mesh = tMesh;
        }

        mesh.RecalculateBounds();
        var go = new GameObject();
        go.transform.SetParent(Instance.transform);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        go.GetComponent<MeshRenderer>().material.shader = Shader.Find("Standard");
        go.GetComponent<MeshRenderer>().material.mainTexture = texture;
        go.SetActive(false);
        go.name = string.Format("<{0}>{1:x6}", name, go.GetInstanceID() & 0xffffff);
        //Tools.SaveModel(go);

        if (texture) SaveToPNG(texture, go.name);
        SaveMesh(mesh, go.name);
        Debug.Log("TexTracker: " + name);
    }

    [Conditional("TEXTRACKER_ENABLED")]
    public static void Track(Material mat, string name = null)
    {
        if (name == null) name = mat.name;
        if (name == null) name = "Quad";

        mat = new Material(mat) {name = mat.name};
        
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(Instance.transform);

        foreach (int nameID in mat.GetTexturePropertyNameIDs())
        {
            Texture tex = mat.GetTexture(nameID);
            tex = SetupTexture(tex);
            mat.SetTexture(nameID, tex);
        }
        if (mat.HasProperty("_MainTex") && mat.mainTexture)
        {
            Texture mainTex = mat.mainTexture;
            quad.transform.localScale = new Vector3(1, mainTex.height / (float) mainTex.width, 1);
        }

        quad.GetComponent<MeshRenderer>().sharedMaterial = mat;
        quad.SetActive(false);
        quad.name = string.Format("<{0}>{1:x6}", name, quad.GetInstanceID() & 0xffffff);

        if (mat.HasProperty("_MainTex") && mat.mainTexture)
        {
            SaveToPNG(mat.mainTexture, quad.name);
        }
        Debug.Log("TexTracker: " + name);
    }

    [Conditional("TEXTRACKER_ENABLED")]
    public static void Track(byte[] pngData, string name)
    {
        Texture2D t = new Texture2D(1, 1);
        t.LoadImage(pngData);
        t.Apply();
        Util.SaveFileToLocal(name, t.EncodeToPNG());
        Track(t);
        Destroy(t);
    }

    public static Texture GetTexture(string name)
    {
        Texture tex;
        texturesDict.TryGetValue(name, out tex);
        return tex;
    }

    private static Texture2D SetupTexture(Texture tex)
    {
        Texture2D tex2d;
        RenderTexture rt;
        if ((rt = tex as RenderTexture) != null)
        {
            TextureFormat format = rt.format == RenderTextureFormat.ARGBFloat
                ? TextureFormat.RGBAFloat
                : TextureFormat.RGBA32;
            tex2d = rt.ToNewTexture2D(format);
        }
        else if ((tex2d = tex as Texture2D) != null && tex2d.isReadable)
        {
            string name = tex2d.name;
            tex2d = Instantiate(tex2d);
            tex2d.name = name;
            tex2d.Apply(false, true);
        }
        
        Instance.textures.Add(tex2d);

        return tex2d;
    }

    private static void SetupModel(GameObject model)
    {
        model.transform.SetParent(Instance.transform);
    }

    [Conditional("SAVETEXTRUE_ENABLED")]
    public static void SaveTextureToPNG(Texture inputTex, string dirPath, string pngName)
    {
        if (inputTex == null)
        {
            Debug.LogError("SaveTextureToPNG: inputTex null");
            return;
        }
        RenderTexture temp = RenderTexture.GetTemporary(inputTex.width, inputTex.height, 0, RenderTextureFormat.ARGB32);
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        Graphics.Blit(inputTex, temp, mat);
        byte[] bytes = temp.TextureEncodeToPNG();
        string path = dirPath+ "/" + pngName + ".png";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
        RenderTexture.ReleaseTemporary(temp);

    }

    [Conditional("SAVETEXTRUE_ENABLED")]
    private static void SaveToPNG(Texture rt, string name)
    {
        SaveTextureToPNG(rt, Application.persistentDataPath + "/TexTracker", name);
    }

    [Conditional("SAVETEXTRUE_ENABLED")]
    private static void SaveMesh(Mesh mesh, string name)
    {
        if (mesh == null)
        {
            Debug.LogError("SaveTextureToPNG: mesh null");
            return;
        }
        string dirPath = Application.persistentDataPath + "/TexTracker";

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Util.ToObjData(mesh.vertices, mesh.uv, mesh.triangles));
        string path = dirPath+ "/" + name + ".obj";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
    }
}
