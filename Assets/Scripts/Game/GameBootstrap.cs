using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private string firstScene = "DemoHouse";

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(firstScene) && SceneManager.GetActiveScene().name != firstScene)
            SceneManager.LoadScene(firstScene);
    }
}
