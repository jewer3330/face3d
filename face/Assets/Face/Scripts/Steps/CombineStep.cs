using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombineStep : StepBase
{
    private bool isFinished = false;
    public CombineStep(FaceManager manager) : base(manager)
    {

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
        _manager.behaviour.StartCoroutine(PostProcess());
    }

    public IEnumerator PostProcess()
    {
        
        var mesh = _manager.data[0].Face;
       
        //todo 精度下降测试
        var temp = RenderTexture.GetTemporary(_manager.FaceProcess.descriptor);
        GraphicsUtils.ConvertUv(_manager.FaceProcess, _manager.data[0].faceARkitUVs, _manager.config.liuhongTop, _manager.data[0].triangles, temp);
        TexTracker.Track(temp, "texture_remap");

        Graphics.Blit(temp, _manager.ResultTexture, _manager.config.liuhongMat);
        RenderTexture.ReleaseTemporary(temp);

        TexTracker.Track(_manager.ResultTexture, "combTex");

        yield return 0;

        //烘顶点色
        var vertexLut = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        //todo 精度下降测试
        MeshUtils.GenVertexLut(vertexLut, mesh, _manager.config.liuhongTop, _manager.config.faceToHead);
        //todo 精度下降测试
        _manager.vertexLutTexture2D = vertexLut.ToNewTexture2D(TextureFormat.RGBAFloat);
        TexTracker.Track(vertexLut, "vertexLut");

        UnityEngine.Object.Destroy(vertexLut);

        isFinished = true;
    }
}
