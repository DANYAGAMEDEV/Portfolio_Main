using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CharacterNameController : MonoBehaviour
{
    private GameObject canvas;
    [SerializeField] private TextMeshProUGUI text;
    private CameraController cameraController;
    private float currentScale = - 1;

    [SerializeField] private RectTransform SelectionIcon;
    [SerializeField] private RectTransform StealthIcon;

    private void Start()
    {
        cameraController = Camera.main.transform.GetComponent<CameraController>();
        canvas = this.gameObject;
    }

    private void Update()
    {
        if (!Optimization.CameraDistanceDefault(transform))
        {
            if (!canvas.activeSelf) canvas.SetActive(true);
            canvas.transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
            if (currentScale != cameraController.currentZoom) StartCoroutine("SetLocalScaleSmooth");
        }
        else
        {
            if (canvas.activeSelf) canvas.SetActive(false);
        }
 
    }


    IEnumerator SetLocalScaleSmooth()
    {
        float k = Mathf.Sign(cameraController.currentZoom - currentScale);

        while ((currentScale < cameraController.currentZoom && k > 0) || (currentScale > cameraController.currentZoom && k < 0))
        {
            currentScale += (0.1f * k);
            float k1 = 0.1f + currentScale / 10f;
            float k2 = 0.1f + currentScale / 30f;
            text.transform.localScale = new Vector3(k1, k1, 1);
            StealthIcon.localScale = new Vector3(k2, k2, 1);
            SelectionIcon.localScale = new Vector3(k2, k2, 1);

            float xOffset = 0.3f;
            Vector3 xOffsetVector1 = new Vector3(((text.text.Length * -0.1f) - xOffset) * k2, 0f, 0f);
            SelectionIcon.localPosition = xOffsetVector1;

            Vector3 xOffsetVector2 = new Vector3(((text.text.Length * 0.1f) + xOffset) * k2, 0f, 0f);
            StealthIcon.localPosition = xOffsetVector2;

            yield return new WaitForSeconds(0.01f);
        }
        currentScale = cameraController.currentZoom;

    }
    public void SetName(string _name)
    {
        text.text = _name;
        currentScale = -1; //update local scale
        if (this.gameObject.layer != LayerMask.NameToLayer("UI.3D"))
        {
            this.gameObject.layer = LayerMask.NameToLayer("UI.3D");
            foreach (Transform t in transform) t.gameObject.layer = LayerMask.NameToLayer("UI.3D");
        }
    }
    public void SelectCharacter()
    {
        text.color = new Color(1, 1, 1, 1);
        SelectionIcon.gameObject.SetActive(true);

    }
    public void DeselectCharacter()
    {
        text.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        SelectionIcon.gameObject.SetActive(false);
        StealthIcon.gameObject.SetActive(false);
    }
    public void Stealth(bool _activate)
    {
        StealthIcon.gameObject.SetActive(_activate);
    }
}



