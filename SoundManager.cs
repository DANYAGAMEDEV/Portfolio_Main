using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class SoundManager: MonoBehaviour
{
    public SoundContainer soundContainer;

    public void PlayClipOneTime(AudioSource _source, Sound[] _soundArray, string _soundName)
    {
        Sound s = Array.Find(_soundArray, x => x.name == _soundName);
        _source.PlayOneShot(s.audioClip);

    }
    public void SetAudioClipNullAfterPlaying(ref AudioSource _source)
    {
        StartCoroutine(SetAudioClipNull(_source));
        IEnumerator SetAudioClipNull(AudioSource _source)
        {
            while (_source.isPlaying) yield return null;
            _source.clip = null;
        }
    } //If I need to set audiocli NULL after playing

}
