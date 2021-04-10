using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using ARFace.Landmarks;
public class WhateverFaceMesh : MonoBehaviour
{
    private Mesh mesh;


    public System.Action<RenderTexture, Dictionary<string, Vector2>> callbackEyeAndMouth;
    public System.Action<RenderTexture> callbackNoseShadow;
    public System.Action<bool, Dictionary<string, Vector2>> callbackFinal;
    public AnimationCurve curve;
    public System.Action<RenderTexture> callbackEraseFeature;

    public static float offsetRight;
    public static float offsetDown;

    public static List<Vector3> leftEyeUV = new List<Vector3>();
    public static List<Vector3> rightEyeUV = new List<Vector3>();
    public static List<Vector2> sdkKeyPointUV = new List<Vector2>();

  

    /// <summary>
    /// 调用胡的接口的回调
    /// </summary>
    /// <param name="dic">Dic.</param>
    public void OnRecvFaceData(Dictionary<string, Vector2> dic)
    {
        StartCoroutine(_OnRecvFaceData(dic));
    }

    public IEnumerator _OnRecvFaceData(Dictionary<string, Vector2> dic)
    {
        if (dic != null && dic.Count != 0)
        {
            if (LandmarkManager.ActiveLandmarkType == LandmarkType.FacePPOnline)
            {
                sdkKeyPointUV.Clear();
                foreach (var k in sdkRetFacePPIndex)
                {
                    sdkKeyPointUV.Add(dic[k]);
                }
            }
            else if (LandmarkManager.ActiveLandmarkType == LandmarkType.Tencent)
            {
                sdkKeyPointUV.Clear();
                foreach (var k in sdkRetTencentIndex)
                {
                    sdkKeyPointUV.Add(dic[k]);
                }
            }
            else if (LandmarkManager.ActiveLandmarkType == LandmarkType.MTShowShow)
            {
                sdkKeyPointUV.Clear();
                foreach (var k in sdkRetMTIndex)
                {
                    sdkKeyPointUV.Add(dic[k]);
                }
            }
            var renderTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "arface WhateverFaceMesh.renderTexture",
            };
            //whateverCamera.backgroundColor = Color.clear;
            mesh = CreateNasalShadowMesh(dic, LandmarkManager.ActiveLandmarkType);
            Mesh2RT(mesh, renderTexture, Color.clear);
            callbackNoseShadow?.Invoke(renderTexture);
            Destroy(mesh);

            yield return 0;
            //whateverCamera.backgroundColor = Color.white;
            mesh = CreateMaskMesh1(dic, 0.05f, LandmarkManager.ActiveLandmarkType, curve);
            Mesh2RT(mesh, renderTexture, Color.white);
            callbackEyeAndMouth?.Invoke(renderTexture, dic);
            Destroy(mesh);
          
            yield return 0;
            mesh = CreateEraseFeatureMaskMesh(dic, LandmarkManager.ActiveLandmarkType);
            Mesh2RT(mesh, renderTexture, Color.clear);
            callbackEraseFeature?.Invoke(renderTexture);
            Destroy(mesh);
            
            if (renderTexture)
                Destroy(renderTexture);

            callbackFinal?.Invoke(true, dic);
        }
        else
        {
            Debug.LogError("get dic error");
            if (callbackEyeAndMouth != null)
                callbackEyeAndMouth(null, null);
            if (callbackNoseShadow != null)
                callbackNoseShadow(null);
            callbackEraseFeature?.Invoke(null);
            callbackFinal?.Invoke(false, null);
        }
        
    }


    public void Mesh2RT(Mesh mesh, RenderTexture dest,Color clear)
    {
        //GetComponent<MeshFilter>().mesh = mesh;
        //dest.DiscardContents();
        //whateverCamera.targetTexture = dest;
        //whateverCamera.Render();

        Material tempMat = null;
        try
        {
            tempMat = new Material(Shader.Find("Unlit/VertexColorUnlitShader"))
            {
                mainTexture = dest
            };
            GraphicsUtils.DrawUvMesh(mesh, tempMat, dest, clear);
        }
        finally
        {
            if (tempMat)
                Destroy(tempMat);
        }
    }

    public static Mesh CreateMaskMesh(Dictionary<string, Vector2> dic, float width, float height)
    {
        List<Vector3> vectors = new List<Vector3>();


        var left_eyebrow_v = GetVectors(left_eyebrow, dic);
        var right_eyebrow_v = GetVectors(right_eyebrow, dic);
        var left_eye_v = GetVectors(left_eye, dic);
        var right_eye_v = GetVectors(right_eye, dic);
        var mouth_v = GetVectors(mouth, dic);

        vectors.AddRange(left_eyebrow_v);
        vectors.AddRange(right_eyebrow_v);
        vectors.AddRange(left_eye_v);
        vectors.AddRange(right_eye_v);
        vectors.AddRange(mouth_v);

        List<int> indexes = new List<int>();

        int offset = 0;
        indexes.AddRange(CreateTriangleVector(left_eyebrow_v.Count, offset));
        offset += left_eyebrow_v.Count;
        indexes.AddRange(CreateTriangleVector(right_eyebrow_v.Count, offset, true));
        offset += right_eyebrow_v.Count;
        indexes.AddRange(CreateTriangleVector(left_eye_v.Count, offset));
        offset += left_eye_v.Count;
        indexes.AddRange(CreateTriangleVector(right_eye_v.Count, offset));
        offset += right_eye_v.Count;
        indexes.AddRange(CreateTriangleVector(mouth_v.Count, offset));

        Mesh ret = new Mesh();
        ret.SetVertices(vectors);
        ret.SetTriangles(indexes, 0);


        ret.UploadMeshData(false);

        return ret;
    }
    /// <summary>
    /// 保留最后一点的位置不要动
    /// </summary>
    /// <param name="vectors">Vectors.</param>
    /// <param name="offsetRight">Offset.</param>
    public static void Modify(IList<Vector3> vectors, float offsetRight, float offsetDown)
    {
        for (int i = 0; i < vectors.Count - 1; i++)
        {
            vectors[i] += new Vector3(offsetRight, offsetDown);
        }
    }

    private static void CreatePredictData(List<Vector3> vertices, int offset, Color inner, Color center, Color outer, float predictLength, List<Vector3> vectors, List<int> triangles, List<Color> colors)
    {
        if (vectors == null || triangles == null || colors == null)
        {
            throw new System.ArgumentNullException();
        }


        var center_point = GetCenter(vertices);
        var predict_points = PredictOutCirClePoints(vertices, predictLength);



        var colors_first_circle = CreateVertexColor(vertices.Count, inner);
        var colors_second_circle = CreateVertexColor(vertices.Count, outer);
        var color_center = center;

        var ts = CreateSpiderWebTriangles(vertices, center_point, predict_points, offset);

        vectors.AddRange(vertices);
        vectors.AddRange(predict_points);
        vectors.Add(center_point);

        triangles.AddRange(ts);



        List<Color> cs = new List<Color>();
        cs.AddRange(colors_first_circle);
        cs.AddRange(colors_second_circle);
        cs.Add(color_center);

        colors.AddRange(cs);
    }

    public static Mesh CreateMaskMesh1(Dictionary<string, Vector2> dic, float predictHeight,
        LandmarkType type = LandmarkType.FacePP, AnimationCurve curve = null)
    {
        List<Vector3> vectors = new List<Vector3>();

        List<int> triangles = new List<int>();

        List<Color> colors = new List<Color>();

        if (type == LandmarkType.FacePP || type == LandmarkType.FacePPOnline)
        {

            var vertices1 = GetVectors(left_eyebrow1, dic);
            var vertices2 = GetVectors(right_eyebrow1, dic);
            var vertices3 = GetVectors(left_eye1, dic);
            var vertices4 = GetVectors(right_eye1, dic);
            var vertices5 = GetVectors(mouth1, dic);

            CreatePredictData(vertices1, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices3, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices4, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices5, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);

            leftEyeUV = vertices3;
            rightEyeUV = vertices4;
        }
        else if (type == LandmarkType.Apple)
        {
            var vertices1 = GetVectors(apple_eyebrow_left, dic);
            var vertices2 = GetVectors(apple_eyebrow_right, dic);
            var vertices3 = GetVectors(apple_eye_left, dic);
            var vertices4 = GetVectors(apple_eye_right, dic);
            var vertices5 = GetVectors(apple_mouth, dic);
            var vertices1_predict = PredictEyebrow(vertices1, predictHeight, curve, false);
            var vertices2_predict = PredictEyebrow(vertices2, predictHeight, curve, true);

            CreatePredictData(vertices1_predict, vectors.Count, Color.white, Color.clear, Color.white, 0.04f, vectors, triangles, colors);
            CreatePredictData(vertices2_predict, vectors.Count, Color.white, Color.clear, Color.white, 0.04f, vectors, triangles, colors);
            CreatePredictData(vertices3, vectors.Count, Color.white, Color.clear, Color.white, 0.08f, vectors, triangles, colors);
            CreatePredictData(vertices4, vectors.Count, Color.white, Color.clear, Color.white, 0.08f, vectors, triangles, colors);
            CreatePredictData(vertices5, vectors.Count, Color.white, Color.clear, Color.white, 0.08f, vectors, triangles, colors);

            leftEyeUV = vertices3;
            rightEyeUV = vertices4;
        }
        else if (type == LandmarkType.Tencent)
        {
            var vertices1 = GetVectors(tencent_eyebrow_left, dic);
            var vertices2 = GetVectors(tencent_eyebrow_right, dic);
            var vertices3 = GetVectors(tencent_eye_left, dic);
            var vertices4 = GetVectors(tencent_eye_right, dic);
            var vertices5 = GetVectors(tencent_mouth, dic);

            CreatePredictData(vertices1, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices3, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices4, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices5, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);

            leftEyeUV = vertices3;
            rightEyeUV = vertices4;
        }
        else if (type == LandmarkType.MTShowShow || type == LandmarkType.MeituOffline || type == LandmarkType.ARKitRemote)
        {
            var vertices1 = GetVectors(mt_eyebrow_left, dic);
            var vertices2 = GetVectors(mt_eyebrow_right, dic);
            var vertices3 = GetVectors(mt_eye_left, dic);
            var vertices4 = GetVectors(mt_eye_right, dic);
            var vertices5 = GetVectors(mt_mouth, dic);

            CreatePredictData(vertices1, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices3, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices4, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);
            CreatePredictData(vertices5, vectors.Count, Color.white, Color.clear, Color.white, 0.02f, vectors, triangles, colors);

            leftEyeUV = vertices3;
            rightEyeUV = vertices4;
        }
        else
        {
            throw new NotSupportedException();
        }

        Mesh ret = new Mesh();
        ret.SetVertices(vectors);
        ret.SetTriangles(triangles, 0);
        ret.SetColors(colors);

        ret.UploadMeshData(false);

        return ret;
    }

    public static Mesh CreateEraseFeatureMaskMesh(Dictionary<string, Vector2> dic, LandmarkType type = LandmarkType.MTShowShow)
    {
        List<Vector3> vectors = new List<Vector3>();

        List<int> triangles = new List<int>();

        List<Color> colors = new List<Color>();

        if (type == LandmarkType.MTShowShow || type == LandmarkType.MeituOffline || type == LandmarkType.ARKitRemote)
        {
            var vertices1 = GetVectors(mt_eyebrow_left, dic);
            var vertices2 = GetVectors(mt_eyebrow_right, dic);
            var vertices3 = GetVectors(mt_mouth, dic);

            CreatePredictData(vertices1, vectors.Count, Color.red, Color.red, Color.clear, 0.03f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.red, Color.red, Color.clear, 0.03f, vectors, triangles, colors);
            CreatePredictData(vertices3, vectors.Count, Color.green, Color.green, Color.clear, 0.03f, vectors, triangles, colors);
        }
        else
        {
            throw new NotSupportedException();
        }

        Mesh ret = new Mesh();
        ret.SetVertices(vectors);
        ret.SetTriangles(triangles, 0);
        ret.SetColors(colors);

        return ret;
    }


    public static List<Color> CreateVertexColor(int length, Color color)
    {
        List<Color> ret = new List<Color>();
        int count = length;
        for (int i = 0; i < count; i++)
        {
            ret.Add(color);
        }

        return ret;
    }


    public static List<Vector3> GetVectors(string[] keys, Dictionary<string, Vector2> dic)
    {
        List<Vector3> rets = new List<Vector3>();
        foreach (var k in keys)
        {
            Vector2 vector;
            if (dic.TryGetValue(k, out vector))
            {
                rets.Add(new Vector3(vector.x, vector.y, 0));
            }
            else
            {
                Debug.LogErrorFormat("get key error {0}", k);

            }
        }

        if (rets.Count != keys.Length)
        {
            Debug.LogError("check the keys");
        }

        return rets;
    }


    public static Vector3 GetCenter(IList<Vector3> vectors)
    {
        Vector3 ret = Vector3.zero;
        float count = vectors.Count;
        foreach (var k in vectors)
        {
            ret += k;
        }
        return ret / count;
        //return (vectors[0] + vectors[vectors.Count / 2]) * 0.5f;
    }

    public static Vector2 GetCenter2D(IList<Vector2> vectors)
    {
        Vector2 ret = Vector2.zero;
        float count = vectors.Count;
        foreach (var k in vectors)
        {
            ret += k;
        }
        return ret / count;
    }

    public static List<Vector3> PredictOutCirClePoints(IList<Vector3> vectors, float length)
    {
        List<Vector3> ret = new List<Vector3>();
        for (int i = 0; i < vectors.Count; i++)
        {
            Vector3 a = vectors[(i - 1 + vectors.Count) % vectors.Count];
            Vector3 b = vectors[i];
            Vector3 c = vectors[(i + 1) % vectors.Count];

            Vector3 predict = MeshUtils.PredictNormal2D(a, b, c) * length + vectors[i];

            ret.Add(predict);
        }
        return ret;
    }

    public static List<Vector2> PredictOutCirCleUVs(IList<Vector2> vectors, float length)
    {
        List<Vector2> ret = new List<Vector2>();
        for (int i = 0; i < vectors.Count; i++)
        {
            Vector3 a = vectors[(i - 1 + vectors.Count) % vectors.Count];
            Vector3 b = vectors[i];
            Vector3 c = vectors[(i + 1) % vectors.Count];

            Vector3 predict = MeshUtils.PredictNormal2D(a, b, c) * length + new Vector3(vectors[i].x, vectors[i].y, 0);

            ret.Add(predict);
        }
        return ret;
    }


    public static List<int> CreateSpiderWebTriangles(List<Vector3> vectors, Vector3 center, List<Vector3> predicts, int offset)
    {
        List<int> ret = new List<int>();

        if (predicts.Count != vectors.Count)
        {
            Debug.LogError("predict is not equal vectors");
            return null;
        }
        int count = vectors.Count;
        int tailID = count * 2;
        for (int i = 0; i < count; i++)
        {
            //内圈的点，蜘蛛网缝
            int idx1 = i;
            int idx2 = (i + 1) % count;
            int idx3 = tailID;
            ret.Add(offset + idx1);
            ret.Add(offset + idx2);
            ret.Add(offset + idx3);
        }

        //外圈的点
        for (int i = 0; i < count; i++)
        {
            int idx1 = i + count;
            int idx2 = (i + count + 1) % (count * 2) == 0 ? count : (i + count + 1);
            int idx3 = i % count;
            int idx4 = (i + 1) % count;

            MeshUtils.AddQuad(ret, offset + idx1, offset + idx2, offset + idx4, offset + idx3);
        }

        return ret;
    }


    public static List<int> CreateTriangleVector(int length, int offset = 0, bool reverse = false)
    {
        List<int> rets = new List<int>();
        for (int i = 0; i < length; i++)
        {

            var id2 = i + 1;
            var id3 = i + 2;
            if (id2 < length && id3 < length)
            {
                rets.Add(i + offset);
                if (reverse)
                {
                    rets.Add(i % 2 == 0 ? id3 + offset : id2 + offset);
                    rets.Add(i % 2 == 0 ? id2 + offset : id3 + offset);
                }
                else
                {
                    rets.Add(i % 2 == 0 ? id2 + offset : id3 + offset);
                    rets.Add(i % 2 == 0 ? id3 + offset : id2 + offset);
                }
            }
        }
        return rets;
    }

    public static List<int> CreateTrianglesClock(IList<int> indices)
    {
        Debug.Assert(indices.Count % 2 == 0);
        List<int> rets = new List<int>();
        var n = indices.Count;
        for (var i = 0; i < n / 2; i++)
        {
            var id1 = indices[i + 0];
            var id2 = indices[i + 1];
            var id3 = indices[ n - 1 - (i + 1)];
            var id4 = indices[ n - 1 - (i + 0)];
            MeshUtils.AddQuad(rets, id1, id2, id3, id4);
        }
        return rets;
    }

    public static List<int> CreateTrianglesCross(IList<int> indices)
    {
        Debug.Assert(indices.Count == 4);
        List<int> rets = new List<int>();
        var n = indices.Count;
        for (var i = 0; i < n / 2; i++)
        {
            var id1 = indices[i + 0];
            var id2 = indices[i + 1];
            var id3 = indices[n - 1 - (i + 1)];
            var id4 = indices[n - 1 - (i + 0)];

            rets.Add(id1);
            rets.Add(id2);
            rets.Add(id4);

            rets.Add(id2);
            rets.Add(id3);
            rets.Add(id4);

        }
        return rets;
    }

    public static Mesh CreateNasalShadowMesh(Dictionary<string, Vector2> dic, LandmarkType type = LandmarkType.FacePP)
    {
        List<Vector3> vectors = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        if (type == LandmarkType.FacePP || type == LandmarkType.FacePPOnline)
        {
            var vertices1 = GetVectors(leftNasalShadow, dic);
            var vertices2 = GetVectors(rightNasalShadow, dic);
            CreatePredictData(vertices1, vectors.Count, Color.green * 0.5f, Color.green, Color.clear, 0.1f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.red * 0.5f, Color.red, Color.clear, 0.1f, vectors, triangles, colors);
        }
        else if (type == LandmarkType.Apple)
        {
            var vertices1 = GetVectors(apple_nose_left, dic);
            var vertices2 = GetVectors(apple_nose_right, dic);

            Modify(vertices1, -offsetRight, -offsetDown);
            Modify(vertices2, offsetRight, -offsetDown);
            CreatePredictData(vertices1, vectors.Count, Color.green * 0.5f, Color.green, Color.clear, 0.1f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.red * 0.5f, Color.red, Color.clear, 0.1f, vectors, triangles, colors);
        }
        else if (type == LandmarkType.Tencent)
        {
            var vertices1 = GetVectors(tencent_nose_left, dic);
            var vertices2 = GetVectors(tencent_nose_right, dic);
            CreatePredictData(vertices1, vectors.Count, Color.green * 0.5f, Color.green, Color.clear, 0.1f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.red * 0.5f, Color.red, Color.clear, 0.1f, vectors, triangles, colors);
        }
        else if (type == LandmarkType.MTShowShow)
        {
            var vertices1 = GetVectors(mt_nose_left, dic);
            var vertices2 = GetVectors(mt_nose_right, dic);
            CreatePredictData(vertices1, vectors.Count, Color.green * 0.5f, Color.green, Color.clear, 0.1f, vectors, triangles, colors);
            CreatePredictData(vertices2, vectors.Count, Color.red * 0.5f, Color.red, Color.clear, 0.1f, vectors, triangles, colors);
        }


        Mesh ret = new Mesh();
        ret.SetVertices(vectors);
        ret.SetTriangles(triangles, 0);
        ret.SetColors(colors);

        ret.UploadMeshData(false);

        return ret;
    }


    public static Mesh CreateNasalShadowMesh1(Dictionary<string, Vector2> dic, float width, float height)
    {
        List<Vector3> vectors = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        var left = GetVectors(leftNasalShadow, dic);
        var right = GetVectors(rightNasalShadow, dic);
        var left_center = left[2];
        var right_center = right[1];

        var left_nostril = GetVectors(leftNostril, dic);
        var right_nostril = GetVectors(rightNostril, dic);

        var left_nostril_center = GetCenter(left_nostril);
        var right_nostril_center = GetCenter(right_nostril);

        CreateCircleData(left_center, 0.08f, vectors.Count, Color.green, Color.clear, vectors, triangles, colors);
        CreateCircleData(right_center, 0.08f, vectors.Count, Color.red, Color.clear, vectors, triangles, colors);
        CreateCircleData(left_nostril_center, 0.03f, vectors.Count, Color.clear, Color.green * 0.5f, vectors, triangles, colors);
        CreateCircleData(right_nostril_center, 0.03f, vectors.Count, Color.clear, Color.red * 0.5f, vectors, triangles, colors);

        Mesh ret = new Mesh();
        ret.SetVertices(vectors);
        ret.SetTriangles(triangles, 0);
        ret.SetColors(colors);

        ret.UploadMeshData(false);

        return ret;
    }


    public static List<Vector3> CreateCircleVertices(Vector3 center, float radius, int count = 100)
    {
        List<Vector3> ret = new List<Vector3>();
        float angle = 2 * Mathf.PI / count;
        for (int i = 0; i < count; i++)
        {
            Vector3 temp = center + radius * new Vector3(Mathf.Cos(i * angle), Mathf.Sin(i * angle), 0);
            ret.Add(temp);
        }
        return ret;
    }

    public static List<int> CreateCircleTriangle(int length, Vector3 center, int offset = 0)
    {
        List<int> ret = new List<int>();
        for (int i = 0; i < length; i++)
        {
            int id1 = i;
            int id3 = (i + 1) % length;
            int id2 = length;

            ret.Add(id1 + offset);
            ret.Add(id2 + offset);
            ret.Add(id3 + offset);
        }
        return ret;
    }


    public static void CreateCircleData(Vector3 center, float radius, int offset, Color inner, Color outer, List<Vector3> vectors, List<int> triangles, List<Color> colors)
    {

        if (vectors == null || triangles == null || colors == null)
        {
            throw new System.ArgumentNullException();
        }
        var _circles = CreateCircleVertices(center, radius);
        var _triangles = CreateCircleTriangle(_circles.Count, center, offset);
        var _colors = CreateVertexColor(_circles.Count, outer);
        var _leftCenterColor = CreateVertexColor(1, inner);


        vectors.AddRange(_circles);
        vectors.Add(center);
        triangles.AddRange(_triangles);
        colors.AddRange(_colors);
        colors.AddRange(_leftCenterColor);
    }

    //左眼
    public static string[] left_eye =
    {
        "left_eye_left_corner",
        "left_eye_upper_left_quarter",
        "left_eye_lower_left_quarter",
        "left_eye_top",
        "left_eye_bottom",
        "left_eye_upper_right_quarter",
        "left_eye_lower_right_quarter",
        "left_eye_right_corner",
    };
    //右眼
    public static string[] right_eye =
    {
        "right_eye_left_corner",
        "right_eye_upper_left_quarter",
        "right_eye_lower_left_quarter",
        "right_eye_top",
        "right_eye_bottom",
        "right_eye_upper_right_quarter",
        "right_eye_lower_right_quarter",
        "right_eye_right_corner",
    };
    //左眉毛
    public static string[] left_eyebrow =
    {
        "left_eyebrow_left_corner",
        "left_eyebrow_upper_left_quarter",
        "left_eyebrow_lower_left_quarter",
        "left_eyebrow_upper_middle",
        "left_eyebrow_lower_middle",
        "left_eyebrow_upper_right_quarter",
        "left_eyebrow_lower_right_quarter",
        "left_eyebrow_upper_right_corner",
        "left_eyebrow_lower_right_corner",
    };
    //右眉毛
    public static string[] right_eyebrow =
    {
        "right_eyebrow_upper_left_corner",
        "right_eyebrow_lower_left_corner",
        "right_eyebrow_upper_left_quarter",
        "right_eyebrow_lower_left_quarter",
        "right_eyebrow_upper_middle",
        "right_eyebrow_lower_middle",
        "right_eyebrow_upper_right_quarter",
        "right_eyebrow_lower_right_quarter",
        "right_eyebrow_right_corner",
    };
    //嘴巴
    public static string[] mouth =
    {
       // "mouth_left_corner",
        "mouth_upper_lip_left_contour3",
        "mouth_upper_lip_left_contour2",
        "mouth_lower_lip_left_contour2",
        "mouth_upper_lip_left_contour1",
        "mouth_lower_lip_left_contour3",
        "mouth_upper_lip_top",
        "mouth_lower_lip_bottom",
        "mouth_upper_lip_right_contour1",
        "mouth_lower_lip_right_contour3",
        "mouth_upper_lip_right_contour2",
        "mouth_lower_lip_right_contour2",
        "mouth_upper_lip_right_contour3",
        //"mouth_right_corner",
    };


    public static string[] left_eyebrow1 =
    {
        "left_eyebrow_left_corner",
        "left_eyebrow_upper_left_quarter",
        "left_eyebrow_upper_middle",
        "left_eyebrow_upper_right_quarter",
        "left_eyebrow_upper_right_corner",
        "left_eyebrow_lower_right_corner",
        "left_eyebrow_lower_right_quarter",
        "left_eyebrow_lower_middle",
        "left_eyebrow_lower_left_quarter",
    };

    public static string[] right_eyebrow1 =
    {
        "right_eyebrow_lower_left_corner",
        "right_eyebrow_upper_left_corner",
        "right_eyebrow_upper_left_quarter",
        "right_eyebrow_upper_middle",
        "right_eyebrow_upper_right_quarter",
        "right_eyebrow_right_corner",
        "right_eyebrow_lower_right_quarter",
        "right_eyebrow_lower_middle",
        "right_eyebrow_lower_left_quarter",
    };

    public static string[] left_eye1 =
    {
        "left_eye_left_corner",
        "left_eye_upper_left_quarter",
        "left_eye_top",
        "left_eye_upper_right_quarter",
        "left_eye_right_corner",
        "left_eye_lower_right_quarter",
        "left_eye_bottom",
        "left_eye_lower_left_quarter",
    };

    public static string[] right_eye1 =
    {
        "right_eye_left_corner",
        "right_eye_upper_left_quarter",
        "right_eye_top",
        "right_eye_upper_right_quarter",
        "right_eye_right_corner",
        "right_eye_lower_right_quarter",
        "right_eye_bottom",
        "right_eye_lower_left_quarter",
    };

    public static string[] mouth1 =
    {
        "mouth_upper_lip_left_contour3",
        "mouth_upper_lip_left_contour2",
        "mouth_upper_lip_left_contour1",
        "mouth_upper_lip_top",
        "mouth_upper_lip_right_contour1",
        "mouth_upper_lip_right_contour2",
        "mouth_upper_lip_right_contour3",
        "mouth_lower_lip_right_contour2",
        "mouth_lower_lip_right_contour3",
        "mouth_lower_lip_bottom",
        "mouth_lower_lip_left_contour3",
        "mouth_lower_lip_left_contour2",
    };

    /// <summary>
    /// 左侧鼻影
    /// </summary>
    public static string[] leftNasalShadow =
    {
        "nose_left_contour2",
        "nose_left_contour4",
        "nose_left_contour3",
    };

    /// <summary>
    /// 右侧鼻影
    /// </summary>
    public static string[] rightNasalShadow =
    {
        "nose_right_contour2",
        "nose_right_contour3",
        "nose_right_contour4",
    };

    public static string[] leftNostril = {
        "nose_left_contour4",
        "nose_left_contour5",
    };

    public static string[] rightNostril = {
        "nose_right_contour5",
        "nose_right_contour4",
    };

    public static List<Vector3> PredictEyebrow(List<Vector3> eyebrows, float height, AnimationCurve curve = null, bool inverse = false)
    {
        List<Vector3> vectors = new List<Vector3>();
        var start = eyebrows[0];
        var end = eyebrows[eyebrows.Count - 1];
        var length = Mathf.Abs(end.x - start.x);

        for (int i = 0; i < eyebrows.Count; i++)
        {
            float temp = height;
            if (curve != null)
            {
                var offset = eyebrows[i].x - start.x;
                var v = inverse ? 1 - offset / length : offset / length;
                temp = height + height * curve.Evaluate(v);
                //print(height);
            }
            var up = eyebrows[i] + new Vector3(0, temp, 0) * 0.2f;

            vectors.Add(up);
        }
        for (int i = eyebrows.Count - 1; i > 0; i--)
        {
            float temp = height;
            if (curve != null)
            {
                var offset = eyebrows[i].x - start.x;
                var v = inverse ? 1 - offset / length : offset / length;
                temp = height + height * curve.Evaluate(v);
                //print(height);
            }
            var down = eyebrows[i] + new Vector3(0, temp, 0) * -0.8f;

            vectors.Add(down);
        }


        return vectors;
    }
    #region Apple Landmark
    public static string[] apple_eyebrow_left = { "0", "1", "2", "3" };
    public static string[] apple_eyebrow_right = { "4", "5", "6", "7" };
    public static string[] apple_eye_left = { "8", "9", "10", "11", "12", "13", "14", "15" };
    public static string[] apple_eye_right = { "16", "17", "18", "19", "20", "21", "22", "23" };
    public static string[] apple_nose_left = { "53", "52", "54" };
    public static string[] apple_nose_right = { "58", "57", "56" };
    public static string[] apple_mouth = { "33", "24", "25", "26", "27", "28", "29", "30", "31", "32" };
    #endregion

    #region Tencent Landmark
    public static string[] tencent_eyebrow_left = { "37", "44", "43", "42", "41", "40", "39", "38" };
    public static string[] tencent_eyebrow_right = { "49", "50", "51", "52", "45", "46", "47", "48" };
    public static string[] tencent_eye_left = { "21", "28", "27", "26", "25", "24", "23", "22" };
    public static string[] tencent_eye_right = { "33", "34", "35", "36", "29", "30", "31", "32" };
    public static string[] tencent_nose_left = { "80", "79", "81" };
    public static string[] tencent_nose_right = { "85", "84", "83" };
    public static string[] tencent_mouth = { "53", "64", "63", "62", "61", "60", "59", "58", "57", "56", "55", "54" };
    #endregion

    #region MTShowShow Landmark
    public static string[] mt_eyebrow_left = { "33", "34", "35", "36", "37", "38", "39", "40", "41" };
    public static string[] mt_eyebrow_right = { "42", "43", "44", "45", "46", "47", "48", "49","50" };
    public static string[] mt_eye_left = { "51", "52", "53", "54", "55", "56", "57", "58" };
    public static string[] mt_eye_right = { "61", "62", "63", "64", "65", "66", "67", "68" };
    public static string[] mt_nose_left = { "77", "76", "78" };
    public static string[] mt_nose_right = { "84", "83", "82" };
    public static string[] mt_mouth = { "98", "87", "88", "89", "90", "91", "102", "93", "94", "95", "96", "97" };
    #endregion
    #region fake uv
    public static Vector3 Lerp(Vector3 a, Vector3 b, float lerp)
    {
        return Vector3.Lerp(a, b, lerp);
    }

    public static List<Vector3> PredictEye(IList<Vector3> uvs)
    {
        if (uvs.Count != 8)
            return null;
        List<Vector3> ret = new List<Vector3>();
        for (int i = 0; i < uvs.Count; i++)
        {
            var i0 = uvs[i];
            var i1 = uvs[(i + 1) % uvs.Count];
            ret.Add(i0);
            //var mid = RectLeftUp(i0, i1);
            //var b0 = Bezier(i0, mid, i1, 1f / 3f);
            //var b1 = Bezier(i0, mid, i1, 2f / 3f);
            var b0 = Lerp(i0, i1, 1f / 3f);
            var b1 = Lerp(i0, i1, 2f / 3f);
            ret.Add(b0);
            ret.Add(b1);
        }
        return ret;
    }

    public static Vector3 RectLeftUp(Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 normal = Vector3.Cross(Vector3.forward, ab).normalized;
        Vector3 c = new Vector3(a.x, b.y, 0);
        Vector3 d = new Vector3(b.x, a.y, 0);
        Vector3 ac = c - a;
        Vector3 ad = d - a;
        Vector3 ret = Vector3.Dot(ac, normal) > 0 ? c : d;
        return ret;
    }

    public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        Vector3 p0p1 = (1 - t) * p0 + t * p1;
        Vector3 p1p2 = (1 - t) * p1 + t * p2;
        Vector3 result = (1 - t) * p0p1 + t * p1p2;
        return result;
    }


    public static List<Vector3> PredictEyeCircle(IList<Vector3> uvs, float length = 0.01f)
    {
        var ret = WhateverFaceMesh.PredictOutCirClePoints(uvs, length);
        ret = PredictEye(ret);
        return ret;
    }

    public static int[] indices_left_1 =
    {
        1100,
        1099,
        1098,
        1097,
        1096,
        1095,
        1094,
        1093,
        1092,
        1091,
        1090,
        1089,
        1088,
        1087,
        1086,
        1085,
        1108,
        1107,
        1106,
        1105,
        1104,
        1103,
        1102,
        1101,
    };

    public static int[] indices_left_2 =
    {
        1181,
        1182,
        1183,
        1184,
        1185,
        1186,
        1187,
        1188,
        1189,
        1190,
        1191,
        1192,
        1193,
        1194,
        1195,
        1196,
        1197,
        1198,
        1199,
        1200,
        1201,
        1202,
        1203,
        1204,
    };
    public static int[] indices_left_3 =
    {
        417,
        56,
        55,
        53,
        54,
        50,
        51,
        52,
        169,
        171,
        357,
        358,
        426,
        48,
        47,
        40,
        41,
        42,
        45,
        44,
        43,
        356,
        416,
        46,
    };

    public static int[] indices_left_4 =
   {
        1134,
        1133,
        1156,
        1155,
        1154,
        1153,
        1152,
        1151,
        1150,
        1149,
        1148,
        1147,
        1146,
        1145,
        1144,
        1143,
        1142,
        1141,
        1140,
        1139,
        1138,
        1137,
        1136,
        1135,
    };

    public static int[] indices_right_1 =
    {
        1080,
        1079,
        1078,
        1077,
        1076,
        1075,
        1074,
        1073,
        1072,
        1071,
        1070,
        1069,
        1068,
        1067,
        1066,
        1065,
        1064,
        1063,
        1062,
        1061,
        1084,
        1083,
        1082,
        1081,
    };
    public static int[] indices_right_2 =
    {
        1168,
        1169,
        1170,
        1171,
        1172,
        1173,
        1174,
        1175,
        1176,
        1177,
        1178,
        1179,
        1180,
        1157,
        1158,
        1159,
        1160,
        1161,
        1162,
        1163,
        1164,
        1165,
        1166,
        1167,
    };
    public static int[] indices_right_3 =
    {
        789,
        788,
        620,
        618,
        502,
        501,
        500,
        504,
        503,
        505,
        506,
        847,
        496,
        846,
        787,
        493,
        494,
        495,
        492,
        491,
        490,
        497,
        498,
        854
    };

    public static int[] indices_right_4 =
    {
        1121,
        1122,
        1123,
        1124,
        1125,
        1126,
        1127,
        1128,
        1129,
        1130,
        1131,
        1132,
        1109,
        1110,
        1111,
        1112,
        1113,
        1114,
        1115,
        1116,
        1117,
        1118,
        1119,
        1120,
    };

    public static int[] indices_mouth =
    {
        //249,
        //393,
        //250,
        //251,
        //252,
        //253,
        //254,
        //255,
        //256,
        //24,
        //691,
        //690,
        //689,
        //688,
        //687,
        //686,
        //685,
        //823,
        //684,
        //834,
        //740,
        //683,
        //682,
        //710,
        //725,
        //709,
        //700,
        //25,
        //265,
        //274,
        //290,
        //275,
        //247,
        //248,
        //305,
        //404,
        395,21,825,28
    };


    public static int[] indices_nose =
    {
        313,8,748,4,
    };


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


    #endregion

    #region uv的正确度

    public static int[] meshVertexIndices =
    {
        1101,1094,1089,1108,
        1081,1076,1069,1062,
        313,8,748,4,
        395,21,825,28
    };
    public static string[] sdkRetFacePPIndex =
    {
        "left_eye_left_corner",
        "left_eye_top",
        "left_eye_right_corner",
        "left_eye_bottom",
        "right_eye_left_corner",
        "right_eye_top",
        "right_eye_right_corner",
        "right_eye_bottom",
        "nose_left_contour3",
        "nose_tip",
        "nose_right_contour3",
        "nose_middle_contour",
        "mouth_left_corner",
        "mouth_upper_lip_top",
        "mouth_right_corner",
        "mouth_lower_lip_bottom",
    };

    public static string[] sdkRetTencentIndex =
     {
        "21","27","25","23",
"33","35","29","31",
"80","75","84","82",
"53","62","59","56"
    };

    public static string[] sdkRetMTIndex =
    {
        "51","53","55","57",
"61","63","65","67",
"77","74","83","80",
"86","89","92","95",
    };

    public static Vector8 CostFunction(IList<Vector2> vectors1, IList<Vector2> vectors2)
    {
        Debug.Assert(vectors1.Count == vectors2.Count);
        var length = vectors1.Count;
        Vector4 m1 = Vector4.zero;
        Vector4 m2 = Vector4.zero;
        for (int i = 0; i < length; i+=4)
        {
            float sumX = 0;
            sumX += Mathf.Abs((vectors2[i + 0] - vectors1[i + 0]).x);
            sumX += Mathf.Abs((vectors2[i + 1] - vectors1[i + 1]).x);
            sumX += Mathf.Abs((vectors2[i + 2] - vectors1[i + 2]).x);
            sumX += Mathf.Abs((vectors2[i + 3] - vectors1[i + 3]).x);
            sumX /= 4f;
            m1[i / 4] = sumX;
            float sumY = 0;
            sumY += Mathf.Abs((vectors2[i + 0] - vectors1[i + 0]).y);
            sumY += Mathf.Abs((vectors2[i + 1] - vectors1[i + 1]).y);
            sumY += Mathf.Abs((vectors2[i + 2] - vectors1[i + 2]).y);
            sumY += Mathf.Abs((vectors2[i + 3] - vectors1[i + 3]).y);
            sumY /= 4f;
            m2[i / 4] = sumY;
        }
        
        return new Vector8() { m1 = m1,m2 = m2};
    }

    public static Vector8 CostFunction(IList<Vector2> screenUV)
    {
        if (LandmarkManager.ActiveLandmarkType == LandmarkType.FacePPOnline || LandmarkManager.ActiveLandmarkType == LandmarkType.Tencent || LandmarkManager.ActiveLandmarkType == LandmarkType.MTShowShow)
        {
            List<Vector2> ret = new List<Vector2>();
            for (int i = 0; i < meshVertexIndices.Length; i++)
            {
                ret.Add(screenUV[meshVertexIndices[i]]);
            }

            return CostFunction(ret, sdkKeyPointUV);
        }

        return Vector8.zero;
    }

	public struct Vector8
	{
		public Vector4 m1;
		public Vector4 m2;

		public static Vector8 zero = new Vector8() { m1 = Vector4.zero, m2 = Vector4.zero };

	}

    

	#endregion
}