using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ARFace.Landmarks;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

public class MTShowShowLandmark : LandmarkBase
{
    

    private void Awake()
    {
        type = LandmarkType.MTShowShow;
    }

    protected override void GetPhotoUVPosition(byte[] bytes, int photoWidth, int photoHeight, Action<Dictionary<string, Vector2>> callback, Action<float> uploadProgressCallback)
    {

        if (null == bytes || bytes.Length < 1)
        {
            Debug.Log("bytes is null or bytes.length < 1!!");
            return;
        }

        m_bytes = bytes;
        m_photoHeight = photoHeight;
        m_photoWidth = photoWidth;
        m_uvPosCallback = callback;
        m_uploadProgressCallback = uploadProgressCallback;

        StartCoroutine(GetLandmark());
    }

    private float m_fPreUp = -1;
    private IEnumerator GetLandmark()
    {
        string img64 = Convert.ToBase64String(m_bytes);
        //1 表示返回 106 点百分比，2 表示返回 106 点实值，3 表示 118 点百分比，4 表示 118 点实值，5 表示 171 点百分比，6 表示 171 点实值
        string json = JsonMapper.ToJson(new MyLandmark(img64, 2));//**注意：用实值**
        //Debug.Log("json = " + json);
        UploadHandlerRaw uhr = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json))
        {
            contentType = "application/json"
        };
        string url = string.Format(MTShowShowConfig.MT_LANDMARK_URL, MTShowShowConfig.MT_FACE_APP_KEY, MTShowShowConfig.MT_FACE_SECRET_ID);
        UnityWebRequest uwr = UnityWebRequest.Post(url, json);
        uwr.uploadHandler = uhr;
        uwr.timeout = 30;
        uwr.SendWebRequest();

        while (!uwr.isDone)
        {
            float preUP = uwr.uploadProgress;
            if (preUP > m_fPreUp)
            {
                Debug.LogFormat("upload progress {0}%", preUP * 100f);
                m_fPreUp = preUP;
                if (null != m_uploadProgressCallback)
                {
                    m_uploadProgressCallback(preUP * 100f);
                }
            }

            yield return 1;
        }

        if (uwr.isHttpError || uwr.isNetworkError)
        {
            Debug.LogErrorFormat("Request MTShowShow API Failed! url = {0}, www.error = {1}", url, uwr.error);
            Debug.LogErrorFormat("uwr.downloadHandler.text = {0}", uwr.downloadHandler.text);
            if (m_uvPosCallback != null)
            {
                m_dict.Clear();
                m_uvPosCallback(m_dict);
            }
        }
        else
        {
            try
            {
                //Debug.LogFormat("rootJson = {0}", uwr.downloadHandler.text);
                JsonData rootData = JsonMapper.ToObject(uwr.downloadHandler.text);
                JsonData mediaInfoData = rootData["media_info_list"];
                JsonData mediaExtra = mediaInfoData[0]["media_extra"];
                JsonData faces = mediaExtra["faces"];
                JsonData face0 = faces[0];
                JsonData landmark = face0["face_landmark"];
                m_dict.Clear();
                for (int i = 0; i < landmark.Count; i++)
                {
                    JsonData ld = landmark[i];
                    float x = float.Parse(ld[0].ToString());
                    float y = float.Parse(ld[1].ToString());
                    y = m_photoHeight - y;
                    m_dict.Add(i.ToString(), new Vector2(x, y));
                }

                if (m_uvPosCallback != null)
                {
                    m_uvPosCallback(m_dict);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (m_uvPosCallback != null)
                {
                    m_dict.Clear();
                    m_uvPosCallback(m_dict);
                }
            }
        }
    }

}