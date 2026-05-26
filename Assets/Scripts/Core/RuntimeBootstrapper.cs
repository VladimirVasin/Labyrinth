using UnityEngine;

namespace Labyrinth.Core
{
    public static class RuntimeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameBootstrap()
        {
            if (Object.FindAnyObjectByType<GameBootstrap>() != null)
            {
                return;
            }

            var bootstrapObject = new GameObject("GameBootstrap");
            bootstrapObject.AddComponent<GameBootstrap>();
        }
    }
}
