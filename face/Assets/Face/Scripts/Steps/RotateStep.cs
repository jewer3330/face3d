using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateStep : StepBase
{
    private bool isFinished;
    private readonly int id;
    public RotateStep(FaceManager manager,int id) : base(manager)
    {
        this.id = id;
    }

    public override void Finish()
    {
       
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
        RenderTexture temp = Util.RotateTexture(_manager.data[id].faceTexture, _manager.data[id].faceAngle);
        Object.Destroy(_manager.data[id].faceTexture);
        _manager.data[id].faceTexture = temp;
        _manager.data[id].faceTextureUVs = Util.RotateVector2(_manager.data[id].faceTextureUVs, _manager.data[id].faceAngle).ToArray();
        isFinished = true;
    }
}
