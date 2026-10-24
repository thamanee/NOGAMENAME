using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-400)]
public sealed class GameSaveService : MonoBehaviour
{
    [SerializeField] private string fileName = "progress.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Start()
    {
        LoadPersistentProgress();
        if (GameStateService.Instance != null)
            GameStateService.Instance.PersistentProgressChanged += SavePersistentProgress;
    }

    private void OnDestroy()
    {
        if (GameStateService.Instance != null)
            GameStateService.Instance.PersistentProgressChanged -= SavePersistentProgress;
    }

    public void SavePersistentProgress()
    {
        if (GameStateService.Instance == null) return;
        string json = JsonUtility.ToJson(GameStateService.Instance.CapturePersistentProgress(), true);
        string temporaryPath = SavePath + ".tmp";
        File.WriteAllText(temporaryPath, json);
        if (File.Exists(SavePath)) File.Delete(SavePath);
        File.Move(temporaryPath, SavePath);
    }

    public void LoadPersistentProgress()
    {
        if (GameStateService.Instance == null || !File.Exists(SavePath)) return;
        try
        {
            PersistentProgressData data = JsonUtility.FromJson<PersistentProgressData>(File.ReadAllText(SavePath));
            GameStateService.Instance.RestorePersistentProgress(data);
        }
        catch (System.Exception error)
        {
            Debug.LogWarning($"Save data could not be loaded: {error.Message}", this);
        }
    }
}
