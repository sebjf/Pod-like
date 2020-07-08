using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(Catalogue))]
public class CatalogueEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Update"))
        {
            // Undo.RecordObject(target, "Find Assets for Catalogue"); // https://answers.unity.com/questions/1355904/undorecordobject-doest-work.html

            var component = target as Catalogue;

            foreach(var guid in AssetDatabase.FindAssets("t:car"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Car>(path);
                if(!component.cars.Contains(asset))
                {
                    component.cars.Add(asset);
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:circuit"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Circuit>(path);
                if (!component.circuits.Contains(asset))
                {
                    component.circuits.Add(asset);
                }
            }

            EditorUtility.SetDirty(component);
        }
    }
}
