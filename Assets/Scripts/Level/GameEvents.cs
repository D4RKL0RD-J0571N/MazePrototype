using System;

namespace Level
{
    public static class GameEvents
    {
        public static event Action OnPlayerReachedExit;

        public static void TriggerPlayerReachedExit()
        {
            OnPlayerReachedExit?.Invoke();
        }
    }
}