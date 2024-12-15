using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip audioClip;
}
[CreateAssetMenu(fileName = "SoundContainer", menuName = "Sound/SoundContainer")]
public class SoundContainer : ScriptableObject
{
    public Sound[] weapons;
    public Sound[] containers;
    public Sound[] afterShotEffects;
}
#if UNITY_EDITOR
[CustomEditor(typeof(SoundContainer))]
public class SoundContainerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SoundContainer soundContainer = (SoundContainer)target;
        EditorGUILayout.Space();
        //FILL HERE FOR NEW ARRAYS
        DisplaySoundArray(soundContainer.weapons);
        DisplaySoundArray(soundContainer.containers);
        DisplaySoundArray(soundContainer.afterShotEffects);
    }
    private void DisplaySoundArray(Sound[] soundArray)
    {
        if (soundArray != null)
        {
            for (int i = 0; i < soundArray.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();

                if (soundArray[i] != null && soundArray[i].audioClip != null)
                {
                    soundArray[i].name = soundArray[i].audioClip.name;
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif