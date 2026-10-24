using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MirrorPrototypeSetup
{
    private const string DemoScene = "Assets/Scenes/DemoHouse.unity";
    private const string DataFolder = "Assets/Game/Environment/Mirror";
    private const string TexturePath = DataFolder + "/MirrorReflection.renderTexture";
    private const string MaterialPath = DataFolder + "/MirrorSurface.mat";

    [InitializeOnLoadMethod]
    private static void ConfigureWhenDemoIsOpen()
    {
        EditorApplication.delayCall += TryConfigureOpenDemo;
    }

    private static void TryConfigureOpenDemo()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryConfigureOpenDemo;
            return;
        }

        if (SceneManager.GetActiveScene().path == DemoScene && GameObject.Find("MirrorPrototype") == null)
            Run();
    }

    [MenuItem("NOGAMENAME/Build Mirror Prototype")]
    public static void Run()
    {
        EnsureFolders();
        RenderTexture texture = EnsureRenderTexture();
        Material material = EnsureMirrorMaterial(texture);

        Scene scene = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
        Camera sourceCamera = Camera.main;
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (sourceCamera == null || player == null)
            throw new System.InvalidOperationException("DemoHouse requires Main Camera and PlayerController.");

        GameObject previous = GameObject.Find("MirrorPrototype");
        if (previous != null)
            Object.DestroyImmediate(previous);

        Vector3 viewDirection = Vector3.ProjectOnPlane(sourceCamera.transform.forward, Vector3.up).normalized;
        if (viewDirection.sqrMagnitude < 0.01f)
            viewDirection = player.transform.forward;

        GameObject root = new("MirrorPrototype");
        root.transform.SetPositionAndRotation(
            player.transform.position + viewDirection * 2.2f + Vector3.up * 1.15f,
            Quaternion.LookRotation(-viewDirection, Vector3.up));

        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surface.name = "MirrorSurface";
        surface.transform.SetParent(root.transform, false);
        surface.transform.localScale = new Vector3(1.25f, 2f, 1f);
        surface.layer = LayerMask.NameToLayer("MirrorSurface");
        Object.DestroyImmediate(surface.GetComponent<Collider>());
        Renderer surfaceRenderer = surface.GetComponent<Renderer>();
        surfaceRenderer.sharedMaterial = material;

        Material frameMaterial = EnsureFrameMaterial();
        CreateFramePiece(root.transform, "FrameTop", new Vector3(0f, 1.04f, .03f), new Vector3(1.38f, .08f, .08f), frameMaterial);
        CreateFramePiece(root.transform, "FrameBottom", new Vector3(0f, -1.04f, .03f), new Vector3(1.38f, .08f, .08f), frameMaterial);
        CreateFramePiece(root.transform, "FrameLeft", new Vector3(-.665f, 0f, .03f), new Vector3(.08f, 2f, .08f), frameMaterial);
        CreateFramePiece(root.transform, "FrameRight", new Vector3(.665f, 0f, .03f), new Vector3(.08f, 2f, .08f), frameMaterial);

        GameObject cameraObject = new("MirrorCamera");
        cameraObject.transform.SetParent(root.transform, false);
        Camera reflectionCamera = cameraObject.AddComponent<Camera>();
        reflectionCamera.CopyFrom(sourceCamera);
        reflectionCamera.tag = "Untagged";
        reflectionCamera.depth = sourceCamera.depth - 1f;
        reflectionCamera.nearClipPlane = .05f;
        reflectionCamera.targetTexture = texture;
        int playerLayer = LayerMask.NameToLayer("PlayerAvatar");
        int mirrorLayer = LayerMask.NameToLayer("MirrorSurface");
        reflectionCamera.cullingMask |= 1 << playerLayer;
        reflectionCamera.cullingMask &= ~(1 << mirrorLayer);
        reflectionCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;

        MirrorReflection mirror = root.AddComponent<MirrorReflection>();
        mirror.Configure(sourceCamera, reflectionCamera, surfaceRenderer, texture);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MIRROR_PROTOTYPE_COMPLETE: Main Camera hides PlayerAvatar; MirrorCamera renders the active full-body Player.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Game/Environment"))
            AssetDatabase.CreateFolder("Assets/Game", "Environment");
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/Game/Environment", "Mirror");
    }

    private static RenderTexture EnsureRenderTexture()
    {
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(TexturePath);
        if (texture != null) return texture;
        texture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
        {
            name = "MirrorReflection",
            antiAliasing = 2,
            useMipMap = false,
            autoGenerateMips = false
        };
        AssetDatabase.CreateAsset(texture, TexturePath);
        return texture;
    }

    private static Material EnsureMirrorMaterial(RenderTexture texture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new System.InvalidOperationException("URP Unlit shader was not found.");
            material = new Material(shader) { name = "MirrorSurface" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.mainTexture = texture;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material EnsureFrameMaterial()
    {
        const string path = DataFolder + "/MirrorFrame.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader) { name = "MirrorFrame", color = new Color(.08f, .06f, .05f) };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateFramePiece(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = position;
        piece.transform.localScale = scale;
        piece.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(piece.GetComponent<Collider>());
    }
}
