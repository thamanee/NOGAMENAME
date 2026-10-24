using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCameraVisibility : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerModel;
    [SerializeField] private string playerLayerName = "PlayerAvatar";

    public Camera PlayerCamera => playerCamera;
    public Transform PlayerModel => playerModel;

    private void Awake() => ApplyVisibility();

    public void Configure(Camera cameraToExcludePlayer, Transform fullBodyModel)
    {
        playerCamera = cameraToExcludePlayer;
        playerModel = fullBodyModel;
        ApplyVisibility();
    }

    [ContextMenu("Apply Player Camera Visibility")]
    public void ApplyVisibility()
    {
        int playerLayer = LayerMask.NameToLayer(playerLayerName);
        if (playerLayer < 0 || playerCamera == null || playerModel == null)
        {
            Debug.LogError("PlayerCameraVisibility requires the PlayerAvatar layer, Main Camera and full-body model.", this);
            return;
        }

        foreach (Renderer bodyPart in playerModel.GetComponentsInChildren<Renderer>(true))
            bodyPart.gameObject.layer = playerLayer;

        playerCamera.cullingMask &= ~(1 << playerLayer);
    }
}
