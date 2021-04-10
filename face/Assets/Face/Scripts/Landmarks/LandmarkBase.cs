using System;
using System.Collections.Generic;
using System.Linq;
using ARFace;
using ARFace.Landmarks;
using UnityEngine;

public abstract class LandmarkBase : MonoBehaviour
{
    public LandmarkType type = (LandmarkType) (-1);
    protected Dictionary<string, Vector2> m_dict = new Dictionary<string, Vector2>();
    protected Action<Dictionary<string, Vector2>> m_uvPosCallback = null;
    protected Action<float> m_uploadProgressCallback = null;
    protected byte[] m_bytes = null;
    protected int m_photoHeight = 0;
    protected int m_photoWidth = 0;

    protected virtual void Start()
    {

    }

    public void GetPhotoUVPosition(Texture texture, Action<Dictionary<string, Vector2>> callback, Action<float> uploadProgressCallback = null)
    {
        var data = texture.TextureEncodeToPNG();
        var textureSize = texture.Size();
        // System.IO.File.WriteAllBytes(Application.persistentDataPath + string.Format("/cap{0}.png", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")), data);
        if (!LandmarkManager.ActiveLandmarkType.IsResultUvSpace())
        {
            var originCallback = callback;
            callback = originDict =>
            {
                var dict = originDict.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value / textureSize);
                originCallback(dict);
            };
        }
        GetPhotoUVPosition(data, texture.width, texture.height, callback, uploadProgressCallback);
    }

    protected abstract void GetPhotoUVPosition(byte[] bytes, int photoWidth, int photoHeight, Action<Dictionary<string, Vector2>> callback, Action<float> uploadProgressCallback);


    #region 顶点与landmark误差统计
    //Landmark对应的左眼顶点索引
    public static int[] LeftEyeMeshVertexIndices =
    {
        1101,1094,1089,1108
    };
    //Landmark对应的右眼顶点索引
    public static int[] RightEyeMeshVertexIndices =
    {
        1081,1076,1069,1062
    };
    //Landmark对应的鼻子顶点索引
    public static int[] NoseMeshVertexIndices =
    {
        313,8,748,4
    };
    //Landmark对应的嘴巴顶点索引
    public static int[] MouthMeshVertexIndices =
    {
        395,21,825,28
    };

 
    #endregion
}
