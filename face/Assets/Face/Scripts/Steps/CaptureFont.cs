using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CaptureFont : StepBase
{
    public CaptureData data;
    public bool isFinished;
    public CaptureFont(FaceManager manager)
        :base(manager)
    {
        data = new CaptureData();
        isFinished = false;
    }

    public override void Finish()
    {
        _manager.data[0] = data;
        Debug.Log("CaptureFont finished");
    }

    public override bool IsFinished()
    {
        return isFinished;
    }

    public override void Update()
    {
        
        _manager.Capture(out data);
        TexTracker.Track(data.faceTexture, data.Face,"font");
        isFinished = true;
    }
}