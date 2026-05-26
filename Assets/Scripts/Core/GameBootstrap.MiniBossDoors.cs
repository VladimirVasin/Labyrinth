using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void RefreshCentralExitSeal()
        {
            var door = GetCentralExitDoor(currentMaze);
            if (door == null)
            {
                return;
            }

            var shouldSeal = door.IsClosed && mobManager != null && mobManager.HasCentralMiniBossAlive;
            door.SetSealed(shouldSeal, "Сначала победите мини-босса в центральной комнате.");
        }

        private void UnsealCentralExitDoorAfterMiniBoss()
        {
            var door = GetCentralExitDoor(currentMaze);
            if (door == null)
            {
                return;
            }

            door.SetSealed(false);
            if (door.Open(currentMaze != null ? currentMaze.Grid : null))
            {
                GameAudioController.Play(GameSfx.DoorOpen, mazeRenderer.GridToWorld(door.Position));
            }

            GameDebugLog.Info("Dungeon", $"MiniBoss defeated, {door.Name} opened at {GameDebugLog.Position(door.Position)}.");
        }

        private static CentralDoorModel GetCentralExitDoor(MazeGenerationResult maze)
        {
            if (maze == null || !maze.CentralRoom.IsValid || maze.CentralDoors == null)
            {
                return null;
            }

            foreach (var door in maze.CentralDoors)
            {
                if (door != null && door.Position == maze.CentralRoom.ExitPosition)
                {
                    return door;
                }
            }

            return null;
        }
    }
}
