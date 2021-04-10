using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ARFace;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(FaceConfig))]
[CanEditMultipleObjects]
public class FaceConfigEditor : Editor
{


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();
        var c = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;


        if (GUILayout.Button("Build LiuHong Top"))
        {
            FaceConfig t = target as FaceConfig;
            //var UVRemapLut = MeshUtils.BuildUvRemapLut(t.remapMesh.uv2, t.remapMesh.uv, t.remapMesh.triangles.ToList(), 2048);
            //var UVRemapLutT2D = UVRemapLut.ToNewTexture2D(TextureFormat.RGBAFloat);
            //t.liuhongTop = t.remapMesh.uv2.Select(v =>
            //{
            //    return MeshUtils.ValidUV(UVRemapLutT2D, v, null);
            //}).ToArray();

            //DestroyImmediate(UVRemapLut);
            //DestroyImmediate(UVRemapLutT2D);
            t.liuhongTop = t.remapMesh.uv;
            EditorUtility.SetDirty(t);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Build arkit->design Matrix4x4"))
        {
            FaceConfig t = target as FaceConfig;
            var designHead = t.allHead.Find("head");
            var arkitHead = t.allHead.Find("renlian_uvremap");
            Debug.Assert(designHead, "find child head not find");
            Debug.Assert(arkitHead, "find child renlian_uvremap not find");
            t.faceToHead = designHead.worldToLocalMatrix * arkitHead.localToWorldMatrix;
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        GUI.backgroundColor = c;

        serializedObject.ApplyModifiedProperties();
    }


}
