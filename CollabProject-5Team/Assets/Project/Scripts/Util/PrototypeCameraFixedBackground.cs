using UnityEngine;

[ExecuteAlways]
public sealed class PrototypeCameraFixedBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float fillScale = 1.35f;
    [SerializeField] private float distanceFromCamera = 2f;

    public void Configure(Camera camera, float newFillScale, float newDistanceFromCamera)
    {
        targetCamera = camera;
        fillScale = Mathf.Max(1f, newFillScale);
        distanceFromCamera = Mathf.Max(0.1f, newDistanceFromCamera);
        SyncToCamera();
    }

    private void LateUpdate()
    {
        SyncToCamera();
    }

    private void OnValidate()
    {
        fillScale = Mathf.Max(1f, fillScale);
        distanceFromCamera = Mathf.Max(0.1f, distanceFromCamera);
    }

    private void SyncToCamera()
    {
        Camera camera = targetCamera != null ? targetCamera : Camera.main;
        if (camera == null)
            return;

        float distance = GetBackgroundDistance(camera);
        transform.position = camera.transform.position + camera.transform.forward * distance;
        transform.rotation = camera.transform.rotation;

        float height;
        if (camera.orthographic)
        {
            height = camera.orthographicSize * 2f;
        }
        else
        {
            height = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        float width = height * Mathf.Max(0.1f, camera.aspect);
        transform.localScale = new Vector3(width * fillScale, height * fillScale, 1f);
    }

    private float GetBackgroundDistance(Camera camera)
    {
        return Mathf.Max(camera.nearClipPlane + 0.1f, distanceFromCamera);
    }
}
