using UnityEngine;
using UnityEngine.XR.iOS;
using System.Linq;
using System.Collections.Generic;

public class FaceDataAdapter
{
    public FaceManager manager;
    public float faceRotate;

    private Mesh faceMesh;
    private ARFaceAnchor lastAnchorData;

    private Vector3[] faceVertices;
    private Vector2[] faceUV;
    private int[] faceTriangles;

    private Transform faceGo;
    private Transform leftEye;
    private Transform rightEye;

    private ScreenOrientation m_curOrientation = ScreenOrientation.Unknown;
    private Vector2 m_FaceAngle = Vector2.zero;

    private List<float> angles = new List<float>();

    /// <summary>
    /// 稳定最大的角度
    /// </summary>
    private int stableMaxAngle = 10;

    /// <summary>
    /// 多少帧稳定
    /// </summary>
    private int stableFrameCount = 5;

    /// <summary>
    /// 头部转到多少度可以拍照
    /// </summary>
    private int headRotateMinAngle = 20;

    public void Run(FaceManager manager)
    {
        this.manager = manager;
#if UNITY_EDITOR
        UnityARSessionNativeInterface.ARFaceAnchorAddedEvent += FaceUpdated;
        UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent += FaceUpdated;
        UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent += FaceRemoved;
#endif

        UnityARSessionNativeInterface.ARFrameUpdatedEvent += FrameUpdated;
        if (!faceGo)
        {
            faceGo = new GameObject("FaceDataAdapterGo").transform;
            faceGo.gameObject.AddComponent<MeshFilter>();
            faceGo.gameObject.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color"));
            faceGo.transform.parent = manager.faceVideo.transform.parent;
        }
        if (!leftEye)
        {
            leftEye = new GameObject("leftEye").transform;
            leftEye.transform.parent = faceGo.transform;
        }
        if (!rightEye)
        {
            rightEye = new GameObject("rightEye").transform;
            rightEye.transform.parent = faceGo.transform;

        }
    }

    public void Stop()
    {
#if UNITY_EDITOR
        UnityARSessionNativeInterface.ARFaceAnchorAddedEvent -= FaceUpdated;
        UnityARSessionNativeInterface.ARFaceAnchorUpdatedEvent -= FaceUpdated;
        UnityARSessionNativeInterface.ARFaceAnchorRemovedEvent -= FaceRemoved;
#endif

        UnityARSessionNativeInterface.ARFrameUpdatedEvent -= FrameUpdated;
        this.manager = null;
        if (faceGo)
            Object.Destroy(faceGo.gameObject);
        if (leftEye)
            Object.Destroy(leftEye.gameObject);
        if (rightEye)
            Object.Destroy(rightEye.gameObject);
    }

    public bool CaptureFace(out CaptureData data)
    {

        if (faceVertices == null)
        {
            data = null;
            return false;
        }
        var vid = manager.faceVideo;
        var cam = manager.faceVideo.GetComponent<Camera>();
        // -- uvs
        var tex_uvs = new Vector2[faceVertices.Length];


        for (int i = 0, n = faceVertices.Length; i < n; i++)
        {
            var lp = faceGo.TransformPoint(faceVertices[i]);
            var p = cam.WorldToViewportPoint(lp);
            tex_uvs[i] = p;
        }

        var clipBox = tex_uvs.CalcBounds();

        clipBox = Util.ResizeRect(clipBox, 1.5f);
        var captureTex = vid.CaptureVideoImage(clipBox);

        for (var i = 0; i < tex_uvs.Length; i++)
        {
            tex_uvs[i] = Rect.PointToNormalized(clipBox, tex_uvs[i]);
        }

        var tex = captureTex;

        data = new CaptureData
        {
            vertices = faceVertices,
            triangles = CombinedEyeTriangles(CombinedMouthTriangles(faceTriangles, FaceConfig.DefaultFaceMeshMouthBorder), FaceConfig.DefaultFaceLeftEyeBorder, FaceConfig.DefaultFaceRightEyeBorder),
            faceARkitUVs = Util.FlipUVVertical(faceUV),
            faceTextureUVs = tex_uvs,
            faceTexture = tex,
            faceARKitTexture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        };
        return true;
    }


    public bool IsLeftOrRightCaptureReady(bool left)
    {
        float faceAngle = GetFaceAngleNonFront();
        if (!IsStable(faceAngle, stableFrameCount))
            return false;

        if (left)
        {
            return 90 - faceAngle > headRotateMinAngle;
        }
        else // right
        {
            return faceAngle - 90 > headRotateMinAngle;
        }
    }

