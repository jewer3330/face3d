using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArkitTopStep : StepBase
{
    private int id;
    private RenderTexture src;
    private RenderTexture dest;

    private Vector2[] uvsrc;
    private Vector2[] uvdest;
    private int[] triangles;

    public ArkitTopStep(FaceManager manager,int id) : base(manager)
    {
        this.id = id;
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
        if (id < 3)
        {
            src = _manager.data[id].faceTexture;
            dest = _manager.data[id].faceARKitTexture;
            uvsrc = _manager.data[id].faceTextureUVs;
            uvdest = _manager.data[id].faceARkitUVs;
            triangles = _manager.data[id].triangles;
        }
        else if (id == 3)
        {
            src = _manager.eyeAndMouthMask;
            dest = null;
            uvsrc = _manager.data[0].faceTextureUVs;
            uvdest = _manager.data[0].faceARkitUVs;
            triangles = _manager.data[0].triangles;
        }
        else if (id == 4)
        {
            src = _manager.noseShadow;
            dest = null;
            uvsrc = _manager.data[0].faceTextureUVs;
            uvdest = _manager.data[0].faceARkitUVs;
            triangles = _manager.data[0].triangles;
        }
        else if (id == 5)
        {
            src = _manager.eraseFeatureMaskTexture;
            dest = null;
            uvsrc = _manager.data[0].faceTextureUVs;
            uvdest = _manager.data[0].faceARkitUVs;
            triangles = _manager.data[0].triangles;
        }
      

        if (!src)
            throw new System.Exception("src is null");
        bool back = false;
        if (!dest)
        {
            dest = RenderTexture.GetTemporary(1024, 1024, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            back = true;
        }
        GraphicsUtils.ConvertUv(src, uvsrc, uvdest, triangles, dest);

        if (back)
        {
            src.DiscardContents();
            Graphics.Blit(dest, src);
            RenderTexture.ReleaseTemporary(dest);
        }
    }


}
