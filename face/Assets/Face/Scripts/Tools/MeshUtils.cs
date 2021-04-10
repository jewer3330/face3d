using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

public static class MeshUtils 
{

    public static Mesh GetMesh(GameObject gameObject)
    {
        return GetMesh(gameObject.GetComponent<Renderer>());
    }

    public static Mesh GetMesh(this Renderer renderer)
    {
        if (renderer is MeshRenderer)
            return renderer.GetComponent<MeshFilter>().sharedMesh;
        if (renderer is SkinnedMeshRenderer)
            return ((SkinnedMeshRenderer)renderer).sharedMesh;
        throw new NotSupportedException();
    }

    public static Rect PixelRectToUvRect(RectInt pixelRect, Vector2Int pixelSize)
    {
        Vector2 pixelSizeF = pixelSize;
        return new Rect(
            pixelRect.x / pixelSizeF.x,
            pixelRect.y / pixelSizeF.y,
            pixelRect.width / pixelSizeF.x,
            pixelRect.height / pixelSizeF.y);
    }


    public static RectInt UvRectToPixelRect(Rect uvRect, Vector2Int pixelSize)
    {
        return new RectInt(
            Vector2Int.RoundToInt(Vector2.Scale(uvRect.position, pixelSize)),
            Vector2Int.RoundToInt(Vector2.Scale(uvRect.size, pixelSize)));
    }

