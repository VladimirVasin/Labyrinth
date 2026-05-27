using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void ClearTerrainDecorationsAround(Vector2Int position, int footprintRadius)
        {
            terrainDecorations?.ClearAround(position, footprintRadius + BaseDevelopment.BuildingVisibilityPaddingCells);
        }
    }
}
