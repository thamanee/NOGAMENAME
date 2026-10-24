using NUnit.Framework;
using UnityEngine;

public sealed class GameStateServiceTests
{
    private GameObject systems;
    private GameStateService state;

    [SetUp]
    public void SetUp()
    {
        systems = new GameObject("GameStateServiceTests");
        state = systems.AddComponent<GameStateService>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(systems);
    }

    [Test]
    public void KnowledgeSurvivesWorldRestore()
    {
        state.Learn(GameFlagId.KnowMiuIdentity);
        state.SetWorldFlag(GameFlagId.MinAlive, true);
        WorldStateSnapshot snapshot = state.CaptureWorldState();
        state.SetWorldFlag(GameFlagId.MinAlive, false);

        state.RestoreWorldState(snapshot);

        Assert.That(state.HasKnowledge(GameFlagId.KnowMiuIdentity), Is.True);
        Assert.That(state.HasWorldFlag(GameFlagId.MinAlive), Is.True);
    }

    [Test]
    public void RewindUseReducesLimitAndRaisesPersistentChange()
    {
        int changeCount = 0;
        state.PersistentProgressChanged += () => changeCount++;
        float originalLimit = state.CurrentRewindLimitMinutes;

        bool accepted = state.TryConsumeRewind(10f);

        Assert.That(accepted, Is.True);
        Assert.That(state.RewindUseCount, Is.EqualTo(1));
        Assert.That(state.CurrentRewindLimitMinutes, Is.EqualTo(originalLimit - 5f));
        Assert.That(changeCount, Is.EqualTo(1));
    }

    [Test]
    public void PersistentProgressRoundTripsKnowledgeAndRewindCount()
    {
        state.Learn(GameFlagId.KnowMotherControl);
        state.TryConsumeRewind(1f);
        PersistentProgressData data = state.CapturePersistentProgress();

        Object.DestroyImmediate(systems);
        systems = new GameObject("RestoredGameStateService");
        state = systems.AddComponent<GameStateService>();
        state.RestorePersistentProgress(data);

        Assert.That(state.HasKnowledge(GameFlagId.KnowMotherControl), Is.True);
        Assert.That(state.RewindUseCount, Is.EqualTo(1));
    }
}