    ///<summary>缝合眼睛（只缝合面，不增加顶点）</summary>
    public static int[] CombinedEyeTriangles(IList<int> triangles, short[] leftEyeBorder, short[] rightEyeBorder)
    {
        //int oldCount = mesh.triangles.Length;

        List<int> newTriangles = new List<int>(triangles);
        int oldCount = newTriangles.Count;
        for (int i = 0; i < rightEyeBorder.Length; ++i)
        {
            int id2 = i + 1;
            int id3 = i + 2;
            if (id2 < rightEyeBorder.Length && id3 < rightEyeBorder.Length)
            {
                newTriangles.Add(rightEyeBorder[i]);
                newTriangles.Add(0 != i % 2 ? rightEyeBorder[id2] : rightEyeBorder[id3]);
                newTriangles.Add(0 != i % 2 ? rightEyeBorder[id3] : rightEyeBorder[id2]);
            }
        }

        for (int i = 0; i < leftEyeBorder.Length; ++i)
        {
            int id2 = i + 1;
            int id3 = i + 2;
            if (id2 < leftEyeBorder.Length && id3 < leftEyeBorder.Length)
            {
                newTriangles.Add(leftEyeBorder[i]);
                newTriangles.Add(0 != i % 2 ? leftEyeBorder[id3] : leftEyeBorder[id2]);
                newTriangles.Add(0 != i % 2 ? leftEyeBorder[id2] : leftEyeBorder[id3]);
            }
        }


        return newTriangles.ToArray();
    }

    public static List<int> CombinedMouthTriangles(IList<int> triangles, short[] mouthBorder)
    {
        var newTriangles = new List<int>(triangles);
        int oldCount = newTriangles.Count;
        for (int i = 0; i < mouthBorder.Length; ++i)
        {
            int id2 = i + 1;
            int id3 = i + 2;
            if (id2 < mouthBorder.Length && id3 < mouthBorder.Length)
            {
                newTriangles.Add(mouthBorder[i]);
                newTriangles.Add(0 != i % 2 ? mouthBorder[id2] : mouthBorder[id3]);
                newTriangles.Add(0 != i % 2 ? mouthBorder[id3] : mouthBorder[id2]);
            }
        }

        return newTriangles;
    }

    #region private functions



    /// <summary>
    /// Is the angle stable in <paramref name="count"/> frames.
    /// </summary>
    /// <returns><c>true</c>, if stable , <c>false</c> otherwise.</returns>
    /// <param name="angle">Angle.</param>
    /// <param name="count">Count.</param>
    bool IsStable(float angle, int count)
    {
        if (float.IsInfinity(angle) || float.IsNaN(angle))
        {
            angles.Clear();
            return false;
        }

        angles.Add(angle);
        if (angles.Count > count)
        {
            var max_x = float.MinValue;
            var min_x = float.MaxValue;
            foreach (var q in angles)
            {
                var euler = q;
                max_x = Mathf.Max(euler, max_x);
                min_x = Mathf.Min(euler, min_x);

            }
            if (max_x - min_x < stableMaxAngle)
            {
                angles.Clear();
                return true;
            }
            angles.RemoveAt(0);
        }

        return false;
    }

    float GetFaceAngleNonFront()
    {

        if (lastAnchorData == null)
            return float.NaN;

        ScreenOrientation curOrientation = GetScreenOrientation();

        var vid = manager.faceVideo;

        Vector3 faceForward = vid.transform.InverseTransformDirection(lastAnchorData.transform.MultiplyPoint3x4(Vector3.forward));
        Vector2 vec01 = new Vector2(0, 1), vec10 = new Vector2(1, 0);

        Vector2 flr2df = faceForward.xz();
        Vector2 fud2df = faceForward.yz();
        float lrAng = Vector2.Angle(flr2df, vec01);
        float lrRAng = Vector2.Angle(flr2df, vec10);

        float udAng = Vector2.Angle(fud2df, vec01);
        float udUAng = Vector2.Angle(fud2df, vec10);

        m_FaceAngle.Set(udUAng, lrRAng);

        bool bLeft = curOrientation == ScreenOrientation.LandscapeLeft,
            bRight = curOrientation == ScreenOrientation.LandscapeRight,
            bPorUpsideDown = curOrientation == ScreenOrientation.PortraitUpsideDown;

        float resultLrAngle = (bLeft || bRight) ? lrRAng : udUAng;
        if (bRight || bPorUpsideDown)
        {
            resultLrAngle = resultLrAngle > 90 ? 90 - (resultLrAngle - 90) : 90 + (90 - resultLrAngle);
        }

        return resultLrAngle;
    }

