using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.XR.iOS.Utils;


namespace UnityEngine.XR.iOS
{
	#if UNITY_EDITOR || !UNITY_IOS
	public class ARFaceGeometry
	{
		private serializableFaceGeometry sFaceGeometry;

		public ARFaceGeometry (serializableFaceGeometry ufg)
		{
			sFaceGeometry = ufg;
		}

		public int vertexCount { get { return sFaceGeometry.Vertices.Length; } }
		public int triangleCount {  get  { return sFaceGeometry.TriangleIndices.Length; } }
		public int textureCoordinateCount { get { return sFaceGeometry.TexCoords.Length; } }

		public Vector3 [] vertices { get { return sFaceGeometry.Vertices; } }

		public Vector2 [] textureCoordinates { get { return sFaceGeometry.TexCoords; } }

		public int [] triangleIndices { get { return sFaceGeometry.TriangleIndices; } }

	}

	public class ARFaceAnchor 
	{
		serializableUnityARFaceAnchor m_sfa;

		public ARFaceAnchor(serializableUnityARFaceAnchor sfa)
		{
			m_sfa = sfa;
		}

		public string identifierStr { get { return  Encoding.UTF8.GetString (m_sfa.identifierStr); } }

		public Matrix4x4 transform { get { return m_sfa.worldTransform; } }

		public ARFaceGeometry faceGeometry { get { return new ARFaceGeometry (m_sfa.faceGeometry);	} }

		public Dictionary<string, float> blendShapes { get { return m_sfa.arBlendShapes; } }

		public Pose leftEyePose
		{
			get
			{
				if (m_sfa.leftEyeTransform == null)
					return new Pose(Vector3.zero, Quaternion.identity);
				
				var ret = UnityARMatrixOps.GetPose((UnityARMatrix4x4) m_sfa.leftEyeTransform);
				ret.position.z *= -1;
				return ret;
			}
		}

		public UnityARMatrix4x4 leftEyeTransform
		{
			get { return m_sfa.leftEyeTransform == null ? 
				UnityARMatrixOps.GetMatrix(Matrix4x4.identity) :
				(UnityARMatrix4x4) m_sfa.leftEyeTransform; }
		}
		public UnityARMatrix4x4 rightEyeTransform
		{
			get { return m_sfa.rightEyeTransform == null ? 
				UnityARMatrixOps.GetMatrix(Matrix4x4.identity) :
				(UnityARMatrix4x4) m_sfa.rightEyeTransform; }
		}

		public Pose rightEyePose
		{
			get
			{
				if (m_sfa.rightEyeTransform == null)
					return new Pose(Vector3.zero, Quaternion.identity);
				
				var ret = UnityARMatrixOps.GetPose((UnityARMatrix4x4) m_sfa.rightEyeTransform);
				ret.position.z *= -1;
				return ret;
			}
		}

		public Vector3 lookAtPoint
		{
			get
			{
				return Vector3.zero;
			}
		}

		public bool isTracked { get { return m_sfa.isTracked; } }


	}
	#endif
}
