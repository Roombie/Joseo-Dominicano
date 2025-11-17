using UnityEngine;
using Unity.Cinemachine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.5f, 0.5f);

    [Header("Cinemachine Target Camera")]
    [Tooltip("Assign the Cinemachine Camera that drives this parallax background.")]
    [SerializeField] private CinemachineCamera targetVirtualCamera;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;

    private void Start()
    {
        if (targetVirtualCamera == null)
        {
            Debug.LogWarning($"[{nameof(ParallaxBackground)}] No Cinemachine Camera assigned on {name}.", this);
            enabled = false;
            return;
        }

        // In Cinemachine 3.x, the camera is a component on the same GameObject
        cameraTransform = targetVirtualCamera.transform;
        lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        if (targetVirtualCamera == null) return;

        if (cameraTransform == null)
            cameraTransform = targetVirtualCamera.transform;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(
            deltaMovement.x * parallaxMultiplier.x,
            deltaMovement.y * parallaxMultiplier.y,
            0f
        );

        lastCameraPosition = cameraTransform.position;
    }
}