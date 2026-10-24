using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerVrmRecovery
{
    private const string PlayerVrm = "Assets/Game/Characters/Player/Player_Rigged_Physics.vrm";
    private const string DemoScene = "Assets/Scenes/DemoHouse.unity";

    [InitializeOnLoadMethod]
    private static void ConfigureVisibilityWhenDemoIsOpen()
    {
        EditorApplication.delayCall += TryConfigureVisibilityInOpenScene;
    }

    private static void TryConfigureVisibilityInOpenScene()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryConfigureVisibilityInOpenScene;
            return;
        }

        if (SceneManager.GetActiveScene().path != DemoScene)
            return;

        PlayerCameraVisibility existing = Object.FindFirstObjectByType<PlayerCameraVisibility>();
        int playerLayer = LayerMask.NameToLayer("PlayerAvatar");
        bool alreadyConfigured = existing != null && existing.PlayerCamera != null && existing.PlayerModel != null &&
                                 playerLayer >= 0 &&
                                 (existing.PlayerCamera.cullingMask & (1 << playerLayer)) == 0 &&
                                 existing.PlayerModel.GetComponentsInChildren<Renderer>(true)
                                     .All(renderer => renderer.gameObject.layer == playerLayer);
        if (!alreadyConfigured)
            ConfigurePlayerCameraVisibility();
    }

    [MenuItem("NOGAMENAME/Repair Player VRM")]
    public static void RecoverPlayer()
    {
        AssetDatabase.ImportAsset(PlayerVrm,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVrm);
        if (playerPrefab == null)
        {
            Debug.LogError($"PLAYER_VRM_RECOVERY_FAILED: Unity could not import {PlayerVrm} as a GameObject.");
            return;
        }

        EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);

        GameObject player = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == "Player_Rigged_Physics");

        bool missingPrefab = player == null ||
            PrefabUtility.GetPrefabInstanceStatus(player) == PrefabInstanceStatus.MissingAsset;
        if (missingPrefab)
        {
            Debug.LogError("PLAYER_VRM_RECOVERY_FAILED: DemoHouse still contains a missing Player prefab instance.");
            return;
        }

        Debug.Log("PLAYER_VRM_RECOVERY_COMPLETE: Player VRM was reimported and DemoHouse references are valid.");
    }

    [MenuItem("NOGAMENAME/Configure Player Camera Visibility")]
    public static void ConfigurePlayerCameraVisibility()
    {
        Scene scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
        PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
        Camera playerCamera = Camera.main;
        GameObject playerModel = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == "Player_Rigged_Physics");

        if (controller == null || playerCamera == null || playerModel == null)
            throw new System.InvalidOperationException("Player root, Main Camera or full-body model was not found.");

        PlayerCameraVisibility visibility = controller.GetComponent<PlayerCameraVisibility>();
        if (visibility == null)
            visibility = controller.gameObject.AddComponent<PlayerCameraVisibility>();
        visibility.Configure(playerCamera, playerModel.transform);

        EditorUtility.SetDirty(visibility);
        EditorUtility.SetDirty(playerCamera);
        foreach (Renderer renderer in playerModel.GetComponentsInChildren<Renderer>(true))
            EditorUtility.SetDirty(renderer.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("PLAYER_CAMERA_VISIBILITY_COMPLETE: Main Camera excludes PlayerAvatar; the full body remains active for mirror cameras.");
    }
}
