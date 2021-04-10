using System;
using UnityEngine;
using UnityEngine.XR.iOS;


public class FaceManagerMono : MonoBehaviour
{
    public FaceManager FaceManager = new FaceManager();
    public FaceConfig config;
    private IStep current;

    private void Start()
    {
        if (!config)
            throw new Exception("config is null");
        FaceManager.config = config;
        FaceManager.Run(this);
    }

    private void OnDestroy()
    {
        FaceManager.Stop();
    }

    private void Update()
    {
        try
        {
            if (current != null)
            {
                current.Initialize();
                if (current.IsFinished())
                {
                    current.Finish();
                    current = current.Next();
                }
                else
                {
                    current.Update();
                }
            }
        }
        catch 
        {
            current = null;
            throw;
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Cap"))
        {
            var first = new CaptureFont(FaceManager);


            first.SetNext(new CaptureRight(FaceManager))
                 .SetNext(new CaptureLeft(FaceManager))
                 .SetNext(new RotateStep(FaceManager, 0))
                 .SetNext(new RotateStep(FaceManager, 1))
                 .SetNext(new RotateStep(FaceManager, 2))
                 .SetNext(new MeituOnlineStep(FaceManager, 0))
                 .SetNext(new MeituOnlineStep(FaceManager, 1))
                 .SetNext(new MeituOnlineStep(FaceManager, 2))
                 .SetNext(new LandmarkStep(FaceManager))
                 .SetNext(new ArkitTopStep(FaceManager, 0))
                 .SetNext(new ArkitTopStep(FaceManager, 1))
                 .SetNext(new ArkitTopStep(FaceManager, 2))
                 .SetNext(new ArkitTopStep(FaceManager, 4))
                 .SetNext(new ArkitTopStep(FaceManager, 5))
                 .SetNext(new ThreeToOne(FaceManager))
                 .SetNext(new HSLStep(FaceManager))
                 .SetNext(new GammaStep(FaceManager))
                 .SetNext(new CombineStep(FaceManager))
                 ;

            current = first;
        }

        if (GUILayout.Button("GenHead"))
        {
            var go = FaceManager.ResultGo(Instantiate(FaceManager.config.allHead.gameObject));
        }
    }
}

[System.Serializable]
public class FaceManager
{
    public MonoBehaviour behaviour;
    public bool delighting;
    public bool stop;
    public CaptureData[] data = new CaptureData[3];
    #region landmark
    public RenderTexture eyeAndMouthMask = null;
    public RenderTexture noseShadow = null;
    public RenderTexture eraseFeatureMaskTexture = null;
    #endregion

    #region 3t1
    public RenderTexture FaceProcess;
    #endregion

    #region config
    public FaceConfig config;
    #endregion

    #region result
    public RenderTexture ResultTexture;
    public Texture2D vertexLutTexture2D;

    public GameObject ResultGo(GameObject src)
    {
            return MeshUtils.RemapHead(src, vertexLutTexture2D, config.vertexMask, ResultTexture);
    }
    #endregion
    public FaceVideo faceVideo
    {
        get
        {
            if (!_faceVideo)
                _faceVideo = UnityEngine.Object.FindObjectOfType<FaceVideo>();
            return _faceVideo;
        }
    }

    private FaceVideo _faceVideo;

    public UnityARVideo unityARVideo
    {
        get
        {
            if (!_unityARVideo)
                _unityARVideo = UnityEngine.Object.FindObjectOfType<UnityARVideo>();
            return _unityARVideo;
        }
    }

    private UnityARVideo _unityARVideo;

    public MTShowShowAIBeauty AIBeauty
    {
        get
        {
            if (!_aIBeauty)
                _aIBeauty = UnityEngine.Object.FindObjectOfType<MTShowShowAIBeauty>();
            return _aIBeauty;
        }
    }
    private MTShowShowAIBeauty _aIBeauty;

    public WhateverFaceMesh WhateverFaceMesh
    {
        get
        {
            if (!_whateverFaceMesh)
                _whateverFaceMesh = UnityEngine.Object.FindObjectOfType<WhateverFaceMesh>();
            return _whateverFaceMesh;
        }
    }
    private WhateverFaceMesh _whateverFaceMesh;


    

    private readonly FaceDataAdapter faceDataAdapter = new FaceDataAdapter();
    
    public void Stop()
    {
        UnityARSessionNativeInterface.GetARSessionNativeInterface().Pause();
        stop = true;
        faceDataAdapter.Stop();
        faceVideo.Stop();
        if(FaceProcess)
            UnityEngine.Object.Destroy(FaceProcess);
        if (ResultTexture)
            UnityEngine.Object.Destroy(ResultTexture);
        if (vertexLutTexture2D)
            UnityEngine.Object.Destroy(vertexLutTexture2D);
        if (eyeAndMouthMask)
            UnityEngine.Object.Destroy(eyeAndMouthMask);
        if (noseShadow)
            UnityEngine.Object.Destroy(noseShadow);
        if (eraseFeatureMaskTexture)
            UnityEngine.Object.Destroy(eraseFeatureMaskTexture);
        behaviour = null;
    }

    public void Run(MonoBehaviour behaviour)
    {
        this.behaviour = behaviour;
        var config = new ARKitFaceTrackingConfiguration();
        if (!config.IsSupported)
            throw new NotSupportedException();

        config.enableLightEstimation = true;
        UnityARSessionNativeInterface.GetARSessionNativeInterface().RunWithConfigAndOptions(config,UnityARSessionRunOption.ARSessionRunOptionResetTracking);
        faceDataAdapter.Run(this);
        faceVideo.Run(this);
        stop = false;
        FaceProcess = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        //后处理图片
        
        ResultTexture = new RenderTexture(FaceProcess.descriptor)
        {
            name = "result.texture",
        };
    }

    public bool Capture(out CaptureData data)
    {
        return faceDataAdapter.CaptureFace(out data);
    }

    public bool IsLeftReady()
    {
        return faceDataAdapter.IsLeftOrRightCaptureReady(true);
    }

    public bool IsRightReady()
    {
        return faceDataAdapter.IsLeftOrRightCaptureReady(false);
    }

    public float FaceAngle
    {
        get
        {
            return faceDataAdapter.faceRotate;
        }
    }
}







