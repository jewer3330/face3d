using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ARFace.Landmarks;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

public class TencentCloudLandmark : LandmarkBase
{

    private void Awake()
    {
        type = LandmarkType.Tencent;
    }

    private Dictionary<string, List<Vector2>> m_dicFacePos = new Dictionary<string, List<Vector2>>();
    private List<Vector2> m_listFaceProfile = new List<Vector2>();
    private List<Vector2> m_listLeftEye = new List<Vector2>();
    private List<Vector2> m_listRightEye = new List<Vector2>();
    private List<Vector2> m_listLeftEyeBrow = new List<Vector2>();
    private List<Vector2> m_listRightEyeBrow = new List<Vector2>();
    private List<Vector2> m_listMouth = new List<Vector2>();
    private List<Vector2> m_listNose = new List<Vector2>();
    private List<Vector2> m_listLeftPupil = new List<Vector2>();
    private List<Vector2> m_listRightPupil = new List<Vector2>();

    private List<Vector2> m_listArray = new List<Vector2>();

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

    private IEnumerator GetLandmark()
    {
        string service = "iai";
        string host = "iai.tencentcloudapi.com";
        string region = "ap-shanghai";
        string action = "AnalyzeFace";
        string version = "2018-03-01";
        string algorithm = "TC3-HMAC-SHA256";
        DateTime utcNow = DateTime.UtcNow;
        string date = utcNow.ToString("yyyy-MM-dd");

        Dictionary<string, string> dicParams = new Dictionary<string, string>
        {
            { "Image", Convert.ToBase64String(m_bytes) }
        };
        string jsonData = JsonMapper.ToJson(dicParams);

        // ************* 步骤 1：拼接规范请求串 *************
        string httpRequestMethod = "POST";
        string canonicalUri = "/";
        string canonicalQueryString = "";
        string canonicalHeaders = "content-type:application/json\n" + "host:" + host + "\n";
        string signedHeaders = "content-type;host";

        string payload = jsonData;
        string hashedRequestPayload = Sha256Hex(payload);
        string canonicalRequest = httpRequestMethod + "\n" + canonicalUri + "\n" + canonicalQueryString + "\n"
                + canonicalHeaders + "\n" + signedHeaders + "\n" + hashedRequestPayload;
        Debug.LogFormat("canonicalRequest = {0}", canonicalRequest);

        // ************* 步骤 2：拼接待签名字符串 *************
        string credentialScope = date + "/" + service + "/" + "tc3_request";
        string hashedCanonicalRequest = Sha256Hex(canonicalRequest);
        string timeStamp = GetTimeStamp(utcNow);
        string stringToSign = algorithm + "\n" + timeStamp + "\n" + credentialScope + "\n" + hashedCanonicalRequest;
        Debug.LogFormat("stringToSign = {0}", stringToSign);

        //// ************* 步骤 3：计算签名 *************
        byte[] secretDate = Hmc256(Encoding.UTF8.GetBytes("TC3" + TencentCloudConfig.TC_FACE_SECRET_KEY), date);
        byte[] secretService = Hmc256(secretDate, service);
        byte[] secretSigning = Hmc256(secretService, "tc3_request");
        string signature = PrintHexBinary(Hmc256(secretSigning, stringToSign));

        // ************* 步骤 4：拼接 Authorization *************
        string authorization = algorithm + " " + "Credential=" + TencentCloudConfig.TC_FACE_SECRET_ID + "/" + credentialScope + ", "
                + "SignedHeaders=" + signedHeaders + ", " + "Signature=" + signature;
        Debug.LogFormat("authorization = {0}", authorization);

        UploadHandlerRaw uhr = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData))
        {
            contentType = "application/json"
        };
        UnityWebRequest uwr = UnityWebRequest.Post(TencentCloudConfig.TC_ANALYZEFACE_URL, jsonData);
        uwr.uploadHandler = uhr;
        uwr.SetRequestHeader("Authorization", authorization);
        uwr.SetRequestHeader("X-TC-Action", action);
        uwr.SetRequestHeader("X-TC-Version", version);
        uwr.SetRequestHeader("X-TC-Timestamp", timeStamp);
        uwr.SetRequestHeader("X-TC-Region", region);
        uwr.timeout = 30;

        uwr.SendWebRequest();

        while (!uwr.isDone)
        {
            float fProgress = Mathf.Floor(uwr.uploadProgress * 99);
            Debug.LogFormat("upload progress {0}%", fProgress);
            if (null != m_uploadProgressCallback)
            {
                m_uploadProgressCallback(fProgress);
            }
            yield return 1;
        }

        if (uwr.isDone)
        {
            if (null != m_uploadProgressCallback)
            {
                m_uploadProgressCallback(100f);
            }
        }

        if (uwr.isHttpError || uwr.isNetworkError)
        {
            Debug.LogErrorFormat("Request TencentCloud AnalyzeFace API Failed! url = {0}, www.error = {1}", TencentCloudConfig.TC_ANALYZEFACE_URL, uwr.error);
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
                JsonData responseData = rootData["Response"];
                string requestId = responseData["RequestId"].ToString();
                Debug.LogFormat("RequestId = {0}", requestId);
                int imageWidth = Convert.ToInt32(responseData["ImageWidth"].ToString());
                int imageHeight = Convert.ToInt32(responseData["ImageHeight"].ToString());
                Debug.LogFormat("ImageSize = ({0},{1})", imageWidth, imageHeight);
                JsonData facesData = responseData["FaceShapeSet"];
                if (facesData.Count < 1)
                {
                    Debug.LogError("[FaceShapeSet]->faces count < 1!!!");
                    if (m_uvPosCallback != null)
                    {
                        m_dict.Clear();
                        m_uvPosCallback(m_dict);
                    }
                }
                else
                {
                    JsonData face0 = facesData[0];
                    m_dicFacePos.Clear();
                    m_dicFacePos.Add(TencentCloudConfig.TC_FACEPROFILE, ParseFaceInfo(face0[TencentCloudConfig.TC_FACEPROFILE], m_listFaceProfile));
                    m_dicFacePos.Add(TencentCloudConfig.TC_LEFTEYE, ParseFaceInfo(face0[TencentCloudConfig.TC_LEFTEYE], m_listLeftEye));
                    m_dicFacePos.Add(TencentCloudConfig.TC_RIGHTEYE, ParseFaceInfo(face0[TencentCloudConfig.TC_RIGHTEYE], m_listRightEye));
                    m_dicFacePos.Add(TencentCloudConfig.TC_LEFTEYEBROW, ParseFaceInfo(face0[TencentCloudConfig.TC_LEFTEYEBROW], m_listLeftEyeBrow));
                    m_dicFacePos.Add(TencentCloudConfig.TC_RIGHTEYEBROW, ParseFaceInfo(face0[TencentCloudConfig.TC_RIGHTEYEBROW], m_listRightEyeBrow));
                    m_dicFacePos.Add(TencentCloudConfig.TC_MOUTH, ParseFaceInfo(face0[TencentCloudConfig.TC_MOUTH], m_listMouth));
                    m_dicFacePos.Add(TencentCloudConfig.TC_NOSE, ParseFaceInfo(face0[TencentCloudConfig.TC_NOSE], m_listNose));
                    m_dicFacePos.Add(TencentCloudConfig.TC_LEFTPUPIL, ParseFaceInfo(face0[TencentCloudConfig.TC_LEFTPUPIL], m_listLeftPupil));
                    m_dicFacePos.Add(TencentCloudConfig.TC_RIGHTPUPIL, ParseFaceInfo(face0[TencentCloudConfig.TC_RIGHTPUPIL], m_listRightPupil));

                    m_listArray.Clear();
                    foreach (var item in m_dicFacePos)
                    {
                        m_listArray.AddRange(m_dicFacePos[item.Key]);
                    }

                    m_dict.Clear();
                    for (int i = 0; i < m_listArray.Count; i++)
                    {
                        m_dict.Add(i.ToString(), m_listArray[i]);
                    }
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

    private static string GetTimeStamp(DateTime utcNow)
    {
        TimeSpan ts = utcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds).ToString();
    }

    private List<Vector2> ParseFaceInfo(JsonData jd, List<Vector2> list)
    {
        list.Clear();
        if (jd.Count < 1)
        {
            Debug.LogErrorFormat("[ParseFaceInfo] count < 1!!!");
            return list;
        }
        for (int i = 0; i < jd.Count; i++)
        {
            Vector2 vec = new Vector2(Convert.ToSingle(jd[i]["X"].ToString()), m_photoHeight - Convert.ToSingle(jd[i]["Y"].ToString()));
            list.Add(vec);
        }
        return list;
    }

    public static byte[] Hmc256(byte[] key, string msg)
    {
        HMACSHA256 myHMACSHA256 = new HMACSHA256(key);
        byte[] bytes = Encoding.UTF8.GetBytes(msg);
        byte[] retBytes = myHMACSHA256.ComputeHash(bytes);

        return retBytes;
    }

    public static string Sha256Hex(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        return Sha256Hex(bytes);
    }

    public static string Sha256Hex(byte[] bytes)
    {
        byte[] hash = SHA256.Create().ComputeHash(bytes);
        //byte[] hash = HashAlgorithm.Create("SHA256").ComputeHash(bytes);
        return PrintHexBinary(hash);
    }

    private static string PrintHexBinary(byte[] hash)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }


    ////test
    //[SerializeField]
    //private Texture m_tex = null;
    //[SerializeField]
    //private GameObject m_goPhoto = null;
    //[SerializeField]
    //private GameObject m_goPoint = null;

    //private byte[] m_testBytes = null;


    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        m_testBytes = m_tex.TextureEncodeToPNG();
    //        GetPhotoUVPosition(m_testBytes, m_tex.width, m_tex.height, GetDict);
    //    }
    //}

    //private void GetDict(Dictionary<string, Vector2> dic)
    //{
    //    Debug.Log("OK...");
    //    int i = 0;
    //    var vertices1 = WhateverFaceMesh.GetVectors(WhateverFaceMesh.tencent_eye_left, dic, m_tex.width, m_tex.height);
    //    var vertices2 = WhateverFaceMesh.GetVectors(WhateverFaceMesh.tencent_eye_right, dic, m_tex.width, m_tex.height);
    //    var predict1 = WhateverFaceMesh.PredictEyeCircle(vertices1, 0.003f);
    //    var predict2 = WhateverFaceMesh.PredictEyeCircle(vertices1, 0.006f);
    //    var predict3 = WhateverFaceMesh.PredictEyeCircle(vertices1, 0.01f);

    //    var predict4 = WhateverFaceMesh.PredictEyeCircle(vertices2, 0.003f);
    //    var predict5 = WhateverFaceMesh.PredictEyeCircle(vertices2, 0.006f);
    //    var predict6 = WhateverFaceMesh.PredictEyeCircle(vertices2, 0.01f);
    //    PrintPoints(predict1, Color.white);
    //    PrintPoints(predict2, Color.yellow);
    //    PrintPoints(predict3, Color.green);
    //    PrintPoints(predict4, Color.white);
    //    PrintPoints(predict5, Color.yellow);
    //    PrintPoints(predict6, Color.green);

    //    //PrintPoints(vertices1, Color.red);
    //    //PrintPoints(vertices2, Color.red);


    //    //foreach (var item in dic)
    //    //{
    //    //    GameObject goItem = Instantiate(m_goPoint, m_goPhoto.transform);
    //    //    goItem.SetActive(true);
    //    //    goItem.name = i.ToString();
    //    //    RectTransform rt = goItem.transform.GetComponent<RectTransform>();
    //    //    rt.anchoredPosition = item.Value;

    //    //    i++;
    //    //}
    //}

    //void PrintPoints(IList<Vector3> vectors, Color color)
    //{
    //    var i = 0;
    //    foreach (var item in vectors)
    //    {
    //        GameObject goItem = Instantiate(m_goPoint, m_goPhoto.transform);
    //        goItem.SetActive(true);
    //        goItem.name = i.ToString();
    //        RectTransform rt = goItem.transform.GetComponent<RectTransform>();
    //        goItem.GetComponent<UnityEngine.UI.Image>().color = color;
    //        rt.anchoredPosition = new Vector2(item.x * m_tex.width, item.y * m_tex.height);
    //        i++;
    //    }
    //}
}
