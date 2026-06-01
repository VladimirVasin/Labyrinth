using Labyrinth.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public readonly struct CaveInfo
    {
        public CaveInfo(Vector2Int center, Vector2Int entrancePosition)
        {
            Center = center;
            EntrancePosition = entrancePosition;
        }

        public Vector2Int Center { get; }

        public Vector2Int EntrancePosition { get; }

        public bool IsValid => Center != default || EntrancePosition != default;
    }

    public readonly struct CentralRoomInfo
    {
        public CentralRoomInfo(
            Vector2Int min,
            Vector2Int max,
            Vector2Int entrancePosition,
            Vector2Int entranceExternalPosition,
            Vector2Int exitPosition,
            Vector2Int exitExternalPosition)
        {
            Min = min;
            Max = max;
            EntrancePosition = entrancePosition;
            EntranceExternalPosition = entranceExternalPosition;
            ExitPosition = exitPosition;
            ExitExternalPosition = exitExternalPosition;
        }

        public Vector2Int Min { get; }

        public Vector2Int Max { get; }

        public Vector2Int EntrancePosition { get; }

        public Vector2Int EntranceExternalPosition { get; }

        public Vector2Int ExitPosition { get; }

        public Vector2Int ExitExternalPosition { get; }

        public int Width => Max.x - Min.x + 1;

        public int Height => Max.y - Min.y + 1;

        public bool IsValid => Max.x > Min.x && Max.y > Min.y;

        public bool Contains(Vector2Int position)
        {
            return position.x >= Min.x
                && position.x <= Max.x
                && position.y >= Min.y
                && position.y <= Max.y;
        }

        public bool IsBeyondExitSide(Vector2Int position)
        {
            return position.x > Max.x;
        }
    }

    public enum CentralDoorState
    {
        Closed,
        Open
    }

    public sealed class CentralDoorModel
    {
        private GameObject visualObject;

        public CentralDoorModel(string name, Vector2Int position, Vector2Int externalPosition)
        {
            Name = name;
            Position = position;
            ExternalPosition = externalPosition;
            State = CentralDoorState.Closed;
        }

        public string Name { get; }

        public Vector2Int Position { get; }

        public Vector2Int ExternalPosition { get; }

        public CentralDoorState State { get; private set; }

        public bool IsSealed { get; private set; }

        public string SealedReason { get; private set; } = string.Empty;

        public bool IsOpen => State == CentralDoorState.Open;

        public bool IsClosed => State == CentralDoorState.Closed;

        public void AttachVisual(GameObject visual)
        {
            visualObject = visual;
            if (visualObject != null)
            {
                visualObject.SetActive(IsClosed);
            }
        }

        public void SetSealed(bool sealedDoor, string reason = "")
        {
            IsSealed = sealedDoor;
            SealedReason = sealedDoor ? reason : string.Empty;
        }

        public bool Open(MazeGrid grid)
        {
            if (IsOpen)
            {
                return true;
            }

            if (IsSealed)
            {
                return false;
            }

            State = CentralDoorState.Open;
            if (grid != null && grid.InBounds(Position))
            {
                grid.SetType(Position, MazeCellType.OpenDoor);
            }

            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }

            return true;
        }
    }

    public sealed class KeyPickupModel
    {
        private GameObject visualObject;

        public KeyPickupModel(Vector2Int position, string itemName)
        {
            Position = position;
            ItemName = itemName;
        }

        public Vector2Int Position { get; private set; }

        public string ItemName { get; }

        public bool IsCollected { get; private set; }

        public bool IsAvailable => !IsCollected;

        public bool HasVisual => visualObject != null;

        public void AttachVisual(GameObject visual)
        {
            if (visualObject != null && visualObject != visual)
            {
                Object.Destroy(visualObject);
            }

            visualObject = visual;
            if (visualObject != null)
            {
                visualObject.SetActive(!IsCollected);
            }
        }

        public void Drop(Vector2Int position)
        {
            Position = position;
            IsCollected = false;
            if (visualObject != null)
            {
                visualObject.SetActive(true);
            }
        }

        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;
            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }
        }
    }

    public enum DungeonStairsDirection
    {
        Down,
        Up
    }

    public sealed class DungeonStairsModel
    {
        private GameObject closedVisual;
        private GameObject openVisual;

        public DungeonStairsModel(Vector2Int position, DungeonStairsDirection direction, int targetLevel, bool startsOpen)
        {
            Position = position;
            Direction = direction;
            TargetLevel = targetLevel;
            IsOpen = startsOpen;
        }

        public Vector2Int Position { get; }

        public DungeonStairsDirection Direction { get; }

        public int TargetLevel { get; }

        public bool IsOpen { get; private set; }

        public bool IsClosed => !IsOpen;

        public string DisplayName => Direction == DungeonStairsDirection.Down ? "Спуск вниз" : "Подъем наверх";

        public void AttachVisual(GameObject closedObject, GameObject openObject)
        {
            closedVisual = closedObject;
            openVisual = openObject;
            RefreshVisual();
        }

        public void Open(MazeGrid grid)
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            if (grid != null && grid.InBounds(Position))
            {
                grid.SetType(Position, MazeCellType.OpenDownStairs);
            }

            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (closedVisual != null)
            {
                closedVisual.SetActive(!IsOpen);
            }

            if (openVisual != null)
            {
                openVisual.SetActive(IsOpen);
            }
        }
    }

    public sealed class MazeGenerationResult
    {
        public MazeGenerationResult(
            MazeGrid grid,
            MazeGenerationSettings settings,
            Vector2Int basePosition,
            Vector2Int entrancePosition,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CentralDoorModel> centralDoors,
            KeyPickupModel centralRoomKey,
            IReadOnlyList<ChestModel> chests,
            IReadOnlyList<CaveInfo> caves,
            IReadOnlyList<OreDepositModel> oreDeposits,
            DungeonStairsModel downStairs = null,
            DungeonStairsModel upStairs = null,
            int levelNumber = 1,
            CaveInfo bossCave = default)
        {
            Grid = grid;
            Settings = settings;
            LevelNumber = levelNumber;
            BasePosition = basePosition;
            EntrancePosition = entrancePosition;
            CentralRoom = centralRoom;
            CentralDoors = centralDoors == null ? new List<CentralDoorModel>() : new List<CentralDoorModel>(centralDoors);
            CentralRoomKey = centralRoomKey;
            KeyPickups = new List<KeyPickupModel>();
            if (centralRoomKey != null)
            {
                KeyPickups.Add(centralRoomKey);
            }

            Chests = chests == null ? new List<ChestModel>() : new List<ChestModel>(chests);
            Caves = caves == null ? new List<CaveInfo>() : new List<CaveInfo>(caves);
            BossCave = bossCave;
            OreDeposits = oreDeposits == null ? new List<OreDepositModel>() : new List<OreDepositModel>(oreDeposits);
            DownStairs = downStairs;
            UpStairs = upStairs;
        }

        public MazeGrid Grid { get; }

        public MazeGenerationSettings Settings { get; }

        public int LevelNumber { get; }

        public Vector2Int BasePosition { get; }

        public Vector2Int EntrancePosition { get; }

        public CentralRoomInfo CentralRoom { get; }

        public IReadOnlyList<CentralDoorModel> CentralDoors { get; }

        public KeyPickupModel CentralRoomKey { get; }

        public List<KeyPickupModel> KeyPickups { get; }

        public IReadOnlyList<ChestModel> Chests { get; }

        public IReadOnlyList<CaveInfo> Caves { get; }

        public CaveInfo BossCave { get; }

        public IReadOnlyList<OreDepositModel> OreDeposits { get; }

        public DungeonStairsModel DownStairs { get; }

        public DungeonStairsModel UpStairs { get; }

        public KeyPickupModel GetOrCreateKeyPickup(Vector2Int position, string itemName)
        {
            for (var i = 0; i < KeyPickups.Count; i++)
            {
                var key = KeyPickups[i];
                if (key != null && key.ItemName == itemName)
                {
                    key.Drop(position);
                    return key;
                }
            }

            var created = new KeyPickupModel(position, itemName);
            KeyPickups.Add(created);
            return created;
        }
    }
}
