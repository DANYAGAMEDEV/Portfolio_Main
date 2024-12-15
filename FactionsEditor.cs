using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FactionsDisplay))]
public class FactionsEditor : Editor
{
    private FactionRelationsData savedData;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Faction Relations Matrix", EditorStyles.boldLabel);

        if (FactionsManager.factions.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(100));

            foreach (var faction in FactionsManager.factions)
            {
                EditorGUILayout.LabelField(faction.ToString(), GUILayout.Width(50));
            }
            EditorGUILayout.EndHorizontal();

            foreach (var faction1 in FactionsManager.factions)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(faction1.ToString(), GUILayout.Width(100));

                foreach (var faction2 in FactionsManager.factions)
                {
                    int currentRelation = FactionsManager.GetRelation(faction1, faction2);
                    int newRelation = EditorGUILayout.IntField(currentRelation, GUILayout.Width(50));

                    if (newRelation != currentRelation)
                    {
                        FactionsManager.SetRelation(faction1, faction2, newRelation);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Save Relations to ScriptableObject", EditorStyles.boldLabel);

        FactionsDisplay display = (FactionsDisplay)target;
        savedData = display.relationsData;

        if (GUILayout.Button("Save to ScriptableObject") && savedData != null)
        {
            SaveToScriptableObject();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void SaveToScriptableObject()
    {
        savedData.factions.Clear();
        savedData.relations.Clear();

        savedData.factions.AddRange(FactionsManager.factions);

        foreach (var _factionHorizontal in FactionsManager.factions)
        {
            foreach (var _factionVertical in FactionsManager.factions)
            {
                var relation = new FactionRelationsData.FactionRelation
                {
                    factionHorizontal = _factionHorizontal,
                    factionVertical = _factionVertical,
                    relation = FactionsManager.GetRelation(_factionHorizontal, _factionVertical)
                };
                savedData.relations.Add(relation);
            }
        }
        EditorUtility.SetDirty(savedData);

        Debug.Log("Faction relations saved to ScriptableObject.");
    }
}


