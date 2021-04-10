using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.iOS.Utils;

namespace UnityEngine.XR.iOS
{

	public class UnityRemoteVideo : MonoBehaviour
	{
		public ConnectToEditor connectToEditor;

		private UnityARSessionNativeInterface m_Session;
		private bool bTexturesInitialized;

		private int currentFrameIndex;
		private byte[] m_textureYBytes;
		private byte[] m_textureUVBytes;
		private byte[] m_textureYBytes2;
		private byte[] m_textureUVBytes2;
		private GCHandle m_pinnedYArray;
		private GCHandle m_pinnedUVArray;

#if !UNITY_EDITOR && UNITY_IOS

		public void Start()
		{
			m_Session = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += UpdateCamera;
			currentFrameIndex = 0;
			bTexturesInitialized = false;
		}

		void UpdateCamera(UnityARCamera camera)
		{
			if (!bTexturesInitialized) {
				InitializeTextures (camera);
			}
			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= UpdateCamera;

		}

		void InitializeTextures(UnityARCamera camera)
		{
			int numYBytes = camera.videoParams.yWidth * camera.videoParams.yHeight;
			int numUVBytes = camera.videoParams.yWidth * camera.videoParams.yHeight / 2; //quarter resolution, but two bytes per pixel
			
			m_textureYBytes = new byte[numYBytes];
			m_textureUVBytes = new byte[numUVBytes];
			m_textureYBytes2 = new byte[numYBytes];
			m_textureUVBytes2 = new byte[numUVBytes];
			m_pinnedYArray = GCHandle.Alloc (m_textureYBytes);
			m_pinnedUVArray = GCHandle.Alloc (m_textureUVBytes);
			bTexturesInitialized = true;
		}

		IntPtr PinByteArray(ref GCHandle handle, byte[] array)
		{
			handle.Free ();
			handle = GCHandle.Alloc (array, GCHandleType.Pinned);
			return handle.AddrOfPinnedObject ();
		}

		byte [] ByteArrayForFrame(int frame,  byte[] array0,  byte[] array1)
		{
			return frame == 1 ? array1 : array0;
		}

		byte [] YByteArrayForFrame(int frame)
		{
			return ByteArrayForFrame (frame, m_textureYBytes, m_textureYBytes2);
		}

		byte [] UVByteArrayForFrame(int frame)
		{
			return ByteArrayForFrame (frame, m_textureUVBytes, m_textureUVBytes2);
		}

		void OnDestroy()
		{
			m_Session.SetCapturePixelData (false, IntPtr.Zero, IntPtr.Zero);

			m_pinnedYArray.Free ();
			m_pinnedUVArray.Free ();

		}

		public void OnPreRender()
		{
			ARTextureHandles handles = m_Session.GetARVideoTextureHandles();
            if (handles.IsNull())
			{
				return;
			}

			if (!bTexturesInitialized)
				return;
			
			currentFrameIndex = (currentFrameIndex + 1) % 2;

			Resolution currentResolution = Screen.currentResolution;


			m_Session.SetCapturePixelData (true, PinByteArray(ref m_pinnedYArray,YByteArrayForFrame(currentFrameIndex)), PinByteArray(ref m_pinnedUVArray,UVByteArrayForFrame(currentFrameIndex)));

			connectToEditor.SendToEditor (ConnectionMessageIds.screenCaptureYMsgId, 
					CompressionHelper.ByteArrayCompress(YByteArrayForFrame(1-currentFrameIndex)));
			connectToEditor.SendToEditor (ConnectionMessageIds.screenCaptureUVMsgId, 
					CompressionHelper.ByteArrayCompress(UVByteArrayForFrame(1-currentFrameIndex)));
			
#if ARKIT_REMOTE_LANDMARK
			// landmark
			Assembly ARFace = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "ARFace");
			Type LandmarkManager = ARFace.GetType("ARFace.Landmarks.LandmarkManager");
			FieldInfo ActiveLandmarkType =
				LandmarkManager.GetField("ActiveLandmarkType", BindingFlags.Static | BindingFlags.Public);
			MethodInfo GetLandmark =
				LandmarkManager.GetMethod("GetLandmark", BindingFlags.Static | BindingFlags.Public);
			System.Diagnostics.Debug.Assert(GetLandmark != null, nameof(GetLandmark) + " != null");

			var material = GetComponent<UnityARVideo>().m_ClearMaterial;
			var matrix = GetComponent<UnityARVideo>().displayTransform;
			var textureY = material.GetTexture("_textureY");
			if(!textureY)
			{
				Debug.Log("textureY == null");
				return;
			}
			var p = matrix.MultiplyPoint(new Vector2(textureY.width, textureY.height));
			var size = new Vector2Int((int) Mathf.Abs(p.x), (int) Mathf.Abs(p.y));
			RenderTexture rt = RenderTexture.GetTemporary(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			Graphics.Blit(null, rt, material);

			var landmarkType = ActiveLandmarkType.GetValue(null);
			GetLandmark.Invoke(null, new[]
			{
				rt,
				landmarkType,
				(Action<Dictionary<string, Vector2>>) (landmarks =>
				{
					RenderTexture.ReleaseTemporary(rt);
					if(landmarks == null)
					{
						Debug.Log("no landmark.");
						return;
					}
					var data = landmarks.ToDictionary(
						pair => pair.Key,
						pair => (serializableVector2) pair.Value);
					connectToEditor.SendToEditor(ConnectionMessageIds.screenCaptureLandmarksId, data);
				}),
				null
			});
#endif
		}
#endif
    }
}
