using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainWrapper))]
public class TerrainWrapperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        TerrainWrapper tw = (TerrainWrapper)target;

        if (GUILayout.Button("Generate All"))
            tw.GenerateAll();
    }
}