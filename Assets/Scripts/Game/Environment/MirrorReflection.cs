using UnityEngine;

[DisallowMultipleComponent]
public sealed class MirrorReflection : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Camera reflectionCamera;
    [SerializeField] private Renderer mirrorSurface;
    [SerializeField] private RenderTexture reflectionTexture;

    public Camera SourceCamera => sourceCamera;
    public Camera ReflectionCamera => reflectionCamera;
    public Renderer MirrorSurface => mirrorSurface;
    public RenderTexture ReflectionTexture => reflectionTexture;

    public void Configure(Camera source, Camera reflection, Renderer surface, RenderTexture texture)
    {
        sourceCamera = source;
        reflectionCamera = reflection;
        mirrorSurface = surface;
        reflectionTexture = texture;
        ApplySettings();
    }

    private void Awake() => ApplySettings();

    private void LateUpdate()
    {
        if (sourceCamera == null || reflectionCamera == null)
            return;

        Vector3 planePosition = transform.position;
        Vector3 planeNormal = transform.forward;
        Transform source = sourceCamera.transform;
        Transform reflection = reflectionCamera.transform;

        float distance = Vector3.Dot(planeNormal, source.position - planePosition);
        Vector3 reflectedPosition = source.position - 2f * distance * planeNormal;
        Vector3 reflectedForward = Vector3.Reflect(source.forward, planeNormal);
        Vector3 reflectedUp = Vector3.Reflect(source.up, planeNormal);

        reflection.SetPositionAndRotation(
            reflectedPosition,
            Quaternion.LookRotation(reflectedForward, reflectedUp));
        reflectionCamera.fieldOfView = sourceCamera.fieldOfView;
        reflectionCamera.aspect = sourceCamera.aspect;
    }

    private void ApplySettings()
    {
        if (reflectionCamera != null)
        {
            reflectionCamera.targetTexture = reflectionTexture;
            reflectionCamera.enabled = true;
        }

        if (mirrorSurface != null && mirrorSurface.sharedMaterial != null && reflectionTexture != null)
            mirrorSurface.sharedMaterial.mainTexture = reflectionTexture;
    }
}
