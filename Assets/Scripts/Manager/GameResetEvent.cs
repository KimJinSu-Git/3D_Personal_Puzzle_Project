using System;

public static class GameResetEvent
{
    public static event Action OnPlayerReset;

    public static void BroadcastPlayerReset()
    {
        OnPlayerReset?.Invoke();
    }
}