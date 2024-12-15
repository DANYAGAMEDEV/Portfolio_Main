using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Optimization 
{
    public static bool CameraDistanceDefault(Transform _t)
    {
        float maxDistance = 100f;

        Camera mainCamera = Camera.main;
        float distance = Vector3.Distance(_t.position, mainCamera.transform.position);

        if (distance > maxDistance) return true;
        else return false;
    }
    public static void RenderersUpdate( ref SkinnedMeshRenderer[] renderers)
    {
        foreach (var renderer in renderers)
        {
            if (renderer.isVisible) renderer.updateWhenOffscreen = true;
            else renderer.updateWhenOffscreen = false;
        }
    }
    public static void AnimatorCulling(ref Animator _animator, bool _b)
    {
       if(_b) _animator.cullingMode = AnimatorCullingMode.CullCompletely;
       else _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }
}
