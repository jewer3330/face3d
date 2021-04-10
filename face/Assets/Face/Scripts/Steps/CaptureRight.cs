using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CaptureRight : StepBase
{
    public CaptureData data;
    public bool isFinished;
    public CaptureRight(FaceManager manager) : base(manager)
    {
        data = new CaptureData();
        isFinished = false;
    }

    public override void Finish()
    {
        _manager.data[2] = data;
        Debug.Log("CaptureRight finished");

    }

    public override bool IsFinished()
    {
        return isFinished;
    }

    public override void Update()
    {
        if (_manager.IsRightReady())
        {
            _manager.Capture(out data);
            TexTracker.Track(data.faceTexture, data.Face,"right");
            isFinished = true;
        }
    }


}
