using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ThreeToOne : StepBase
{
    public ThreeToOne(FaceManager manager) : base(manager)
    {

    }

    public override void Finish()
    {
        
    }

    public override bool IsFinished()
    {
        return true;
    }

    public override void Update()
    {
        
    }

    protected override void Init()
    {
        ProcessMultiCap(_manager.data[0].faceARKitTexture, _manager.data[1].faceARKitTexture, _manager.data[2].faceARKitTexture, _manager.FaceProcess);
    }

    void ProcessMultiCap(RenderTexture main, RenderTexture left, RenderTexture right, RenderTexture dest)
    {
        if (main && left && right)
        {

            Material mat = _manager.config.threeToOne;
            mat.SetTexture("_MainTex", main);
            mat.SetTexture("_LeftTex", left);
            mat.SetTexture("_RightTex", right);

            dest.DiscardContents();
            Graphics.Blit(null, dest, mat);
            TexTracker.Track(dest, "3t1");
        }

    }
}


public class HSLStep : StepBase
{
    private Material hslMat;
    public HSLStep(FaceManager manager) : base(manager)
    {

    }

    public override void Finish()
    {

    }

    public override bool IsFinished()
    {
        Object.Destroy(hslMat);
        return true;
    }

    public override void Update()
    {

    }

    protected override void Init()
    {
        hslMat = new Material(Shader.Find("Custom/HSLShader"));
        ChangeHSL(_manager.config.hsl);
        var temp = RenderTexture.GetTemporary(_manager.FaceProcess.descriptor);
        Graphics.Blit(_manager.FaceProcess, temp, hslMat);
        TexTracker.Track(temp, "hsl.temp");

        _manager.FaceProcess.DiscardContents();
        Graphics.Blit(temp, _manager.FaceProcess);
        TexTracker.Track(_manager.FaceProcess, "hsl");
    }

    void ChangeHSL(Vector3 hsl)
    {
        var h = Mathf.Lerp(0f, 1f, hsl.x);
        var s = Mathf.Lerp(0f, 1f, hsl.y);
        var l = Mathf.Lerp(0f, 1f, hsl.z);
        hslMat.SetFloat("_H", h);
        hslMat.SetFloat("_S", s);
        hslMat.SetFloat("_L", l);
    }
}


public class GammaStep : StepBase
{
    public GammaStep(FaceManager manager) : base(manager)
    {

    }

    public override void Finish()
    {

    }

    public override bool IsFinished()
    {
        return true;
    }

    public override void Update()
    {

    }

    protected override void Init()
    {
        SpStylize(_manager.eyeAndMouthMask,_manager.config.gamma);
    }
    private void SpStylize(RenderTexture faceMask,float gamma = 0.433f)
    {
        var mat = new Material(Shader.Find("Hidden/ImageLevels"));
        if (!faceMask)
        {
            Debug.LogWarning("SpStylize: no facePPBlend");
        }
        else
        {
            mat.SetTexture("_MaskTex", faceMask);
        }
        mat.SetFloat("_Gamma", gamma);
        mat.SetFloat("_Blend", 1);
        mat.SetVector("_Param", new Vector4(0, 1, 0, 1));

        var temp = RenderTexture.GetTemporary(_manager.FaceProcess.descriptor);
        Graphics.Blit(_manager.FaceProcess, temp, mat);
        TexTracker.Track(temp, "sp.temp");

        _manager.FaceProcess.DiscardContents();
        Graphics.Blit(temp, _manager.FaceProcess);
        TexTracker.Track(_manager.FaceProcess, "sp");

        Object.Destroy(mat);
    }

}
