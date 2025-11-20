using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class CinemachineOrthoSizeManager : MonoBehaviour
{
    [Header("Base size (16:9)")]
    public float baseSize = 7.29f;

    [Header("Aspect ratio base")]
    public float referenceAspect = 16f / 9f;

    private List<CinemachineCamera> vCams = new List<CinemachineCamera>();
    private List<Camera> unityCameras = new List<Camera>();

    private int lastW, lastH;

    private void Awake()
    {
        RefreshCameraLists();

        // Listener correcto para Cinemachine 3.x
        CinemachineCore.CameraActivatedEvent.AddListener(OnCinemachineActivated);
    }

    private void OnDestroy()
    {
        CinemachineCore.CameraActivatedEvent.RemoveListener(OnCinemachineActivated);
    }

    private void Start()
    {
        ApplySize();
    }

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
        {
            lastW = Screen.width;
            lastH = Screen.height;
            ApplySize();
        }
    }

    /// <summary>
    /// Evento correcto para Cinemachine 3.x
    /// </summary>
    private void OnCinemachineActivated(ICinemachineCamera.ActivationEventParams evt)
    {
        ApplySize();
    }

    private void RefreshCameraLists()
    {
        vCams.Clear();
        unityCameras.Clear();

        vCams.AddRange(
            Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        );

        unityCameras.AddRange(
            Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        );
    }

    private void ApplySize()
    {
        float currentAspect = (float)Screen.width / Screen.height;
        float scale = referenceAspect / currentAspect;
        float finalSize = baseSize * scale;

        // Cinemachine cameras
        foreach (var vcam in vCams)
        {
            if (vcam == null) continue;

            var lens = vcam.Lens;
            lens.OrthographicSize = finalSize;
            vcam.Lens = lens; // importantísimo: Lens es struct
        }

        // Unity cameras (Main Camera real)
        foreach (var cam in unityCameras)
        {
            if (cam == null || !cam.orthographic) continue;
            cam.orthographicSize = finalSize;
        }
    }
}