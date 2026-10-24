using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectValidation
{
    public static void Run()
    {
        ValidateBuildSettings();
        ValidateBootstrap();
        ValidateDemoHouse();
        Debug.Log("PROJECT_VALIDATION_PASS: build scenes, systems, characters, physics and script references are valid.");
    }

    private static void ValidateBuildSettings()
    {
        string[] expected = { "Assets/Scenes/Bootstrap.unity", "Assets/Scenes/DemoHouse.unity" };
        string[] actual = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        Require(actual.SequenceEqual(expected), "Build Settings must contain Bootstrap followed by DemoHouse.");
    }

    private static void ValidateBootstrap()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Single);
        Require(UnityEngine.Object.FindFirstObjectByType<GameStateService>() != null, "Bootstrap is missing GameStateService.");
        Require(UnityEngine.Object.FindFirstObjectByType<GameSaveService>() != null, "Bootstrap is missing GameSaveService.");
        Require(UnityEngine.Object.FindFirstObjectByType<WorldClock>() != null, "Bootstrap is missing WorldClock.");
        Require(UnityEngine.Object.FindFirstObjectByType<RewindManager>() != null, "Bootstrap is missing RewindManager.");
        Require(UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() != null, "Bootstrap is missing GameBootstrap.");
        ValidateNoMissingScripts();
    }

    private static void ValidateDemoHouse()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DemoHouse.unity", OpenSceneMode.Single);
        NarrativeCharacter[] characters = UnityEngine.Object.FindObjectsByType<NarrativeCharacter>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Require(characters.Any(character => character.Identity == StoryCharacterId.Min), "DemoHouse is missing Min.");
        Require(characters.Any(character => character.Identity == StoryCharacterId.Miu), "DemoHouse is missing Miu.");
        Require(characters.All(character => character.Definition != null), "A narrative character has no definition.");
        Require(characters.Select(character => character.RewindId).Distinct().Count() == characters.Length,
            "Narrative character rewind IDs must be unique.");
        Require(UnityEngine.Object.FindFirstObjectByType<WorldTimeline>() != null, "DemoHouse is missing WorldTimeline.");
        ValidateFullBodyPlayer();

        GameObject house = GameObject.Find("House2163_Shell");
        Require(house != null, "DemoHouse is missing House2163_Shell.");
        int inappropriateConvex = house.GetComponentsInChildren<MeshCollider>(true)
            .Count(collider => collider.convex && collider.GetComponentInParent<Rigidbody>() == null);
        Require(inappropriateConvex == 0, "Static house geometry still contains convex MeshColliders.");
        ValidateNoMissingScripts();
    }

    private static void ValidateFullBodyPlayer()
    {
        GameObject playerModel = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name == "Player_Rigged_Physics");
        Require(playerModel != null, "DemoHouse is missing the full-body Player model.");
        Require(PrefabUtility.GetPrefabInstanceStatus(playerModel) != PrefabInstanceStatus.MissingAsset,
            "The Player model is still connected to a missing prefab.");

        SkinnedMeshRenderer[] bodyRenderers = playerModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Require(bodyRenderers.Length > 0, "Player has no skinned body renderer for first-person body or mirrors.");
        Require(bodyRenderers.All(renderer => renderer.sharedMesh != null), "A Player body renderer has no mesh.");
        Require(bodyRenderers.Sum(renderer => renderer.sharedMesh.vertexCount) > 0, "Player body meshes are empty.");

        Animator animator = playerModel.GetComponentInChildren<Animator>(true);
        Require(animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman,
            "Player requires a valid Humanoid Animator for body movement and mirror reflection.");
        Require(UnityEngine.Object.FindFirstObjectByType<PlayerController>() != null, "DemoHouse is missing PlayerController.");
        Require(UnityEngine.Object.FindFirstObjectByType<PlayerLook>() != null, "DemoHouse is missing the original PlayerLook.");
        Require(UnityEngine.Object.FindFirstObjectByType<PlayerBodyAnimator>() != null,
            "DemoHouse is missing PlayerBodyAnimator.");

        PlayerCameraVisibility visibility = UnityEngine.Object.FindFirstObjectByType<PlayerCameraVisibility>();
        Require(visibility != null && visibility.PlayerCamera != null && visibility.PlayerModel != null,
            "PlayerCameraVisibility is not configured.");
        int playerLayer = LayerMask.NameToLayer("PlayerAvatar");
        Require(playerLayer >= 0, "The PlayerAvatar layer is missing.");
        Require((visibility.PlayerCamera.cullingMask & (1 << playerLayer)) == 0,
            "The Main Camera still renders PlayerAvatar.");
        Require(bodyRenderers.All(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy),
            "A Player body renderer was disabled; use camera culling instead of hiding the body.");
        Require(playerModel.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.gameObject.layer == playerLayer),
            "All Player renderers must use the PlayerAvatar layer.");

        Debug.Log($"FULL_BODY_PLAYER_VALID: {bodyRenderers.Length} skinned renderers, " +
                  $"{bodyRenderers.Sum(renderer => renderer.sharedMesh.vertexCount)} vertices, Humanoid Animator ready.");
    }

    private static void ValidateNoMissingScripts()
    {
        var missing = new List<string>();
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) > 0)
                missing.Add(item.name);
        Require(missing.Count == 0, $"Missing scripts found on: {string.Join(", ", missing)}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
