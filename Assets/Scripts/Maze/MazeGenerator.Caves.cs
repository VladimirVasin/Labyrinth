using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeGenerator
    {
        private static bool IsCaveBlockedByCentralPassage(Vector2Int center, CentralRoomInfo centralRoom)
        {
            var radius = CaveSize / 2;
            var caveMinX = center.x - radius;
            var caveMaxX = center.x + radius;
            var caveMinY = center.y - radius;
            var caveMaxY = center.y + radius;

            if (caveMinX <= centralRoom.Max.x && caveMaxX >= centralRoom.Min.x)
            {
                return true;
            }

            return caveMinX <= centralRoom.Max.x + 1
                && caveMaxX >= centralRoom.Min.x - 1
                && caveMinY <= centralRoom.Max.y + 1
                && caveMaxY >= centralRoom.Min.y - 1;
        }

        private static List<CaveEntranceContact> CollectExternalPathContacts(MazeGrid grid, Vector2Int center)
        {
            var contacts = new List<CaveEntranceContact>();
            var radius = CaveSize / 2;
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var y = center.y - radius; y <= center.y + radius; y++)
                {
                    var caveCell = new Vector2Int(x, y);
                    foreach (var direction in MazeDirections.Cardinal)
                    {
                        var external = caveCell + direction;
                        if (IsInsideCave(external, center, radius)
                            || !grid.InBounds(external)
                            || !grid.Get(external).IsWalkable
                            || ContainsExternalContact(contacts, external))
                        {
                            continue;
                        }

                        contacts.Add(new CaveEntranceContact(caveCell, external));
                    }
                }
            }

            return contacts;
        }

        private static bool IsInsideCave(Vector2Int position, Vector2Int center, int radius)
        {
            return Mathf.Abs(position.x - center.x) <= radius
                && Mathf.Abs(position.y - center.y) <= radius;
        }

        private static bool ContainsExternalContact(IReadOnlyList<CaveEntranceContact> contacts, Vector2Int externalPosition)
        {
            foreach (var contact in contacts)
            {
                if (contact.ExternalPosition == externalPosition)
                {
                    return true;
                }
            }

            return false;
        }

        private static CaveEntranceContact SelectCaveEntranceContact(
            IReadOnlyList<CaveEntranceContact> contacts,
            Vector2Int mazeEntrance)
        {
            var best = contacts[0];
            var bestDistance = GridDistance(best.ExternalPosition, mazeEntrance);

            for (var i = 1; i < contacts.Count; i++)
            {
                var contact = contacts[i];
                var distance = GridDistance(contact.ExternalPosition, mazeEntrance);
                if (distance < bestDistance
                    || (distance == bestDistance && IsEarlierPosition(contact.EntrancePosition, best.EntrancePosition)))
                {
                    best = contact;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool IsEarlierPosition(Vector2Int current, Vector2Int best)
        {
            return current.x < best.x || (current.x == best.x && current.y < best.y);
        }

        private static void ApplyCaveCandidate(
            MazeGrid grid,
            Vector2Int center,
            IReadOnlyList<CaveEntranceContact> contacts,
            CaveEntranceContact selectedContact,
            List<CellSnapshot> snapshots)
        {
            var radius = CaveSize / 2;
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var y = center.y - radius; y <= center.y + radius; y++)
                {
                    SetTypeWithSnapshot(grid, new Vector2Int(x, y), MazeCellType.Path, snapshots);
                }
            }

            foreach (var contact in contacts)
            {
                if (contact.ExternalPosition == selectedContact.ExternalPosition)
                {
                    continue;
                }

                SetTypeWithSnapshot(grid, contact.ExternalPosition, MazeCellType.Wall, snapshots);
            }
        }

        private static void SetTypeWithSnapshot(
            MazeGrid grid,
            Vector2Int position,
            MazeCellType type,
            List<CellSnapshot> snapshots)
        {
            if (!ContainsSnapshot(snapshots, position))
            {
                snapshots.Add(new CellSnapshot(position, grid.Get(position).Type));
            }

            grid.SetType(position, type);
        }

        private static bool ContainsSnapshot(IReadOnlyList<CellSnapshot> snapshots, Vector2Int position)
        {
            foreach (var snapshot in snapshots)
            {
                if (snapshot.Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RestoreSnapshots(MazeGrid grid, IReadOnlyList<CellSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                grid.SetType(snapshot.Position, snapshot.Type);
            }
        }

        private static bool AllWalkableCellsReachable(MazeGrid grid, Vector2Int entrance)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance);
            foreach (var cell in grid.Cells())
            {
                if (!cell.IsWalkable)
                {
                    continue;
                }

                if (!distances.ContainsKey(new Vector2Int(cell.X, cell.Y)))
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle(List<Vector2Int> positions, System.Random random)
        {
            for (var i = positions.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = positions[i];
                positions[i] = positions[j];
                positions[j] = temp;
            }
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static int MakeOdd(int value)
        {
            if (value % 2 != 0)
            {
                return value;
            }

            return value <= 1 ? value + 1 : value - 1;
        }

        private readonly struct CaveEntranceContact
        {
            public CaveEntranceContact(Vector2Int entrancePosition, Vector2Int externalPosition)
            {
                EntrancePosition = entrancePosition;
                ExternalPosition = externalPosition;
            }

            public Vector2Int EntrancePosition { get; }

            public Vector2Int ExternalPosition { get; }
        }

        private readonly struct CellSnapshot
        {
            public CellSnapshot(Vector2Int position, MazeCellType type)
            {
                Position = position;
                Type = type;
            }

            public Vector2Int Position { get; }

            public MazeCellType Type { get; }
        }

        private enum CavePlacementStatus
        {
            Placed,
            TooCloseToEntrance,
            TooCloseToOtherCave,
            NoExternalContact,
            DisconnectsMaze,
            BlocksCentralPassage
        }
    }
}
