using System.Collections.Generic;

public static class MTShowShowConfig
{
    public const string MT_FACE_APP_ID = "1247";
    public const string MT_FACE_APP_KEY = "H_u3FVNbnCj2FIgCL3EFLt7S0MJydjgO";
    public const string MT_FACE_SECRET_ID = "Er5m0LnchRLp5DK19p6Jgf7duG6Hb0Y2";

    public const string MT_ARMAKEUP_URL = "https://openapi.mtlab.meitu.com/v1/ar?api_key={0}&api_secret={1}";
    public const string MT_MAKEUP_URL = "https://openapi.mtlab.meitu.com/v3/makeup?api_key={0}&api_secret={1}";
    public const string MT_AIBEAUTY_URL = "https://openapi.mtlab.meitu.com/v2/AIBeauty?api_key={0}&api_secret={1}";
    public const string MT_LANDMARK_URL = "https://openapi.mtlab.meitu.com/v1/facedetect?api_key={0}&api_secret={1}";

    //整体妆容ID
    public const string MT_MAKEUP_0 = "Db0008YLJvi5tvdo";
    public const string MT_MAKEUP_1 = "Fb0020A7UaiQjarZ";
    public const string MT_MAKEUP_2 = "Fb00217NLnZskl90";
    public const string MT_MAKEUP_3 = "Fb0022HPFo1DTzqV";
    public const string MT_MAKEUP_4 = "Fb0028shm9ZtHdQ4";
    public const string MT_MAKEUP_5 = "Fb0038w2voWBKnGi";
}

public class MyArMakeupJsonData
{
    public class cl_parameter
    {
        public string makeup_id;
        public int makeup_alpha;
        public int beauty_alpha;
        public int hair_mask;
    }

    public class cl_extra { }

    public class cl_listItem
    {
        public string media_data;
        public cl_extra media_extra = new cl_extra();
        public cl_media_profiles media_profiles = new cl_media_profiles();

        public class cl_media_profiles
        {
            public string media_data_type = "jpg";
        }
    }

    public List<cl_listItem> media_info_list;
    public cl_extra extra;
    public cl_parameter parameter;

    public MyArMakeupJsonData(string mediaData, string makeupId, int makeupAlpha, int beautyAlpha, int hairMask, int beautyLv)
    {
        parameter = new cl_parameter
        {
            makeup_id = makeupId,
            makeup_alpha = makeupAlpha,
            beauty_alpha = beautyAlpha,
            hair_mask = hairMask,
        };

        extra = new cl_extra();

        media_info_list = new List<cl_listItem>
        {
            new cl_listItem
            {
                media_data = mediaData
            }
        };
    }
}

public class MyMakeupJsonData
{
    public class cl_parameter
    {
        public string makeupId;
        public int makeupAlpha;
        public int beautyAlpha;
        public string rsp_media_type;
        public int hairMask;
        public int beautyLevel;
    }

    public class cl_extra { }

    public class cl_listItem
    {
        public string media_data;
        public cl_media_profiles media_profiles = new cl_media_profiles();

        public class cl_media_profiles
        {
            public string media_data_type = "jpg";
        }
    }

    public List<cl_listItem> media_info_list;
    public cl_extra extra;
    public cl_parameter parameter;

    public MyMakeupJsonData(string mediaData, string makeupId, int makeupAlpha, int beautyAlpha, int hairMask, int beautyLv)
    {
        parameter = new cl_parameter
        {
            makeupId = makeupId,
            makeupAlpha = makeupAlpha,
            beautyAlpha = beautyAlpha,
            rsp_media_type = "base64",
            hairMask = hairMask,
            beautyLevel = beautyLv
        };

        extra = new cl_extra();

        media_info_list = new List<cl_listItem>
        {
            new cl_listItem
            {
                media_data = mediaData
            }
        };
    }
}

public class MyAIBeauty
{
    public class aib_parameter
    {
        public int outputType = 0;
        public string rsp_media_type = "base64";
        public string version = "1.0";
    }

    public class aib_extra { }

    public class aib_listItem
    {
        public class aib_media_extra { }
        public class aib_media_profiles { }

        public string media_data;
        public aib_media_extra media_extra = new aib_media_extra();
        public aib_media_profiles media_profiles = new aib_media_profiles();
    }

    public aib_parameter parameter = new aib_parameter();
    public aib_extra extra = new aib_extra();
    public List<aib_listItem> media_info_list;

    public MyAIBeauty(string mediaData)
    {
        media_info_list = new List<aib_listItem>
        {
            new aib_listItem
            {
                media_data = mediaData
            }
        };
    }
}

public class MyLandmark
{
    public class lm_parameter
    {
        public int return_landmark = 0;
    }

    public class lm_extra { }

    public class lm_listItem
    {
        public class lm_media_extra { }
        public class lm_media_profiles
        {
            public string media_data_type = "jpg";
        }

        public string media_data;
        public lm_media_extra media_extra = new lm_media_extra();
        public lm_media_profiles media_profiles = new lm_media_profiles();
    }

    public lm_parameter parameter;
    public lm_extra extra = new lm_extra();
    public List<lm_listItem> media_info_list;

    public MyLandmark(string mediaData, int landmarkType)
    {
        parameter = new lm_parameter
        {
            return_landmark = landmarkType
        };
        media_info_list = new List<lm_listItem>
        {
            new lm_listItem
            {
                media_data = mediaData
            }
        };
    }
}
