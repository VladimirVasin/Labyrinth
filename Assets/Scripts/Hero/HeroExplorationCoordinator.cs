using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public readonly struct HeroExplorationCandidate
    {
        public HeroExplorationCandidate(
            Vector2Int approachCell,
            Vector2Int targetCell,
            Queue<Vector2Int> path,
            int distance,
            int unknownNeighborCount,
            int strategicWeight)
        {
            ApproachCell = approachCell;
            TargetCell = targetCell;
            Path = path ?? new Queue<Vector2Int>();
            Distance = distance;
            UnknownNeighborCount = unknownNeighborCount;
            StrategicWeight = strategicWeight;
        }

        public Vector2Int ApproachCell { get; }

        public Vector2Int TargetCell { get; }

        public Queue<Vector2Int> Path { get; }

        public int Distance { get; }

        public int UnknownNeighborCount { get; }

        public int StrategicWeight { get; }
    }

    public sealed class HeroExplorationCoordinator
    {
        private const int DistanceWeight = 10;
        private const int UnknownNeighborBonus = 7;
        private const int OwnTargetStabilityBonus = 18;
        private const int SameTargetPenalty = 220;
        private const int NearbyTargetPenalty = 28;
        private const int SameSectorPenalty = 18;
        private const int RetargetLogDistance = 2;

        private readonly Dictionary<int, Reservation> reservationsByHero = new Dictionary<int, Reservation>();
        private readonly Dictionary<Vector2Int, int> targetOwners = new Dictionary<Vector2Int, int>();
        private Vector2Int entrancePosition;
        private int levelNumber;

        public void Reset(MazeGrid nextGrid, Vector2Int nextEntrancePosition, int nextLevelNumber)
        {
            if (nextGrid == null)
            {
                Clear();
                return;
            }

            entrancePosition = nextEntrancePosition;
            levelNumber = nextLevelNumber;
            reservationsByHero.Clear();
            targetOwners.Clear();
        }

        public void Clear()
        {
            entrancePosition = Vector2Int.zero;
            levelNumber = 0;
            reservationsByHero.Clear();
            targetOwners.Clear();
        }

        public bool TryGetReservedTarget(int heroNumber, out Vector2Int target)
        {
            if (reservationsByHero.TryGetValue(heroNumber, out var reservation))
            {
                target = reservation.TargetCell;
                return true;
            }

            target = default;
            return false;
        }

        public bool TryChooseTarget(
            int heroNumber,
            Vector2Int origin,
            IReadOnlyList<HeroExplorationCandidate> candidates,
            out HeroExplorationCandidate selected)
        {
            selected = default;
            if (candidates == null || candidates.Count == 0)
            {
                Release(heroNumber, "no frontier candidates");
                return false;
            }

            var hasPrevious = reservationsByHero.TryGetValue(heroNumber, out var previous);
            var bestScore = int.MaxValue;
            var bestPenalty = 0;
            var bestSector = 0;
            var bestIndex = -1;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var score = ScoreCandidate(
                    heroNumber,
                    origin,
                    candidate,
                    hasPrevious ? previous.TargetCell : default,
                    hasPrevious,
                    out var reservationPenalty,
                    out var sector);

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestPenalty = reservationPenalty;
                bestSector = sector;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                Release(heroNumber, "no scored frontier");
                return false;
            }

            selected = candidates[bestIndex];
            Reserve(heroNumber, selected, bestScore, bestPenalty, bestSector);
            return true;
        }

        public void CompleteTarget(int heroNumber, Vector2Int position)
        {
            if (!reservationsByHero.TryGetValue(heroNumber, out var reservation)
                || reservation.TargetCell != position)
            {
                return;
            }
            // Keep the completed cell reserved as a short-lived occupancy marker
            // until the hero picks the next target or leaves exploration.
        }

        public void Release(int heroNumber, string reason)
        {
            if (!reservationsByHero.TryGetValue(heroNumber, out var reservation))
            {
                return;
            }

            RemoveReservation(heroNumber, reservation.TargetCell);
            if (!string.IsNullOrWhiteSpace(reason) && reason != "completed")
            {
                GameDebugLog.Info(
                    "Hero",
                    $"Hero #{heroNumber} released exploration target: target={GameDebugLog.Position(reservation.TargetCell)}, reason={reason}, level={levelNumber}.");
            }
        }

        private void Reserve(
            int heroNumber,
            HeroExplorationCandidate candidate,
            int score,
            int reservationPenalty,
            int sector)
        {
            var changed = !reservationsByHero.TryGetValue(heroNumber, out var previous)
                || previous.TargetCell != candidate.TargetCell;

            if (changed && reservationsByHero.TryGetValue(heroNumber, out previous))
            {
                RemoveReservation(heroNumber, previous.TargetCell);
            }

            var next = new Reservation(candidate.TargetCell, candidate.ApproachCell, sector);
            reservationsByHero[heroNumber] = next;
            targetOwners[candidate.TargetCell] = heroNumber;

            if (!changed || (candidate.Distance < RetargetLogDistance && reservationPenalty <= 0))
            {
                return;
            }

            GameDebugLog.Info(
                "Hero",
                $"Hero #{heroNumber} assigned exploration target: target={GameDebugLog.Position(candidate.TargetCell)}, approach={GameDebugLog.Position(candidate.ApproachCell)}, distance={candidate.Distance}, unknownNeighbors={candidate.UnknownNeighborCount}, score={score}, crowdPenalty={reservationPenalty}, sector={sector}, level={levelNumber}.");
        }

        private int ScoreCandidate(
            int heroNumber,
            Vector2Int origin,
            HeroExplorationCandidate candidate,
            Vector2Int previousTarget,
            bool hasPrevious,
            out int reservationPenalty,
            out int sector)
        {
            sector = CalculateSector(candidate.TargetCell);
            reservationPenalty = CalculateReservationPenalty(heroNumber, candidate.TargetCell, sector);

            var score = candidate.Distance * DistanceWeight;
            score -= candidate.UnknownNeighborCount * UnknownNeighborBonus;
            score -= candidate.StrategicWeight;
            score += reservationPenalty;
            score += StableJitter(heroNumber, origin, candidate.TargetCell);

            if (hasPrevious && previousTarget == candidate.TargetCell)
            {
                score -= OwnTargetStabilityBonus;
            }

            return score;
        }

        private int CalculateReservationPenalty(int heroNumber, Vector2Int target, int sector)
        {
            var penalty = 0;
            foreach (var pair in reservationsByHero)
            {
                if (pair.Key == heroNumber)
                {
                    continue;
                }

                var reservation = pair.Value;
                var distance = GridDistance(target, reservation.TargetCell);
                if (distance == 0)
                {
                    penalty += SameTargetPenalty;
                }
                else if (distance <= 3)
                {
                    penalty += (4 - distance) * NearbyTargetPenalty;
                }

                if (sector == reservation.Sector)
                {
                    penalty += SameSectorPenalty;
                }
            }

            return penalty;
        }

        private int CalculateSector(Vector2Int target)
        {
            var dx = target.x - entrancePosition.x;
            var dy = target.y - entrancePosition.y;
            if (dx == 0 && dy == 0)
            {
                return 0;
            }

            var absX = Mathf.Abs(dx);
            var absY = Mathf.Abs(dy);
            if (absX >= absY * 2)
            {
                return dx > 0 ? 1 : 5;
            }

            if (absY >= absX * 2)
            {
                return dy > 0 ? 3 : 7;
            }

            if (dx > 0 && dy > 0)
            {
                return 2;
            }

            if (dx < 0 && dy > 0)
            {
                return 4;
            }

            return dx < 0 ? 6 : 8;
        }

        private void RemoveReservation(int heroNumber, Vector2Int target)
        {
            reservationsByHero.Remove(heroNumber);
            if (targetOwners.TryGetValue(target, out var owner) && owner == heroNumber)
            {
                targetOwners.Remove(target);
            }
        }

        private static int StableJitter(int heroNumber, Vector2Int origin, Vector2Int target)
        {
            unchecked
            {
                var hash = heroNumber * 73856093
                    ^ origin.x * 19349663
                    ^ origin.y * 83492791
                    ^ target.x * 265443576
                    ^ target.y * 1597334677;
                hash ^= hash >> 13;
                return Mathf.Abs(hash % 6);
            }
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private readonly struct Reservation
        {
            public Reservation(Vector2Int targetCell, Vector2Int approachCell, int sector)
            {
                TargetCell = targetCell;
                ApproachCell = approachCell;
                Sector = sector;
            }

            public Vector2Int TargetCell { get; }

            public Vector2Int ApproachCell { get; }

            public int Sector { get; }
        }
    }
}
