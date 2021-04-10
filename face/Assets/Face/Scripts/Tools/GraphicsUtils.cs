using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

public static class GraphicsUtils
{
    public enum TextureConvert
    {
        None,
        LinearToGamma,
        GammaToLinear,
    }

    public static int Channels(this TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.ARGB4444:
            case TextureFormat.RGBA32:
            case TextureFormat.ARGB32: return 4;
            case TextureFormat.RGB24:
            case TextureFormat.RGB565: return 3;
            case TextureFormat.Alpha8:
            case TextureFormat.R8: return 1;
            default:
                throw new NotSupportedException();
        }
    }

    public static int Channels(this RenderTextureFormat format)
    {
        switch (format)
        {
            case RenderTextureFormat.ARGB32:
            case RenderTextureFormat.ARGBHalf:
            case RenderTextureFormat.ARGB4444:
            case RenderTextureFormat.ARGB1555:
            case RenderTextureFormat.ARGB2101010:
            case RenderTextureFormat.ARGB64:
            case RenderTextureFormat.ARGBFloat: return 4;
            case RenderTextureFormat.RGB565: return 3;
            case RenderTextureFormat.RGFloat:
            case RenderTextureFormat.RGHalf: return 2;
            case RenderTextureFormat.RFloat:
            case RenderTextureFormat.RHalf:
            case RenderTextureFormat.R8: return 1;

            default:
                throw new NotSupportedException();
        }
    }

    public static byte[] TextureEncodeToPNG(this Texture @this, TextureConvert convert = TextureConvert.None,
        TextureFormat pngFormat = TextureFormat.RGBA32)
    {
        if (convert == TextureConvert.None)
        {
            if (@this is Texture2D )
            {
                var tex0 = @this as Texture2D;
                int channelsActual = tex0.format.Channels();
                int channelsExpect = pngFormat.Channels();
                if (!(channelsActual == channelsExpect || channelsActual == 3 && channelsExpect == 4))
                {
                    Debug.LogWarningFormat("Tex channels not match: \"{0}\",{1}, expect: {2}", tex0.name, tex0.format, pngFormat);
                }

                if (tex0.isReadable)
                {
                    return tex0.EncodeToPNG();
                }
                else
                {
                    Texture2D tex2D = new Texture2D(tex0.width, tex0.height);
                    try
                    {
                        Graphics.CopyTexture(tex0, tex2D);
                        return tex2D.EncodeToPNG();
                    }
                    finally
                    {
                        Util.DestroyRes(tex2D);
                    }
                }
            }
            else if (@this is RenderTexture)
            {
                Texture2D tex = ((RenderTexture)@this).ToNewTexture2D(pngFormat);
                try
                {
                    return tex.EncodeToPNG();
                }
                finally
                {
                    Util.DestroyRes(tex);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        else
        {
            RenderTexture rt = null;
            Material convertMat = null;
            try
            {
                rt = RenderTexture.GetTemporary(@this.width, @this.height);
                convertMat = new Material(Shader.Find("Hidden/ColorSpaceConvert"));
                Graphics.Blit(@this, rt, convertMat, convert == TextureConvert.LinearToGamma ? 0 : 1);
                return rt.TextureEncodeToPNG(TextureConvert.None, pngFormat);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                Util.DestroyRes(convertMat);
            }
        }
    }

    public static Vector2Int Size(this Texture @this)
    {
        return new Vector2Int(@this.width, @this.height);
    }

    public static void ConvertUv(Texture tex, Vector2[] uv, Vector2[] toUV, int[] triangles, RenderTexture dest,Material material = null, Texture mask = null)
    {
        Assert.AreEqual(uv.Length, toUV.Length);

        bool destroyMat = false;
        if (null == material)
        {
            material = new Material(Shader.Find("Unlit/FaceMaskConvert"));
            destroyMat = true;
        }

        material.mainTexture = tex;
        if (material.HasProperty("_MaskTex"))
        {
            material.SetTexture("_MaskTex", mask);
        }

        var uvMesh = new Mesh
        {
            vertices = toUV.Select(item => (Vector3) item).ToArray(),
            triangles = triangles,
            uv = uv,
            uv4 = toUV,
        };
        DrawUvMesh(uvMesh, material, dest, clear: Color.clear);
        if (destroyMat && material)
        {
            Object.Destroy(material);
        }

        TexTracker.Track(tex, "ARFace.GraphicsUtils.ConvertUv/src");
        TexTracker.Track(dest, "ARFace.GraphicsUtils.ConvertUv/target");
    }

    public static void DrawUvMesh(Mesh uvMesh, Material material, RenderTexture target, Color? clear = null,bool debug = false)
    {
        if (!uvMesh)
            throw new ArgumentNullException();
        if (!target)
            throw new ArgumentNullException();
        
        // 强制覆盖bounds
        uvMesh.bounds = new Bounds
        {
            center = new Vector3(0.5f, 0.5f, 0),
            extents = new Vector3(0.5f, 0.5f, 0),
        };

        GameObject goCamera = null;
        GameObject goRenderer = null;
        try
        {
            goCamera = new GameObject("<GraphicsUtils.DrawMesh.Camera>");
            var camera = goCamera.AddComponent<Camera>();
            camera.orthographic = true;
            if (!clear.HasValue) {
                camera.clearFlags = CameraClearFlags.Nothing;
            }
            else
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = clear.Value;
            }
            camera.orthographicSize = 0.5f;
            camera.transform.position = new Vector3(0.5f, 0.5f, 0);
            camera.nearClipPlane = -1;
            camera.farClipPlane = 1;
            camera.useOcclusionCulling = false;
            camera.cullingMask = LayerMask.GetMask("Ignore Raycast");

            goRenderer = new GameObject("<GraphicsUtils.DrawMesh.Renderer>");
            goRenderer.AddComponent<MeshFilter>().mesh = uvMesh;
            goRenderer.layer = LayerMask.NameToLayer("Ignore Raycast");
            var r = goRenderer.AddComponent<MeshRenderer>();
            r.material = material;

            //if (transform.HasValue)
            //{
            //    goCamera.transform.SetParent(goRenderer.transform, false);
            //    TRS transformNoScale = new TRS(transform.Value) {S = Vector3.one};
            //    TRS.SetTransformLocal(goRenderer.transform, transformNoScale);
            //}

            camera.targetTexture = target;
            camera.Render();
            if (!debug) camera.targetTexture = null;
        }
        finally
        {
            if (goCamera && !debug)
                Object.Destroy(goCamera);
            if (goRenderer && !debug)
                Object.Destroy(goRenderer);
        }
    }


      public static Texture2D LoadTexture(byte[] imageBytes, bool apply = false)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(imageBytes);
            if (apply) tex.Apply(false);
            return tex;
        }

        public static unsafe Texture2D LoadTexture(Color32* color, int width, int height, bool sRGB = true)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, !sRGB);
            var colorArray = new Color32[width * height];
            for (int i = 0; i < colorArray.Length; i++) colorArray[i] = color[i];
            tex.SetPixels32(colorArray);
            tex.Apply(false);
            return tex;
        }

    public static Color GetPixelPoint(this Texture2D texture, float x, float y)
    {
        int width = texture.width;
        int height = texture.height;
        Color ret = texture.GetPixel((int)(x * width), (int)(y * height));
        return ret;
    }
}
