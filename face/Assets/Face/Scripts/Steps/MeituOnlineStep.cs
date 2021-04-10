using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeituOnlineStep : StepBase
{
    private bool isFinished;
    private int MTMakeupAlpha = 100;
    private int MTBeautyAlpha = 30;
    private int id;
    private Action<bool, string, Texture> resultCallback;


    public MeituOnlineStep(FaceManager manager,int id)
        : base(manager)
    {
        this.id = id;
    }

    protected override void Init()
    {
        ReqAIBeauty_MeituOnline(_manager.data[id].faceTexture, (sucess, error, texture) =>
        {
            if (sucess)
            {
                _manager.data[id].faceTexture.DiscardContents();
                Graphics.Blit(texture, _manager.data[id].faceTexture);
                isFinished = true;
            }
            else
            {
                throw new Exception(error);
            }

        });
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


    void OnMeituOnlineHD(byte[] datas, string error)
    {
        if (datas == null)
        {
            resultCallback.Invoke(false, error, null);
            return;
        }

        Texture texture = GraphicsUtils.LoadTexture(datas);
        TexTracker.Track(texture, "beauty.hd.req");
        if (!texture)
        {
            resultCallback(false, "bytes is broken", null);
            return;
        }
        UnityEngine.Object.Destroy(texture);

        _manager.AIBeauty.GetMTMakeupPicture(datas, OnMTMakeup, MTShowShowConfig.MT_MAKEUP_0, MTMakeupAlpha, MTBeautyAlpha, 0);
    }

    void OnMTMakeup(byte[] bytes, string error)
    {
        if (!string.IsNullOrEmpty(error) || bytes == null)
        {
            resultCallback(false, error, null);
            return;
        }

        Texture texture = GraphicsUtils.LoadTexture(bytes);
        TexTracker.Track(texture, "beauty.makeup.req");
        if (!texture)
        {
            resultCallback(false, "bytes is broken", null);
            return;
        }

        resultCallback(true, null, texture);
        UnityEngine.Object.Destroy(texture);
    }

    private void ReqAIBeauty_MeituOnline(Texture input, Action<bool, string, Texture> callback)
    {
        
        _manager.AIBeauty.enabled = true;

        byte[] bytes = input.TextureEncodeToPNG();
        this.resultCallback = callback;
        _manager.AIBeauty.GetMTHDPicture(bytes, OnMeituOnlineHD);
    }
}
