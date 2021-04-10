using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureLeft : StepBase
{
    public CaptureData data;
    public bool isFinished;
    public CaptureLeft(FaceManager manager) : base(manager)
    {
        data = new CaptureData();
        isFinished = false;
    }

    public override void Finish()
    {
        _manager.data[1] = data;
        Debug.Log("CaptureLeft finished");
    }

    public override bool IsFinished()
    {
        return isFinished;
    }

    public override void Update()
    {
        if (_manager.IsLeftReady())
        {
            _manager.Capture(out data);
            TexTracker.Track(data.faceTexture, data.Face,"left");
            isFinished = true;
        }
    }

  
}
