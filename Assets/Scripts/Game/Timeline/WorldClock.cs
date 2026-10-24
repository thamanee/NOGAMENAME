using UnityEngine;

public sealed class WorldClock : MonoBehaviour
{
    [SerializeField, Min(0f)] private float gameMinutesPerRealSecond = 1f;
    [SerializeField] private bool running = true;

    public bool Running { get => running; set => running = value; }

    private void Update()
    {
        if (!running || GameStateService.Instance == null)
            return;

        GameStateService.Instance.SetGameMinutes(
            GameStateService.Instance.GameMinutes + gameMinutesPerRealSecond * Time.deltaTime);
    }
}