    public static void ApplyTransform(IList<Vector3> vertices, Matrix4x4 transform)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = transform.MultiplyPoint(vertices[i]);
        }
    }

    /// <summary>
    /// 预测2个边的外边的线
    /// </summary>
    /// <returns>The normal2 d.</returns>
    /// <param name="a">The alpha component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="c">C.</param>
    public static Vector3 PredictNormal2D(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = (b - a).normalized;
        Vector3 cb = (b - c).normalized;
        Vector3 normal = Vector3.back;

        Vector3 right = Vector3.Cross(ab, normal);

        Vector3 result = ab + cb;
        if (Math.Abs(result.magnitude) < 0.01f)
        {
            result = Vector3.Cross(normal, ab);
        }
        float angle = Vector3.Dot(result, right);
        if (angle < 0)
        {
            result = -result;
        }
        Vector3 predict = result.normalized;

        return predict;
    }

    public static void AddQuad(IList<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
        triangles.Add(a); triangles.Add(c); triangles.Add(d);
    }



    public static RenderTexture BuildUvRemapLutWithMask(IList<Vector2> uvFrom, IList<Vector2> uvTo, List<int> triangles, int size, Texture2D mask)
    {
        var savedActive = RenderTexture.active;
        Assert.AreEqual(uvFrom.Count, uvTo.Count);
        if ((size & 1) != 0)
            throw new ArgumentException();
        Mesh uvMesh = new Mesh
        {
            vertices = uvFrom.Select(uv => (Vector3)uv).ToArray(),
            colors = uvTo.Select(uv =>
            {
                //Color color = mask.GetPixelPoint(uv.x, uv.y);
                //if (color.a > 0)
                //{
                return new Color(uv.x, uv.y, 0, 1);
                //}

                //int index = uvTo.IndexOf(uv);
                //Debug.Log(index);
                //return new Color(uvFrom[index].x, uvFrom[index].y, 0, 1);
            })
            .ToArray(),
        };
        uvMesh.SetTriangles(triangles, 0);

        Material mat = new Material(Shader.Find("Debug/VertexColor"));

        RenderTextureFormat renderTextureFormat;
        {
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32))
                renderTextureFormat = RenderTextureFormat.ARGB32;
            else
                renderTextureFormat = RenderTextureFormat.Default;
        }
        RenderTexture target =
            new RenderTexture(size, size, 0, renderTextureFormat, RenderTextureReadWrite.Linear);
        GraphicsUtils.DrawUvMesh(uvMesh, mat, target);


        return target;
    }

    /// <summary>
    /// 绘制UV拓扑图
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="mat"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    public static GameObject Draw(Mesh mesh, Material mat, int idx)
    {
        if (!mesh)
            return null;
        Mesh ret = new Mesh();
        Vector2[] uvs = null;
        switch (idx)
        {
            case 0:
                uvs = mesh.uv;
                break;
            case 1:
                uvs = mesh.uv2;
                break;
            case 2:
                uvs = mesh.uv3;
                break;
            case 3:
                uvs = mesh.uv4;
                break;
            case 4:
                break;
        }

        ret.vertices = uvs.Select((uv) => new Vector3(uv.x, uv.y, 0)).ToArray();
        ret.triangles = mesh.triangles;
        ret.uv = mesh.uv;


        var go = new GameObject($"UV{idx}");
        var render = go.AddComponent<MeshRenderer>();
        if (mat)
        {
            render.sharedMaterial = new Material(Shader.Find("Unlit/Texture"));
            render.sharedMaterial.mainTexture = mat.mainTexture;
        }
        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = ret;

        return go;

    }

    /// <summary>
    /// 将人脸图片的影色为另外一种拓扑
    /// </summary>
    /// <param name="dest">绘制目标</param>
    /// <param name="src">人脸mesh</param>
    /// <param name="toUVs">uvremap</param>
    /// <param name="srcMain">大饼脸拓扑</param>
    public static void GenRemapTexture(RenderTexture dest, Mesh src, Vector2[] toUVs, Texture srcMain)
    {

        if (dest)
            dest.DiscardContents();
        var mesh = new Mesh();
        mesh.vertices = toUVs.Select(uv =>
        {
            return new Vector3(uv.x, uv.y);
        }).ToArray();
        mesh.triangles = src.triangles;
        mesh.uv = src.uv;


        var tempMat = new Material(Shader.Find("Unlit/Texture"));
        tempMat.mainTexture = srcMain;

        GraphicsUtils.DrawUvMesh(mesh, tempMat, dest, Color.clear);

        //TexTracker.Track(dest, "RemapTexture");

        Util.DestroyRes(tempMat);
        Util.DestroyRes(mesh);

    }

    /// <summary>
    /// 把人脸的顶点信息映射到另外一种拓扑
    /// </summary>
    /// <param name="dest"></param>
    /// <param name="src"></param>
    /// <param name="toUVs"></param>
    /// <param name="renlian2art">人脸模型空间到美术头空间的举证转换</param>
    public static void GenVertexLut(RenderTexture dest, Mesh src, Vector2[] toUVs, Matrix4x4 renlian2art)
    {
        var mesh = new Mesh();
        mesh.vertices = toUVs.Select(uv =>
        {
            return new Vector3(uv.x, uv.y);
        }).ToArray();
        mesh.triangles = src.triangles;

        mesh.colors = src.vertices.Select(v => {
            v = renlian2art.MultiplyPoint3x4(v);
            return new Color(v.x, v.y, v.z, 1);
        }).ToArray();

        if (dest)
            dest.DiscardContents();


        var tempMat = new Material(Shader.Find("Unlit/VertexColorUnlitShader"));
        GraphicsUtils.DrawUvMesh(mesh, tempMat, dest, Color.clear);

        //TexTracker.Track(dest, "VertexLut");

        Object.Destroy(tempMat);
    }


    /// <summary>
    /// 重新映射头
    /// </summary>
    /// <param name="head"></param>
    /// <param name="vertexLut"></param>
    /// <param name="mask"></param>
    /// <param name="main"></param>
    public static GameObject RemapHead(GameObject head, Texture2D vertexLut, Texture2D mask, RenderTexture main)
    {
        GameObject newhead = head;
        newhead.name = "head";
        newhead.gameObject.SetActive(true);
        Transform headTrans = newhead.transform.Find("head");
        var render = headTrans.GetComponent<SkinnedMeshRenderer>();
        var newface = Object.Instantiate(render.sharedMesh);
        List<Vector3> vts = new List<Vector3>(newface.vertexCount);
        var uvs = newface.uv;
        var vertices = newface.vertices;
        for (int i = 0; i < newface.vertexCount; i++)
        {
            Vector3 ret = Vector3.one;
            Color color = vertexLut.GetPixelPoint(uvs[i].x, uvs[i].y);
            float alpha = 1;
            if (mask)
            {
                Color color_mask = mask.GetPixelBilinear(uvs[i].x, uvs[i].y);
                alpha = color_mask.a;
            }
            if (System.Math.Abs(alpha) <= float.Epsilon || (System.Math.Abs(color.r) <= float.Epsilon && System.Math.Abs(color.g) <= float.Epsilon && System.Math.Abs(color.b) <= float.Epsilon))
            {

                var v = (vertices[i]);
                //v = Lerp(min, max, v);
                ret = v;
                alpha = 0;
            }

            ret = new Vector3(color.r, color.g, color.b) * alpha + (vertices[i]) * (1 - alpha);
            vts.Add(ret);
        }

        newface.vertices = vts.ToArray();
        newface.UploadMeshData(false);


        render.sharedMesh = newface;
        render.material.mainTexture = main;

        return newhead;
    }

    /// <summary>
    /// 工具方法，返回一堆 uv1-->uv2 映射的GameObjects
    /// </summary>
    /// <param name="uv">这个meshUV</param>
    /// <param name="parent">生成挂在位置</param>
    /// <param name="uvRemap">uv映射图</param>
    /// <param name="mask">uv映射无效图</param>
    /// <param name="goLines">返回的一堆物件</param>
    public static void DrawUVGravitationalField(IList<Vector2> uv, GameObject parent, Texture2D uvRemap, Texture2D mask, IList<GoLine> goLines)
    {
        if (goLines == null)
        {
            goLines = new List<GoLine>(uv.Count);
        }
        var uvs = uv;
        Material matRed = new Material(Shader.Find("Unlit/Color"));
        Material matGreen = new Material(Shader.Find("Unlit/Color"));
        GameObject orign = new GameObject("orgin");
        GameObject to = new GameObject("to");
        orign.transform.SetParent(parent.transform, false);
        to.transform.SetParent(parent.transform, false);
        matRed.color = Color.red;
        matGreen.color = Color.green;
        for (int i = 0; i < uvs.Count; i++)
        {

            //origin postion
            GameObject go1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go1.transform.SetParent(orign.transform, false);
            go1.transform.localScale = Vector3.one * 0.001f;
            go1.transform.localPosition = uvs[i];
            go1.transform.localRotation = Quaternion.identity;
            go1.name = i.ToString();
            go1.GetComponent<MeshRenderer>().sharedMaterial = matGreen;
            Util.DestroyRes(go1.GetComponent<BoxCollider>());



            //to position
            GameObject go2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go2.transform.SetParent(to.transform, false);
            go2.transform.localScale = Vector3.one * 0.001f;
            go2.transform.localPosition = ValidUV(uvRemap, uvs[i], mask);
            go2.transform.localRotation = Quaternion.identity;
            go2.name = i.ToString();
            Util.DestroyRes(go2.GetComponent<BoxCollider>());
            go2.GetComponent<MeshRenderer>().sharedMaterial = matRed;

            goLines.Add(new GoLine() { go1 = go1, go2 = go2 });
        }
    }

    public static Vector3 InversLerp(Vector3 a, Vector3 b, Vector3 v)
    {
        Vector3 ret = Vector3.zero;
        ret.x = Mathf.InverseLerp(a.x, b.x, v.x);
        ret.y = Mathf.InverseLerp(a.y, b.y, v.y);
        ret.z = Mathf.InverseLerp(a.z, b.z, v.z);
        return ret;
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, Vector3 v)
    {
        Vector3 ret = Vector3.zero;
        ret.x = Mathf.Lerp(a.x, b.x, v.x);
        ret.y = Mathf.Lerp(a.y, b.y, v.y);
        ret.z = Mathf.Lerp(a.z, b.z, v.z);
        return ret;
    }

    public static Vector3 MinVerctor3(IList<Vector3> list)
    {
        Vector3 ret = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        foreach (var item in list)
        {
            ret = Vector3.Min(item, ret);
        }
        return ret;
    }

    public static Vector3 MaxVerctor3(IList<Vector3> list)
    {
        Vector3 ret = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var item in list)
        {
            ret = Vector3.Max(item, ret);
        }
        return ret;
    }

    public static Vector2 ValidUV(Texture2D uvremap, Vector2 uv, Texture2D mask)
    {
        float alpha = 1;
        if (mask)
        {
            Color mk = mask.GetPixelBilinear(uv.x, uv.y);
            alpha = mk.a;
        }

        Color color = uvremap.GetPixelBilinear(uv.x, uv.y);

        if (System.Math.Abs(alpha) < float.Epsilon)
        {
            return uv;
        }
        else if (color.r < float.Epsilon && color.g < float.Epsilon)
        {
            return uv;
        }
        return new Vector2(color.r, color.g);
    }

    public struct GoLine
    {
        public GameObject go1;
        public GameObject go2;
    }
}
