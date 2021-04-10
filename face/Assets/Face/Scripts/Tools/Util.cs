using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ARFace;
using UnityEngine;

public static class Util
{

    public static void SaveFileToLocal(string fileName, byte[] fileData)
    {
        fileName = fileName.Replace("/", "__");
        fileName = fileName.Replace("<", "__");
        fileName = fileName.Replace(">", "__");
        string path = Path.Combine(Application.persistentDataPath, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, fileData);
        Debug.Log("SaveFileToLocal: " + path);
    }

    public static string ToObjData(Vector3[] vertices, Vector2[] uv, int[] triangles)
    {
        return ToObjData(new Mesh
        {
            vertices = vertices,
            uv = uv,
            triangles = triangles,
        });
    }

    public static string ToObjData(Mesh mesh, ObjOptions options = null)
    {
        string name = mesh.name;
        if (string.IsNullOrEmpty(name))
            name = name.Replace("(Clone)", "").Trim(' ');
        if (string.IsNullOrEmpty(name))
            name = "default";
        var model = new ObjExporter.Model(mesh);
        ObjExporter.Export(name, new[] { model }, out string result, out string _, out List<string> _, options);
        return result;
    }

    public static string ToObjData(Renderer renderer, ObjOptions options = null)
    {
        Mesh mesh = renderer.GetMesh();
        string name = mesh.name.Replace("(Clone)", "").Trim(' ');
        if (string.IsNullOrEmpty(name))
            name = renderer.name.Replace("(Clone)", "").Trim(' ');
        if (string.IsNullOrEmpty(name))
            name = "default";
        var model = new ObjExporter.Model(renderer);
        ObjExporter.Export(name, new[] { model }, out string result, out string _, out List<string> _, options);
        return result;
    }


    public static Texture2D ToNewTexture2D(this RenderTexture @this, TextureFormat format = TextureFormat.RGBA32)
    {
        var savedActive = RenderTexture.active;
        Texture2D tex = null;
        try
        {
            tex = new Texture2D(@this.width, @this.height, format, false, !@this.sRGB);
            tex.name = @this.name + " (ToNewTexture2D)";
            @this.CopyTo(tex);
            return tex;
        }
        catch
        {
            DestroyRes(tex);
            throw;
        }
        finally
        {
            RenderTexture.active = savedActive;
        }
    }

    public static void CopyTo(this RenderTexture @this, Texture2D tex)
    {
        var rtSaved = RenderTexture.active;
        try
        {
            RenderTexture.active = @this;
            Rect rect = new Rect(0, 0, @this.width, @this.height);
            tex.ReadPixels(rect, 0, 0, false);
            tex.Apply();
        }
        finally
        {
            RenderTexture.active = rtSaved;
        }
    }