    ScreenOrientation GetScreenOrientation()
    {
        ScreenOrientation retSO = m_curOrientation;
        Vector3 ve3Gravity = Input.gyro.gravity;
        if (Mathf.Abs(ve3Gravity.z) <= 0.9f)
        {
            if (Mathf.Abs(ve3Gravity.x) - Mathf.Abs(ve3Gravity.y) > 0.6f)
            {
                if (ve3Gravity.x > 0f)
                {
                    //"右竖屏"
                    retSO = ScreenOrientation.Portrait;
                }
                else
                {
                    //"左竖屏"
                    retSO = ScreenOrientation.PortraitUpsideDown;
                }
            }
            else if (ve3Gravity.y > 0f)
            {
                //"倒横屏"
                retSO = ScreenOrientation.LandscapeRight;
            }
            else if (Mathf.Abs(ve3Gravity.x) < 0.15f)
            {
                //"正横屏"
                retSO = ScreenOrientation.LandscapeLeft;
            }
        }
        return retSO;
    }

    enum ScreenOrientation
    {
        Unknown = 0,
        Portrait = 1,
        PortraitUpsideDown = 2,
        LandscapeLeft = 3,
        LandscapeRight = 4
    }

    void FaceRemoved(ARFaceAnchor anchorData)
    {
        faceMesh = null;
    }

    void FrameUpdated(UnityARCamera unityArCamera)
    {
        // dispatch
        UnityARVideo arVideo = manager.unityARVideo;
        var camera = arVideo.GetComponent<Camera>();
        UpdateCamera(camera);
        arVideo.UpdateFrame(unityArCamera);


#if !UNITY_EDITOR // 非editor下使用强同步逻辑
        // update anchors
        ARFaceAnchor anchor = unityArCamera.faceAnchors.FirstOrDefault(face => face.isTracked);
        if (anchor != null)
        {
            FaceUpdated(anchor);
        }
        else
        {
            FaceRemoved(null);
        }
#endif
    }

    void FaceUpdated(ARFaceAnchor anchorData)
    {

        var isTracked = anchorData.isTracked;
        if (isTracked)
        {
            faceGo.localPosition = UnityARMatrixOps.GetPosition(anchorData.transform);
            faceGo.localRotation = UnityARMatrixOps.GetRotation(anchorData.transform);


            faceVertices = anchorData.faceGeometry.vertices;
            faceUV = anchorData.faceGeometry.textureCoordinates;
            faceTriangles = anchorData.faceGeometry.triangleIndices;

            UpdateFaceMesh();

            leftEye.transform.localPosition = anchorData.leftEyePose.position;
            leftEye.transform.localRotation = anchorData.leftEyePose.rotation;
            rightEye.transform.localPosition = anchorData.rightEyePose.position;
            rightEye.transform.localRotation = anchorData.rightEyePose.rotation;

            UnityARVideo arVideo = manager.unityARVideo;
            OnFaceRotation(faceGo.localRotation * Quaternion.Inverse(arVideo.transform.rotation));
        }

        lastAnchorData = anchorData;

    }

    void OnFaceRotation(Quaternion rotation)
    {
        var euler = rotation.eulerAngles;
        if (Mathf.Approximately(Mathf.Round(Input.gyro.gravity.z), -1f))
        {
            faceRotate = Mathf.Round(euler.y / 90) * 90;
        }
        else
        {
            faceRotate = Mathf.Round(euler.z / 90) * 90;
        }
        // 输出限制为0或90，修复Quaternion.euler.z带来的180角度误差
        faceRotate = Mathf.Abs(faceRotate % 180);
    }

    void UpdateFaceMesh()
    {
        if (faceMesh == null)
        {
            faceMesh = new Mesh();
        }
        faceMesh.vertices = faceVertices;
        faceMesh.uv = faceUV;
        faceMesh.triangles = faceTriangles;

        faceMesh.RecalculateBounds();
        faceMesh.RecalculateNormals();

        faceGo.gameObject.GetComponent<MeshFilter>().sharedMesh = faceMesh;
    }

    void UpdateCamera(Camera camera)
    {
        // see: Assets/UnityARKitPlugin/Examples/FaceTracking/ARCameraTracker.cs
        Matrix4x4 cameraPose = UnityARSessionNativeInterface.GetARSessionNativeInterface().GetCameraPose();
        camera.transform.localPosition = UnityARMatrixOps.GetPosition(cameraPose);
        camera.transform.localRotation = UnityARMatrixOps.GetRotation(cameraPose);

        camera.projectionMatrix = UnityARSessionNativeInterface.GetARSessionNativeInterface().GetCameraProjection();
    }
    #endregion
}

