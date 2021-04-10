using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CaptureData 
{
    public Mesh Face
    {
        get
        {
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = faceTextureUVs;
            mesh.uv2 = faceARkitUVs;
            mesh.triangles = triangles;
            return mesh;
        }
    }
    public RenderTexture faceTexture;
    public RenderTexture faceARKitTexture;
    public Vector2[] faceTextureUVs;
    public Vector2[] faceARkitUVs;
    public int[] triangles;
    public Vector3[] vertices;
    public float faceAngle;
    public List<Vector2> faceLandmarkList;
}