    public static void DestroyRes(UnityEngine.Object res)
    {
        if (res)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(res);
            else
                UnityEngine.Object.DestroyImmediate(res);
        }
    }


    public static Vector2[] FlipUVVertical(Vector2[] uv)
    {
        // flip faceMesh uv
        for (int i = 0; i < uv.Length; i++)
        {
            uv[i].y = 1 - uv[i].y;
        }
        return uv;
    }

    public static List<Vector2> RotateVector2(IList<Vector2> uv, float angle)
    {
        List<Vector2> ret = new List<Vector2>();
        foreach (var vector in uv)
        {
            ret.Add(RotateVector2(vector, angle));
        }
        return ret;
    }

    public static Vector2 RotateVector2(Vector2 vector, float angle)
    {
        vector -= new Vector2(0.5f, 0.5f);
        float a = Mathf.Cos(angle / 180f * Mathf.PI);
        float b = Mathf.Sin(angle / 180f * Mathf.PI);
        var ret = new Vector2(a * vector.x + b * vector.y, -b * vector.x + a * vector.y);
        return ret + new Vector2(0.5f, 0.5f);
    }


    public static RenderTexture RotateTexture(RenderTexture texture, float angle)
    {
        var width1 = texture.width;
        var height1 = texture.height;

        bool reverse = (int)Math.Abs(angle) / 90 % 2 == 1;
        RenderTexture dest;
        if (reverse)
            dest = new RenderTexture(height1, width1, 0, texture.format, texture.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
        else
            dest = new RenderTexture(width1, height1, 0, texture.format, texture.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);

        if (texture)
        {
            var rotateMat = new Material(Shader.Find("Unlit/UVRotateShader"));
            rotateMat.SetTexture("_MainTex", texture);
            rotateMat.SetFloat("_Rotate", angle);
            Graphics.Blit(texture, dest, rotateMat);
            UnityEngine.Object.Destroy(rotateMat);
        }

        TexTracker.Track(dest, "rotate tex");
        return dest;
    }


    /// <summary>
    /// 从src中裁出矩形
    /// </summary>
    /// <param name="src"></param>
    /// <param name="clipBox"></param>
    /// <param name="dest"></param>
    public static void Clip(RenderTexture src, Rect clipBox, RenderTexture dest)
    {
        dest.DiscardContents();
        var tiling = clipBox.max - clipBox.min;
        var offset = clipBox.min;
        var clipMat = new Material(Shader.Find("Hidden/ImageScaleShader"));
        clipMat.SetTexture("_InputTex", src);
        clipMat.SetTextureScale("_InputTex", tiling);
        clipMat.SetTextureOffset("_InputTex", offset);
        Graphics.Blit(null, dest, clipMat);
        GameObject.Destroy(clipMat);

    }

    /// <summary>中心不变，边长不超过矩形长边的前提下寻找最大方框</summary>
    public static RectInt MakeSquare(RectInt rect, RectInt bounds)
    {
        // 长边
        int a = Mathf.Max(rect.width, rect.height);
        // 上限
        Vector2 center = rect.center;
        float b = Mathf.Min(
            center.x - bounds.xMin,
            center.y - bounds.yMin,
            bounds.xMax - center.x,
            bounds.yMax - center.y);
        //
        Vector2 size = Vector2Int.one * (int)Mathf.Min(a, b * 2);
        return new RectInt(Vector2Int.RoundToInt(center - size / 2), Vector2Int.RoundToInt(size));
    }

    public static Rect ResizeRect(Rect rect, float scale)
    {
        var center = rect.center;
        var min = rect.min;
        var max = rect.max;
        min = Vector2.Max(Vector2.zero, (min - center) * scale + center);
        max = Vector2.Min(Vector2.one, (max - center) * scale + center);
        Rect ret = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return ret;
    }


    #region ARKit
    public static readonly ushort[][] DEFAULTFACEMERGEBORDER_ARKIT =
    {
        new ushort[]{1047, 1048, 1049,  975,   35 },
        new ushort[]{ 913,  994,  983,  974,  777 },
        new ushort[]{ 912,  993,  982,  973,  776 },
        new ushort[]{ 911,  944, 1050,  972,  775 },
        new ushort[]{ 910,  992, 1051,  971,  774 },
        new ushort[]{ 909,  991, 1052,  970,  563 },
        new ushort[]{ 908,  990, 1053,  969,  562 },
        new ushort[]{ 907, 1007, 1054,  509,  843 },
        new ushort[]{ 906, 1006, 1055,  892,  893 },
        new ushort[]{ 822, 1005, 1056,  510,  895 },
        new ushort[]{1216, 1004, 1057,  887,  897 },
        new ushort[]{1215, 1003, 1058, 1024,  886 },
        new ushort[]{1214, 1002, 1059, 1025, 1042 },
        new ushort[]{1213, 1001, 1060, 1026, 1043 },
        new ushort[]{ 730, 1000, 1008, 1027, 1044 },
        new ushort[]{ 807,  999, 1009, 1028, 1045 },
        new ushort[]{ 966,  965, 1010, 1029, 1046 },
        new ushort[]{ 888,  943, 1011, 1030,  890 },
        new ushort[]{ 489,  942, 1012, 1031,  889 },
        new ushort[]{ 579,  941, 1013, 1032,  662 },
        new ushort[]{ 616,  964, 1014, 1033,  663 },
        new ushort[]{ 661,  963, 1015, 1034,  664 },
        new ushort[]{ 765,  962, 1016, 1035,  766 },
        new ushort[]{ 660,  961, 1017, 1036,  665 },
        new ushort[]{ 659,  960, 1018, 1037,  666 },
        new ushort[]{ 580,  959, 1019, 1038,  667 },
        new ushort[]{ 783,  958, 1020, 1039,  668 },
        new ushort[]{ 853,  957, 1021, 1040,  852 },
        new ushort[]{  20,  956, 1022, 1041,   19 },
        new ushort[]{ 425,  955,  424,  423,  422 },
        new ushort[]{ 352,  954,  351,  350,  233 },
        new ushort[]{ 131,  953,  220,  226,  232 },
        new ushort[]{ 211,  952,  219,  225,  231 },
        new ushort[]{ 212,  951,  218,  224,  230 },
        new ushort[]{ 330,  950,  331,  332,  333 },
        new ushort[]{ 213,  949,  217,  223,  229 },
        new ushort[]{ 167,  948,  216,  222,  228 },
        new ushort[]{ 130,  947,  215,  221,  227 },
        new ushort[]{  39,  946,  214,  236,  474 },
        new ushort[]{ 467,  945,  466,  473,  475 },
        new ushort[]{  57,  968, 1023,  472,  458 },
        new ushort[]{ 376,  967,  940,  471,  459 },
        new ushort[]{ 295,  931,  939,  464,  463 },
        new ushort[]{ 208,  930,  938,  465,  469 },
        new ushort[]{ 904,  929,  937,  468,  470 },
        new ushort[]{ 905,  928,  936,  391,  460 },
        new ushort[]{ 462,  927,  935,  461,  482 },
        new ushort[]{ 392,  926,  934,   61,  480 },
        new ushort[]{ 921,  925,  933,  477,  478 },
        new ushort[]{ 920,  924,  932,   60,   59 },
        new ushort[]{ 919,  923,  989,  981,  113 },
        new ushort[]{ 918,  922,  988,  980,  114 },
        new ushort[]{ 917,  998,  987,  979,  341 },
        new ushort[]{ 916,  997,  986,  978,  342 },
        new ushort[]{ 915,  996,  985,  977,  343 },
        new ushort[]{ 914,  995,  984,  976,  344 },
    };

    #endregion

}

