using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ARFace.Landmarks;
using UnityEngine;

public class LandmarkStep : StepBase
{
    protected bool isFinished;
    protected RenderTexture eyeAndMouthMask = null;
    protected RenderTexture noseShadow = null;
    protected RenderTexture eraseFeatureMaskTexture = null;

    public LandmarkStep(FaceManager manager) : base(manager)
    {

    }

    public override void Finish()
    {
        _manager.eyeAndMouthMask = eyeAndMouthMask;
        _manager.noseShadow = noseShadow;
        _manager.eraseFeatureMaskTexture = eraseFeatureMaskTexture;
    }

    public override bool IsFinished()
    {
        return isFinished;
    }

    public override void Update()
    {
    }

    protected override void Init()
    {
        ReqFaceLandmark();
    }

    protected void Set()
    {
        _manager.WhateverFaceMesh.callbackEyeAndMouth = (tex, dict) =>
        {
            if (tex)
            {
                eyeAndMouthMask = new RenderTexture(tex.descriptor);
                Graphics.Blit(tex, eyeAndMouthMask);
            }
        };
        _manager.WhateverFaceMesh.callbackNoseShadow = (tex) =>
        {
            if (tex)
            {
                noseShadow = new RenderTexture(tex.descriptor);
                Graphics.Blit(tex, noseShadow);
            }
        };
        _manager.WhateverFaceMesh.callbackEraseFeature = (tex) =>
        {
            if (tex)
            {
                eraseFeatureMaskTexture = new RenderTexture(tex.descriptor);
                Graphics.Blit(tex, eraseFeatureMaskTexture);
            }
        };
    }

    protected virtual void ReqFaceLandmark()
    {
        

        CaptureData data = _manager.data[0];
        TexTracker.Track(data.faceTexture, "LandmarkStep.input");
        Set();
        _manager.WhateverFaceMesh.callbackFinal = (success, landmarkDict) =>
        {
            if (success)
            {
                data.faceLandmarkList = landmarkDict.Values.ToList();
                isFinished = true;
            }
            
        };

        LandmarkManager.GetLandmark(data.faceTexture, LandmarkManager.ActiveLandmarkType, _manager.WhateverFaceMesh.OnRecvFaceData);

    }
}


public class LandmarkApplyStep : LandmarkStep
{
   
    public LandmarkApplyStep(FaceManager manager) : base(manager)
    {

    }


    protected override void ReqFaceLandmark()
    {

        CaptureData data = _manager.data[0];
        TexTracker.Track(data.faceTexture, "LandmarkApplyStep.input");

        Set();
        _manager.WhateverFaceMesh.callbackFinal = (success, landmarkDict) =>
        {
            if (success)
            {
                isFinished = true;
            }

        };
        _manager.WhateverFaceMesh.OnRecvFaceData(GetDic(data.faceLandmarkList));
    }

    private Dictionary<string, Vector2> GetDic(List<Vector2> list)
    {
        Dictionary<string, Vector2> ret = new Dictionary<string, Vector2>();
        for(int i = 0;i < list.Count;i++)
        {
            ret.Add(i.ToString(), list[i]);
        }
        return ret;
    }
}