using System;
using System.Collections;
using System.Text;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

public class MTShowShowAIBeauty : MonoBehaviour
{
    //高清人像
    private readonly int TYPE_MT_HDPICTURE = 0;
    //美妆
    private readonly int TYPE_MT_MAKEUP = 1;

    private byte[] m_bytes;
    private Action<byte[], string> m_makeupCallback = null;
    private string m_strMakeupId = String.Empty;
    private int m_iMakeupAlpha = 0;
    private int m_iBeautyAlpha = 0;
    private int m_iDermabrasionLv = 0;

    public void GetMTMakeupPicture(byte[] bytes, Action<byte[], string> callback, string makeupId, int makeupAlpha, int beautyAlpha, int dermaLv)
    {
        if (null == bytes || bytes.Length < 1)
        {
            Debug.LogError("bytes is null or bytes.length < 1!!");
            callback(null, "美图美妆：图片数组为空或长度小于1");
            return;
        }

        m_bytes = bytes;
        m_makeupCallback = callback;
        m_strMakeupId = makeupId;
        m_iMakeupAlpha = makeupAlpha;
        m_iBeautyAlpha = beautyAlpha;
        m_iDermabrasionLv = dermaLv;

        StartCoroutine(MTShowShow(TYPE_MT_MAKEUP));
    }

    public void GetMTHDPicture(byte[] bytes, Action<byte[], string> callback)
    {
        if (null == bytes || bytes.Length < 1)
        {
            Debug.Log("bytes is null or bytes.length < 1!!");
            callback(null, "美图高清人像：图片数组为空或长度小于1");
            return;
        }

        m_bytes = bytes;
        m_makeupCallback = callback;

        StartCoroutine(MTShowShow(TYPE_MT_HDPICTURE));
    }

    private float m_fPreUp = 0f;
    private IEnumerator MTShowShow(int type)
    {
        string img64 = Convert.ToBase64String(m_bytes);
        string json = string.Empty;
        if (TYPE_MT_MAKEUP == type)
        {
            json = JsonMapper.ToJson(new MyMakeupJsonData(img64, m_strMakeupId, m_iMakeupAlpha, m_iBeautyAlpha, 0, m_iDermabrasionLv));
        }
        else
        {
            json = JsonMapper.ToJson(new MyAIBeauty(img64));
        }
        //Debug.Log("json = " + json);
        UploadHandlerRaw uhr = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json))
        {
            contentType = "application/json"
        };
        string url = string.Format(TYPE_MT_MAKEUP == type ? MTShowShowConfig.MT_MAKEUP_URL : MTShowShowConfig.MT_AIBEAUTY_URL, MTShowShowConfig.MT_FACE_APP_KEY, MTShowShowConfig.MT_FACE_SECRET_ID);
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
            }

            yield return 1;
        }

        if (uwr.isHttpError || uwr.isNetworkError)
        {
            Debug.LogErrorFormat("Request MTShowShow API Failed! url = {0}, {1} = {2}",
                url, uwr.isHttpError ? "HttpError" : "NetworkError", uwr.error);
            Debug.LogErrorFormat("uwr.downloadHandler.text = {0}", uwr.downloadHandler.text);
            if (null != m_makeupCallback)
            {
                string error = string.Format("{0}请求错误：{1}", type == 0 ? "美妆" : "高清人像", uwr.downloadHandler.text);
                m_makeupCallback(null, error);
            }
        }
        else
        {
            try
            {
                //Debug.LogFormat("rootJson = {0}", uwr.downloadHandler.text);
                JsonData rootData = JsonMapper.ToObject(uwr.downloadHandler.text);
                JsonData mediaInfoData = rootData["media_info_list"];

                JsonData mediaData = mediaInfoData[0]["media_data"];
                string imgB64 = mediaData.ToString();
                byte[] bytes = Convert.FromBase64String(imgB64);
                if (null != m_makeupCallback)
                {
                    m_makeupCallback(bytes, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (null != m_makeupCallback)
                {
                    m_makeupCallback(null, "异常：" + ex);
                }
            }
        }
    }
}
