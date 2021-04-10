using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.XR.iOS
{

    public class UnityARVideo : MonoBehaviour
    {
        public Material m_ClearMaterial;

        private CommandBuffer m_VideoCommandBuffer;
        private Texture2D _videoTextureY;
        private Texture2D _videoTextureCbCr;
        [HideInInspector]
		public Matrix4x4 displayTransform;

        //private Matrix4x4 originDisplayTransform;


        private bool bCommandBufferInitialized;

		public void Start()
		{
			if (UnityARSessionNativeInterface.IsARKitRemoteServer())
			{
				UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateFrame;
			}
			bCommandBufferInitialized = false;
		}

		public void UpdateFrame(UnityARCamera cam)
		{
            //originDisplayTransform = new Matrix4x4();
            //originDisplayTransform.SetColumn(0, cam.displayTransform.column0);
            //originDisplayTransform.SetColumn(1, cam.displayTransform.column1);
            //originDisplayTransform.SetColumn(2, cam.displayTransform.column2);
            //originDisplayTransform.SetColumn(3, cam.displayTransform.column3);

            //temp fix: crop by affine transform
            // todo: ipad not support
            //displayTransform.m00 = Mathf.Round(cam.displayTransform.column0[0]);
            //displayTransform.m01 = Mathf.Round(cam.displayTransform.column1[0]);
            //displayTransform.m10 = Mathf.Round(cam.displayTransform.column0[1]);
            //displayTransform.m11 = Mathf.Round(cam.displayTransform.column1[1]);
            //displayTransform.m22 = 1;
            //displayTransform.m33 = 1;

            displayTransform = new Matrix4x4();
            displayTransform.SetColumn(0, cam.displayTransform.column0);
            displayTransform.SetColumn(1, cam.displayTransform.column1);
            displayTransform.SetColumn(2, cam.displayTransform.column2);
            displayTransform.SetColumn(3, cam.displayTransform.column3);

            this.videoParams = cam.videoParams;
        }

        UnityVideoParams? videoParams;

		void InitializeCommandBuffer()
		{
			m_VideoCommandBuffer = new CommandBuffer(); 
			m_VideoCommandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, m_ClearMaterial);
			GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
			bCommandBufferInitialized = true;

		}

		void OnDestroy()
		{
			if (m_VideoCommandBuffer != null) {
				GetComponent<Camera>().RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, m_VideoCommandBuffer);
			}

			if (UnityARSessionNativeInterface.IsARKitRemoteServer())
			{
				UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateFrame;
			}
			bCommandBufferInitialized = false;

            if (_videoTextureY) Destroy(_videoTextureY);
            if (_videoTextureCbCr) Destroy(_videoTextureCbCr);
        }

#if !UNITY_EDITOR && UNITY_IOS

        public void OnPreRender()
        {
			ARTextureHandles handles = UnityARSessionNativeInterface.GetARSessionNativeInterface().GetARVideoTextureHandles();
            if (handles.IsNull())
            {
                return;
            }

            if (!bCommandBufferInitialized) {
                InitializeCommandBuffer ();
            }

            if (!videoParams.HasValue)
            {
                return;
            }
            Vector2Int size = new Vector2Int(videoParams.Value.yWidth, videoParams.Value.yHeight);

            // Texture Y
            if (_videoTextureY != null && new Vector2Int(_videoTextureY.width, _videoTextureY.height) != size)
            {
                Destroy(_videoTextureY);
            }
            if (_videoTextureY == null) {
              _videoTextureY = Texture2D.CreateExternalTexture(size.x, size.y,
                  TextureFormat.R8, false, false, (System.IntPtr)handles.TextureY);
              _videoTextureY.filterMode = FilterMode.Bilinear;
              _videoTextureY.wrapMode = TextureWrapMode.Repeat;
              m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
            }

            // Texture CbCr
            if (_videoTextureCbCr != null && new Vector2Int(_videoTextureCbCr.width, _videoTextureCbCr.height) != size)
            {
                Destroy(_videoTextureCbCr);
            }
            if (_videoTextureCbCr == null) {
              _videoTextureCbCr = Texture2D.CreateExternalTexture(size.x, size.y,
                  TextureFormat.RG16, false, false, (System.IntPtr)handles.TextureCbCr);
              _videoTextureCbCr.filterMode = FilterMode.Bilinear;
              _videoTextureCbCr.wrapMode = TextureWrapMode.Repeat;
              m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);
            }

            _videoTextureY.UpdateExternalTexture(handles.TextureY);
            _videoTextureCbCr.UpdateExternalTexture(handles.TextureCbCr);

			m_ClearMaterial.SetMatrix("_DisplayTransform", displayTransform);
        }
#else

        public void SetYTexure(Texture2D YTex)
		{
			_videoTextureY = YTex;
		}

		public void SetUVTexure(Texture2D UVTex)
		{
			_videoTextureCbCr = UVTex;
		}

		public void OnPreRender()
		{

			if (!bCommandBufferInitialized) {
				InitializeCommandBuffer ();
			}

			m_ClearMaterial.SetTexture("_textureY", _videoTextureY);
			m_ClearMaterial.SetTexture("_textureCbCr", _videoTextureCbCr);

			m_ClearMaterial.SetMatrix("_DisplayTransform", displayTransform);
		}
 
#endif

	    /// <summary>
	    /// 未准备好时返回null
	    /// </summary>
	    /// <returns></returns>
	    public Material GetScreenMaterial()
	    {
		    OnPreRender();
		    if (!_videoTextureY || !_videoTextureCbCr)
			    return null;
		    
		    return m_ClearMaterial;
	    }

	    public static UnityARVideo Instance;

	    private void Awake()
	    {
		    Instance = this;
	    }
    }
}
