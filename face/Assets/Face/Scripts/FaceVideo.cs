using System;
using UnityEngine;
using UnityEngine.XR.iOS;

public class FaceVideo : MonoBehaviour
{
    private FaceManager manager;

    private UnityARVideo _ARVideo;
    public UnityARVideo ARVideo
    {
        get
        {
            if (_ARVideo == null) { _ARVideo = this.GetComponent<UnityARVideo>(); }
            return _ARVideo;
        }
    }

    private RenderTexture captureImageFull;

    public Texture CaptureVideoImage()
    {
        try
        {
            Material screenMaterial = ARVideo.GetScreenMaterial();
            if (!IsReady)
            {
                //Debug.LogWarning("CaptureVideoImage: not ready");
                return null;
            }
            bool screenMaterialReady = screenMaterial;
            if (!screenMaterialReady)
                return null;

            var size = GetRawSize();

            if (!captureImageFull)
            {
                captureImageFull = new RenderTexture(size.x, size.y, 0,
                    RenderTextureFormat.Default, RenderTextureReadWrite.sRGB)
                {
                    name = "arface CARFaceVideo.captureImageFull",
                };
            }
            captureImageFull.DiscardContents();
            Graphics.Blit(null, captureImageFull, screenMaterial);

            return captureImageFull;
        }
        catch (Exception ex)
        {
            Debug.Log("CaptureVideoImage! failed!! ");
            Debug.LogException(ex);
            return null;
        }
    }


    public bool IsReady
    {
        get
        {
            return ARVideo && ARVideo.m_ClearMaterial && ARVideo.m_ClearMaterial.GetTexture("_textureY");
        }
    }

    public RenderTexture CaptureVideoImage(Rect clipBoxUV)
    {
        if (null == manager)
        {
            Debug.LogWarning("manager is not running");
            return null;
        }
        if (!IsReady)
        {
            Debug.LogWarning("CaptureVideoImage: not ready");
            return null;
        }
        try
        {
            var size = GetRawSize();
            var screenBox = RenderTexture.GetTemporary(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(null, screenBox, ARVideo.m_ClearMaterial);
            TexTracker.Track(screenBox, "CaptureVideoImage.screenBox");
            var clipBox = MeshUtils.UvRectToPixelRect(clipBoxUV, size);
            if (manager.delighting)
            {
                clipBox = Util.MakeSquare(clipBox, new RectInt(Vector2Int.zero, screenBox.Size()));
                clipBoxUV = MeshUtils.PixelRectToUvRect(clipBox, screenBox.Size());
            }
            var clipBoxRt = new RenderTexture(clipBox.width, clipBox.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            Util.Clip(screenBox, clipBoxUV, clipBoxRt);

            RenderTexture.ReleaseTemporary(screenBox);

            if (manager.delighting)
            {
                clipBoxUV = Util.ResizeRect(clipBoxUV, 1.2f);
                var temp = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var rectScale = new Rect(0, 0, 1, 1);
                rectScale = Util.ResizeRect(rectScale, 1.2f);
                Util.Clip(clipBoxRt, rectScale, temp);

                Debug.Log("CaptureVideoImage! END");
                TexTracker.Track(clipBoxRt, "CaptureVideoImage");
                Destroy(clipBoxRt);

                //clipBoxRt
                TexTracker.Track(temp, "resize");
                return temp;
            }
            else
            {
                return clipBoxRt;

            }

        }
        catch (Exception ex)
        {
            Debug.Log("CaptureVideoImage! failed!! ");
            Debug.LogException(ex);
            return null;
        }
    }


    public void Run(FaceManager manager)
    {
        this.manager = manager;
    }

    public void Stop()
    {
        this.manager = null;
    }

    private void OnDestroy()
    {
        if (captureImageFull) Destroy(captureImageFull);
    }

    private Vector2Int GetRawSize()
    {
        var textureY = ARVideo.m_ClearMaterial.GetTexture("_textureY");

        var _DisplayTransform = ARVideo.displayTransform;
        float texX = textureY.width;
        float texY = textureY.height;
        // temp fix: crop by affine transform
        // see: ARSessionNative.mm:487
        // see: YUVShaderLinear.shader:45
        // todo: ipad not support
        //        Vector2 coord = new Vector2(
        //            (Mathf.Round(_DisplayTransform.m00) * texX + Mathf.Round(_DisplayTransform.m10) * (texY)),
        //            (Mathf.Round(_DisplayTransform.m01) * texX + Mathf.Round(_DisplayTransform.m11) * (texY)));
        Vector2 coord = _DisplayTransform.MultiplyPoint(new Vector2(texX, texY));
        Vector2Int size = new Vector2Int(
            Mathf.RoundToInt(Mathf.Abs(coord.x)),
            Mathf.RoundToInt(Mathf.Abs(coord.y)));
        return size;
    }

}