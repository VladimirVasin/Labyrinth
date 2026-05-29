using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Maze
{
    public sealed partial class TerrainDecorationController : MonoBehaviour
    {
        private const float GroundOffset = 0.03f;
        private const float WaterHeight = 0.026f;
        private const int PlacementAttemptMultiplier = 18;
        private const string RiverStraightHorizontalTexturePath = "Textures/Terrain/river_straight_horizontal";
        private const string RiverStraightVerticalTexturePath = "Textures/Terrain/river_straight_vertical";
        private const string RiverCornerNorthWestTexturePath = "Textures/Terrain/river_corner_nw";
        private const string RiverCornerNorthEastTexturePath = "Textures/Terrain/river_corner_ne";
        private const string RiverCornerSouthEastTexturePath = "Textures/Terrain/river_corner_se";
        private const string RiverCornerSouthWestTexturePath = "Textures/Terrain/river_corner_sw";
        private const float RiverFlowSpeed = 0.12f;

        private readonly List<DecorationRuntime> decorations = new List<DecorationRuntime>();
        private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> riverCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> bridgeCells = new HashSet<Vector2Int>();

        private MazeGenerationResult result;
        private MazeRenderer mazeRenderer;
        private BaseDevelopment baseDevelopment;
        private Transform root;
        private Material trunkMaterial;
        private Material leafMaterial;
        private Material darkLeafMaterial;
        private Material bushMaterial;
        private Material rockMaterial;
        private Material riverStraightHorizontalMaterial;
        private Material riverStraightVerticalMaterial;
        private Material riverCornerNorthWestMaterial;
        private Material riverCornerNorthEastMaterial;
        private Material riverCornerSouthEastMaterial;
        private Material riverCornerSouthWestMaterial;
        private Material bankMaterial;
        private Material bridgeMaterial;
        private Material flowerMaterial;
        private Material riverFlowMaterial;
        private Mesh riverTileMesh;
        private Mesh riverFlowMesh;
        private Texture2D riverFlowTexture;
        private Vector2 riverFlowOffset;

        private void Update()
        {
            if (riverFlowMaterial == null)
            {
                return;
            }

            riverFlowOffset.x = Mathf.Repeat(riverFlowOffset.x + Time.deltaTime * RiverFlowSpeed, 1f);
            riverFlowOffset.y = Mathf.Repeat(riverFlowOffset.y + Time.deltaTime * RiverFlowSpeed * 0.42f, 1f);
            riverFlowMaterial.mainTextureOffset = riverFlowOffset;
            if (riverFlowMaterial.HasProperty("_BaseMap"))
            {
                riverFlowMaterial.SetTextureOffset("_BaseMap", riverFlowOffset);
            }
        }

        public void Render(MazeGenerationResult generationResult, MazeRenderer renderer, BaseDevelopment development)
        {
            Clear();
            result = generationResult;
            mazeRenderer = renderer;
            baseDevelopment = development;
            if (result == null || result.Grid == null || mazeRenderer == null)
            {
                return;
            }

            EnsureMaterials();
            root = new GameObject("Terrain Decorations").transform;
            root.SetParent(transform, false);

            var random = new System.Random(CreateSeed(0x341a91));
            var outsideCells = CalculateOutsideCellCount();
            var riverCount = CalculateRiverCount(outsideCells);
            var riverSegments = 0;
            for (var i = 0; i < riverCount; i++)
            {
                riverSegments += CreateRiver(i, random);
            }

            var treeCount = Mathf.Clamp(outsideCells / 68, 18, 150);
            var bushCount = Mathf.Clamp(outsideCells / 58, 24, 170);
            var rockCount = Mathf.Clamp(outsideCells / 150, 10, 60);
            var flowerPatchCount = Mathf.Clamp(outsideCells / 126, 12, 90);

            CreateNature(treeCount, bushCount, rockCount, flowerPatchCount, random);
            GameDebugLog.Info(
                "Terrain",
                $"Decorations generated: outsideCells={outsideCells}, rivers={riverCount}, riverCells={riverSegments}, terrainHills=mesh, trees={treeCount}, bushes={bushCount}, rocks={rockCount}, flowers={flowerPatchCount}.");
        }

        public void Clear()
        {
            for (var i = decorations.Count - 1; i >= 0; i--)
            {
                decorations[i].Destroy();
            }

            decorations.Clear();
            occupiedCells.Clear();
            riverCells.Clear();
            bridgeCells.Clear();

            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }

            result = null;
            mazeRenderer = null;
            baseDevelopment = null;
        }

        public void ClearAround(Vector2Int center, int radius)
        {
            if (radius < 0)
            {
                return;
            }

            for (var i = decorations.Count - 1; i >= 0; i--)
            {
                var decoration = decorations[i];
                if (decoration.Kind == DecorationKind.Bridge)
                {
                    continue;
                }

                if (ChebyshevDistance(center, decoration.Position) > radius + decoration.Clearance)
                {
                    continue;
                }

                decoration.Destroy();
                decorations.RemoveAt(i);
                occupiedCells.Remove(decoration.Position);
            }
        }

        public void RegisterRoadSegment(Vector2Int from, Vector2Int to)
        {
            if (root == null || mazeRenderer == null)
            {
                return;
            }

            var horizontal = from.x != to.x;
            TryCreateBridge(from, horizontal);
            TryCreateBridge(to, horizontal);
        }

        public bool BlocksBuilding(Vector2Int position, int footprintRadius)
        {
            if (riverCells.Count == 0)
            {
                return false;
            }

            var blockedRadius = Mathf.Max(0, footprintRadius) + 1;
            foreach (var riverCell in riverCells)
            {
                if (ChebyshevDistance(position, riverCell) <= blockedRadius)
                {
                    return true;
                }
            }

            return false;
        }

        public bool BlocksCityWalker(Vector2Int cell)
        {
            return riverCells.Contains(cell) && !bridgeCells.Contains(cell);
        }

        private int CreateRiver(int riverIndex, System.Random random)
        {
            var side = riverIndex == 0 ? GetBaseSide() : GetOppositeSide(GetBaseSide());
            var cells = riverIndex == 0
                ? BuildCastleBypassRiver(side)
                : BuildEdgeRiver(side, random);

            var created = 0;
            for (var i = 0; i < cells.Count; i++)
            {
                if (!CanPlaceRiver(cells[i]))
                {
                    continue;
                }

                var previous = i > 0 ? cells[i - 1] : cells[i];
                var next = i < cells.Count - 1 ? cells[i + 1] : cells[i];
                CreateRiverCell(cells[i], previous, next);
                created++;
            }

            return created;
        }

        private List<Vector2Int> BuildCastleBypassRiver(TerrainSide side)
        {
            var minX = -MazeTerrain.PaddingCells + 2;
            var maxX = result.Grid.Width - 1 + MazeTerrain.PaddingCells - 2;
            var minY = -MazeTerrain.PaddingCells + 2;
            var maxY = result.Grid.Height - 1 + MazeTerrain.PaddingCells - 2;
            var castleRadius = BaseDevelopment.CastleFootprintRadiusCells;
            var route = new List<Vector2Int>();

            switch (side)
            {
                case TerrainSide.Left:
                {
                    var wallX = -2;
                    var outerX = Mathf.Clamp(result.BasePosition.x - castleRadius - 4, minX, -3);
                    var lowerY = Mathf.Clamp(result.BasePosition.y - castleRadius - 3, minY + 2, maxY - 6);
                    var upperY = Mathf.Clamp(result.BasePosition.y + castleRadius + 4, lowerY + 4, maxY - 1);
                    AddPathSegment(route, new Vector2Int(wallX, minY));
                    AddPathSegment(route, new Vector2Int(wallX, lowerY));
                    AddPathSegment(route, new Vector2Int(outerX, lowerY));
                    AddPathSegment(route, new Vector2Int(outerX, upperY));
                    AddPathSegment(route, new Vector2Int(wallX, upperY));
                    AddPathSegment(route, new Vector2Int(wallX, maxY));
                    break;
                }

                case TerrainSide.Right:
                {
                    var wallX = result.Grid.Width + 1;
                    var outerX = Mathf.Clamp(result.BasePosition.x + castleRadius + 4, result.Grid.Width + 2, maxX);
                    var lowerY = Mathf.Clamp(result.BasePosition.y - castleRadius - 3, minY + 2, maxY - 6);
                    var upperY = Mathf.Clamp(result.BasePosition.y + castleRadius + 4, lowerY + 4, maxY - 1);
                    AddPathSegment(route, new Vector2Int(wallX, minY));
                    AddPathSegment(route, new Vector2Int(wallX, lowerY));
                    AddPathSegment(route, new Vector2Int(outerX, lowerY));
                    AddPathSegment(route, new Vector2Int(outerX, upperY));
                    AddPathSegment(route, new Vector2Int(wallX, upperY));
                    AddPathSegment(route, new Vector2Int(wallX, maxY));
                    break;
                }

                case TerrainSide.Bottom:
                {
                    var wallY = -2;
                    var outerY = Mathf.Clamp(result.BasePosition.y - castleRadius - 4, minY, -3);
                    var leftX = Mathf.Clamp(result.BasePosition.x - castleRadius - 3, minX + 2, maxX - 6);
                    var rightX = Mathf.Clamp(result.BasePosition.x + castleRadius + 4, leftX + 4, maxX - 1);
                    AddPathSegment(route, new Vector2Int(minX, wallY));
                    AddPathSegment(route, new Vector2Int(leftX, wallY));
                    AddPathSegment(route, new Vector2Int(leftX, outerY));
                    AddPathSegment(route, new Vector2Int(rightX, outerY));
                    AddPathSegment(route, new Vector2Int(rightX, wallY));
                    AddPathSegment(route, new Vector2Int(maxX, wallY));
                    break;
                }

                case TerrainSide.Top:
                default:
                {
                    var wallY = result.Grid.Height + 1;
                    var outerY = Mathf.Clamp(result.BasePosition.y + castleRadius + 4, result.Grid.Height + 2, maxY);
                    var leftX = Mathf.Clamp(result.BasePosition.x - castleRadius - 3, minX + 2, maxX - 6);
                    var rightX = Mathf.Clamp(result.BasePosition.x + castleRadius + 4, leftX + 4, maxX - 1);
                    AddPathSegment(route, new Vector2Int(minX, wallY));
                    AddPathSegment(route, new Vector2Int(leftX, wallY));
                    AddPathSegment(route, new Vector2Int(leftX, outerY));
                    AddPathSegment(route, new Vector2Int(rightX, outerY));
                    AddPathSegment(route, new Vector2Int(rightX, wallY));
                    AddPathSegment(route, new Vector2Int(maxX, wallY));
                    break;
                }
            }

            return route;
        }

        private List<Vector2Int> BuildEdgeRiver(TerrainSide side, System.Random random)
        {
            return side == TerrainSide.Top || side == TerrainSide.Bottom
                ? BuildHorizontalRiver(side, random)
                : BuildVerticalRiver(side, random);
        }

        private List<Vector2Int> BuildVerticalRiver(TerrainSide side, System.Random random)
        {
            var cells = new List<Vector2Int>();
            var x = side == TerrainSide.Left ? -2 : result.Grid.Width + 1;
            var minY = -MazeTerrain.PaddingCells + 2;
            var maxY = result.Grid.Height - 1 + MazeTerrain.PaddingCells - 2;
            var drift = 0;
            for (var y = minY; y <= maxY; y++)
            {
                if (random.NextDouble() < 0.18d)
                {
                    drift += random.Next(0, 2) == 0 ? -1 : 1;
                    drift = Mathf.Clamp(drift, -2, 2);
                }

                var cellX = side == TerrainSide.Left
                    ? Mathf.Min(-2, x - Mathf.Abs(drift))
                    : Mathf.Max(result.Grid.Width + 1, x + Mathf.Abs(drift));
                cells.Add(new Vector2Int(cellX, y));
            }

            return cells;
        }

        private static void AddPathSegment(List<Vector2Int> route, Vector2Int target)
        {
            if (route.Count == 0)
            {
                route.Add(target);
                return;
            }

            var current = route[route.Count - 1];
            while (current != target)
            {
                var stepX = target.x == current.x ? 0 : (target.x > current.x ? 1 : -1);
                var stepY = stepX != 0 || target.y == current.y ? 0 : (target.y > current.y ? 1 : -1);
                current += new Vector2Int(stepX, stepY);
                route.Add(current);
            }
        }

        private List<Vector2Int> BuildHorizontalRiver(TerrainSide side, System.Random random)
        {
            var cells = new List<Vector2Int>();
            var y = side == TerrainSide.Bottom ? -2 : result.Grid.Height + 1;
            var minX = -MazeTerrain.PaddingCells + 2;
            var maxX = result.Grid.Width - 1 + MazeTerrain.PaddingCells - 2;
            var drift = 0;
            for (var x = minX; x <= maxX; x++)
            {
                if (random.NextDouble() < 0.18d)
                {
                    drift += random.Next(0, 2) == 0 ? -1 : 1;
                    drift = Mathf.Clamp(drift, -2, 2);
                }

                var cellY = side == TerrainSide.Bottom
                    ? Mathf.Min(-2, y - Mathf.Abs(drift))
                    : Mathf.Max(result.Grid.Height + 1, y + Mathf.Abs(drift));
                cells.Add(new Vector2Int(x, cellY));
            }

            return cells;
        }

        private void CreateRiverCell(Vector2Int cell, Vector2Int previous, Vector2Int next)
        {
            if (!riverCells.Add(cell))
            {
                return;
            }

            occupiedCells.Add(cell);
            var cellSize = mazeRenderer.CellSize;
            var center = mazeRenderer.GridToWorld(cell) + Vector3.up * GroundOffset;
            var riverRoot = new GameObject($"River {cell.x},{cell.y}");
            riverRoot.transform.SetParent(root, false);
            riverRoot.transform.position = center;

            var incoming = NormalizeDirection(cell - previous);
            var outgoing = NormalizeDirection(next - cell);
            if (incoming == Vector2Int.zero)
            {
                incoming = outgoing;
            }

            if (outgoing == Vector2Int.zero)
            {
                outgoing = incoming;
            }

            var isCorner = incoming != outgoing && !AreOpposite(incoming, outgoing);
            if (isCorner)
            {
                var previousDirection = Opposite(incoming);
                CreateRiverTile(riverRoot.transform, cellSize, GetCornerRiverMaterial(previousDirection, outgoing));
                CreateCornerFlow(riverRoot.transform, cellSize, previousDirection, outgoing);
            }
            else
            {
                var horizontal = Mathf.Abs(outgoing.x) > 0 || Mathf.Abs(incoming.x) > 0;
                CreateRiverTile(riverRoot.transform, cellSize, horizontal ? riverStraightHorizontalMaterial : riverStraightVerticalMaterial);
                CreateStraightFlow(riverRoot.transform, cellSize, horizontal);
            }

            decorations.Add(new DecorationRuntime(riverRoot, cell, DecorationKind.River, 0));
        }

        private void CreateRiverTile(Transform riverRoot, float cellSize, Material material)
        {
            var tile = new GameObject("River Tile");
            tile.transform.SetParent(riverRoot, false);
            tile.transform.localPosition = new Vector3(0f, WaterHeight * 0.55f, 0f);
            tile.transform.localScale = new Vector3(cellSize * 1.08f, 1f, cellSize * 1.08f);
            tile.AddComponent<MeshFilter>().sharedMesh = EnsureRiverTileMesh();
            tile.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void CreateStraightFlow(Transform riverRoot, float cellSize, bool horizontal)
        {
            var scale = horizontal
                ? new Vector3(cellSize * 0.96f, 1f, cellSize * 0.44f)
                : new Vector3(cellSize * 0.44f, 1f, cellSize * 0.96f);
            CreateFlowQuad("River Flow Straight", riverRoot, Vector3.zero, scale);
        }

        private void CreateCornerFlow(Transform riverRoot, float cellSize, Vector2Int firstDirection, Vector2Int secondDirection)
        {
            CreateFlowArm(riverRoot, cellSize, firstDirection);
            CreateFlowArm(riverRoot, cellSize, secondDirection);
            CreateFlowQuad("River Flow Corner Pool", riverRoot, Vector3.zero, new Vector3(cellSize * 0.5f, 1f, cellSize * 0.5f));
        }

        private void CreateFlowArm(Transform riverRoot, float cellSize, Vector2Int direction)
        {
            var offset = new Vector3(direction.x * cellSize * 0.28f, 0f, direction.y * cellSize * 0.28f);
            var scale = direction.x != 0
                ? new Vector3(cellSize * 0.58f, 1f, cellSize * 0.42f)
                : new Vector3(cellSize * 0.42f, 1f, cellSize * 0.58f);
            CreateFlowQuad("River Flow Corner Arm", riverRoot, offset, scale);
        }

        private void CreateFlowQuad(string name, Transform parent, Vector3 localOffset, Vector3 localScale)
        {
            if (riverFlowMaterial == null)
            {
                return;
            }

            var flow = new GameObject(name);
            flow.transform.SetParent(parent, false);
            flow.transform.localPosition = localOffset + new Vector3(0f, WaterHeight * 0.9f, 0f);
            flow.transform.localScale = localScale;
            flow.AddComponent<MeshFilter>().sharedMesh = EnsureRiverFlowMesh();
            var renderer = flow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = riverFlowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private Material GetCornerRiverMaterial(Vector2Int firstDirection, Vector2Int secondDirection)
        {
            var north = firstDirection == Vector2Int.up || secondDirection == Vector2Int.up;
            var south = firstDirection == Vector2Int.down || secondDirection == Vector2Int.down;
            var east = firstDirection == Vector2Int.right || secondDirection == Vector2Int.right;
            var west = firstDirection == Vector2Int.left || secondDirection == Vector2Int.left;
            if (north && west) return riverCornerNorthWestMaterial;
            if (north && east) return riverCornerNorthEastMaterial;
            if (south && east) return riverCornerSouthEastMaterial;
            if (south && west) return riverCornerSouthWestMaterial;
            return riverStraightHorizontalMaterial;
        }

        private static Vector2Int NormalizeDirection(Vector2Int direction)
        {
            if (direction.x > 0) return Vector2Int.right;
            if (direction.x < 0) return Vector2Int.left;
            if (direction.y > 0) return Vector2Int.up;
            if (direction.y < 0) return Vector2Int.down;
            return Vector2Int.zero;
        }

        private static bool AreOpposite(Vector2Int a, Vector2Int b)
        {
            return a.x + b.x == 0 && a.y + b.y == 0;
        }

        private static Vector2Int Opposite(Vector2Int direction)
        {
            return new Vector2Int(-direction.x, -direction.y);
        }

        private void TryCreateBridge(Vector2Int cell, bool horizontal)
        {
            if (!riverCells.Contains(cell) || !bridgeCells.Add(cell))
            {
                return;
            }

            var cellSize = mazeRenderer.CellSize;
            var unit = mazeRenderer.ModelUnitSize;
            var center = mazeRenderer.GridToWorld(cell) + Vector3.up * (GroundOffset + WaterHeight + unit * 0.08f);
            var bridgeRoot = new GameObject($"Bridge {cell.x},{cell.y}");
            bridgeRoot.transform.SetParent(root, false);
            bridgeRoot.transform.position = center;
            var deckScale = horizontal
                ? new Vector3(cellSize * 1.34f, unit * 0.1f, cellSize * 0.52f)
                : new Vector3(cellSize * 0.52f, unit * 0.1f, cellSize * 1.34f);
            CreatePart("Bridge Deck", PrimitiveType.Cube, bridgeRoot.transform, Vector3.zero, deckScale, bridgeMaterial);

            var plankCount = 4;
            for (var i = 0; i < plankCount; i++)
            {
                var offset = (i - 1.5f) * unit * 0.23f;
                var position = horizontal ? new Vector3(offset, unit * 0.08f, 0f) : new Vector3(0f, unit * 0.08f, offset);
                var scale = horizontal
                    ? new Vector3(unit * 0.055f, unit * 0.04f, cellSize * 0.56f)
                    : new Vector3(cellSize * 0.56f, unit * 0.04f, unit * 0.055f);
                CreatePart("Bridge Plank Line", PrimitiveType.Cube, bridgeRoot.transform, position, scale, bankMaterial);
            }

            var railOffset = cellSize * 0.33f;
            var railHeight = unit * 0.18f;
            if (horizontal)
            {
                CreatePart("Bridge Rail North", PrimitiveType.Cube, bridgeRoot.transform, new Vector3(0f, railHeight, railOffset), new Vector3(cellSize * 1.26f, unit * 0.1f, unit * 0.06f), bankMaterial);
                CreatePart("Bridge Rail South", PrimitiveType.Cube, bridgeRoot.transform, new Vector3(0f, railHeight, -railOffset), new Vector3(cellSize * 1.26f, unit * 0.1f, unit * 0.06f), bankMaterial);
            }
            else
            {
                CreatePart("Bridge Rail East", PrimitiveType.Cube, bridgeRoot.transform, new Vector3(railOffset, railHeight, 0f), new Vector3(unit * 0.06f, unit * 0.1f, cellSize * 1.26f), bankMaterial);
                CreatePart("Bridge Rail West", PrimitiveType.Cube, bridgeRoot.transform, new Vector3(-railOffset, railHeight, 0f), new Vector3(unit * 0.06f, unit * 0.1f, cellSize * 1.26f), bankMaterial);
            }

            decorations.Add(new DecorationRuntime(bridgeRoot, cell, DecorationKind.Bridge, 0));
        }

        private void CreateNature(int trees, int bushes, int rocks, int flowers, System.Random random)
        {
            for (var i = 0; i < trees; i++)
            {
                if (TryFindDecorPosition(random, 1, out var cell))
                {
                    CreateTree(cell, random);
                }
            }

            for (var i = 0; i < bushes; i++)
            {
                if (TryFindDecorPosition(random, 0, out var cell))
                {
                    CreateBush(cell, random);
                }
            }

            for (var i = 0; i < rocks; i++)
            {
                if (TryFindDecorPosition(random, 0, out var cell))
                {
                    CreateRock(cell, random);
                }
            }

            for (var i = 0; i < flowers; i++)
            {
                if (TryFindDecorPosition(random, 0, out var cell))
                {
                    CreateFlowerPatch(cell, random);
                }
            }
        }

        private void CreateTree(Vector2Int cell, System.Random random)
        {
            var unit = mazeRenderer.ModelUnitSize;
            var center = mazeRenderer.GridToWorld(cell) + Vector3.up * GroundOffset;
            var tree = new GameObject($"Tree {cell.x},{cell.y}");
            tree.transform.SetParent(root, false);
            tree.transform.position = center;
            tree.transform.rotation = Quaternion.Euler(0f, random.Next(0, 360), 0f);
            var pine = random.NextDouble() < 0.46d;
            CreatePart("Tree Trunk", PrimitiveType.Cylinder, tree.transform, new Vector3(0f, unit * 0.42f, 0f), new Vector3(unit * 0.18f, unit * 0.42f, unit * 0.18f), trunkMaterial);
            if (pine)
            {
                CreatePart("Pine Crown Low", PrimitiveType.Cube, tree.transform, new Vector3(0f, unit * 0.95f, 0f), new Vector3(unit * 0.9f, unit * 0.68f, unit * 0.9f), darkLeafMaterial);
                CreatePart("Pine Crown High", PrimitiveType.Cube, tree.transform, new Vector3(0f, unit * 1.42f, 0f), new Vector3(unit * 0.58f, unit * 0.55f, unit * 0.58f), leafMaterial);
            }
            else
            {
                CreatePart("Leaf Crown A", PrimitiveType.Sphere, tree.transform, new Vector3(0f, unit * 1.15f, 0f), Vector3.one * unit * 0.86f, leafMaterial);
                CreatePart("Leaf Crown B", PrimitiveType.Sphere, tree.transform, new Vector3(unit * 0.28f, unit * 1.03f, unit * 0.08f), Vector3.one * unit * 0.56f, darkLeafMaterial);
            }

            decorations.Add(new DecorationRuntime(tree, cell, DecorationKind.Nature, 1));
            MarkOccupied(cell, 1);
        }

        private void CreateBush(Vector2Int cell, System.Random random)
        {
            var unit = mazeRenderer.ModelUnitSize;
            var bush = new GameObject($"Bush {cell.x},{cell.y}");
            bush.transform.SetParent(root, false);
            bush.transform.position = mazeRenderer.GridToWorld(cell) + Vector3.up * GroundOffset;
            CreatePart("Bush A", PrimitiveType.Sphere, bush.transform, new Vector3(-unit * 0.13f, unit * 0.18f, 0f), Vector3.one * unit * RandomRange(random, 0.36f, 0.5f), bushMaterial);
            CreatePart("Bush B", PrimitiveType.Sphere, bush.transform, new Vector3(unit * 0.16f, unit * 0.15f, unit * 0.08f), Vector3.one * unit * RandomRange(random, 0.28f, 0.42f), darkLeafMaterial);
            decorations.Add(new DecorationRuntime(bush, cell, DecorationKind.Nature, 0));
            occupiedCells.Add(cell);
        }

        private void CreateRock(Vector2Int cell, System.Random random)
        {
            var unit = mazeRenderer.ModelUnitSize;
            var rock = new GameObject($"Rock {cell.x},{cell.y}");
            rock.transform.SetParent(root, false);
            rock.transform.position = mazeRenderer.GridToWorld(cell) + Vector3.up * GroundOffset;
            rock.transform.rotation = Quaternion.Euler(0f, random.Next(0, 360), 0f);
            CreatePart("Rock", PrimitiveType.Sphere, rock.transform, new Vector3(0f, unit * 0.12f, 0f), new Vector3(unit * RandomRange(random, 0.42f, 0.7f), unit * RandomRange(random, 0.2f, 0.36f), unit * RandomRange(random, 0.36f, 0.62f)), rockMaterial);
            decorations.Add(new DecorationRuntime(rock, cell, DecorationKind.Nature, 0));
            occupiedCells.Add(cell);
        }

        private void CreateFlowerPatch(Vector2Int cell, System.Random random)
        {
            var unit = mazeRenderer.ModelUnitSize;
            var patch = new GameObject($"Flowers {cell.x},{cell.y}");
            patch.transform.SetParent(root, false);
            patch.transform.position = mazeRenderer.GridToWorld(cell) + Vector3.up * (GroundOffset + 0.012f);
            var count = random.Next(3, 7);
            for (var i = 0; i < count; i++)
            {
                var offset = new Vector3(RandomRange(random, -0.42f, 0.42f) * unit, 0f, RandomRange(random, -0.42f, 0.42f) * unit);
                CreatePart("Flower Dot", PrimitiveType.Cube, patch.transform, offset, new Vector3(unit * 0.08f, unit * 0.035f, unit * 0.08f), flowerMaterial);
            }

            decorations.Add(new DecorationRuntime(patch, cell, DecorationKind.Nature, 0));
            occupiedCells.Add(cell);
        }

        private bool TryFindDecorPosition(System.Random random, int clearance, out Vector2Int cell)
        {
            var attempts = Mathf.Max(20, PlacementAttemptMultiplier * (clearance + 1));
            for (var i = 0; i < attempts; i++)
            {
                cell = RandomTerrainCell(random);
                if (CanPlaceNature(cell, clearance))
                {
                    return true;
                }
            }

            cell = Vector2Int.zero;
            return false;
        }

        private bool CanPlaceNature(Vector2Int cell, int clearance)
        {
            if (!IsOutsideTerrainCell(cell) || riverCells.Contains(cell))
            {
                return false;
            }

            for (var x = cell.x - clearance; x <= cell.x + clearance; x++)
            {
                for (var y = cell.y - clearance; y <= cell.y + clearance; y++)
                {
                    var check = new Vector2Int(x, y);
                    if (!IsOutsideTerrainCell(check) || occupiedCells.Contains(check) || IsReserved(check, 1))
                    {
                        return false;
                    }
                }
            }

            return !IsNearMazeWall(cell, clearance);
        }

        private bool CanPlaceRiver(Vector2Int cell)
        {
            return IsOutsideTerrainCell(cell)
                && !riverCells.Contains(cell)
                && !IsNearMazeWall(cell, 0)
                && !IsRiverReserved(cell);
        }

        private bool IsRiverReserved(Vector2Int cell)
        {
            return ChebyshevDistance(cell, result.BasePosition) <= BaseDevelopment.CastleFootprintRadiusCells
                || IsInsideExistingBuilding(cell, 0);
        }

        private bool IsReserved(Vector2Int cell, int padding)
        {
            if (ChebyshevDistance(cell, result.BasePosition) <= BaseDevelopment.CastleFootprintRadiusCells + padding)
            {
                return true;
            }

            if (ChebyshevDistance(cell, result.EntrancePosition) <= padding)
            {
                return true;
            }

            if (IsOnEntranceRoad(cell, padding))
            {
                return true;
            }

            return IsInsideExistingBuilding(cell, padding);
        }

        private bool IsInsideExistingBuilding(Vector2Int cell, int padding)
        {
            if (baseDevelopment == null)
            {
                return false;
            }

            foreach (var position in baseDevelopment.FarmPositions)
            {
                if (ChebyshevDistance(cell, position) <= BaseDevelopment.FarmFootprintRadiusCells + padding) return true;
            }

            foreach (var position in baseDevelopment.LumberjackCampPositions)
            {
                if (ChebyshevDistance(cell, position) <= BaseDevelopment.LumberjackCampFootprintRadiusCells + padding) return true;
            }

            foreach (var position in baseDevelopment.HeroHousePositions)
            {
                if (ChebyshevDistance(cell, position) <= BaseDevelopment.HeroHouseFootprintRadiusCells + padding) return true;
            }

            foreach (var position in baseDevelopment.PeasantHutPositions)
            {
                if (ChebyshevDistance(cell, position) <= BaseDevelopment.PeasantHutFootprintRadiusCells + padding) return true;
            }

            return IsServiceBuilding(cell, padding);
        }

        private bool IsServiceBuilding(Vector2Int cell, int padding)
        {
            return (baseDevelopment.HasAlchemistShop && ChebyshevDistance(cell, baseDevelopment.AlchemistShopPosition) <= BaseDevelopment.AlchemistShopFootprintRadiusCells + padding)
                || (baseDevelopment.HasTavern && ChebyshevDistance(cell, baseDevelopment.TavernPosition) <= BaseDevelopment.TavernFootprintRadiusCells + padding)
                || (baseDevelopment.HasForge && ChebyshevDistance(cell, baseDevelopment.ForgePosition) <= BaseDevelopment.ForgeFootprintRadiusCells + padding)
                || (baseDevelopment.HasInfirmary && ChebyshevDistance(cell, baseDevelopment.InfirmaryPosition) <= BaseDevelopment.InfirmaryFootprintRadiusCells + padding)
                || (baseDevelopment.HasCartographerHouse && ChebyshevDistance(cell, baseDevelopment.CartographerHousePosition) <= BaseDevelopment.CartographerHouseFootprintRadiusCells + padding)
                || (baseDevelopment.HasChapel && ChebyshevDistance(cell, baseDevelopment.ChapelPosition) <= BaseDevelopment.ChapelFootprintRadiusCells + padding)
                || (baseDevelopment.HasMinersGuild && ChebyshevDistance(cell, baseDevelopment.MinersGuildPosition) <= BaseDevelopment.MinersGuildFootprintRadiusCells + padding)
                || (baseDevelopment.HasMarket && ChebyshevDistance(cell, baseDevelopment.MarketPosition) <= BaseDevelopment.MarketFootprintRadiusCells + padding);
        }

        private bool IsOnEntranceRoad(Vector2Int cell, int padding)
        {
            var minX = Mathf.Min(result.BasePosition.x, result.EntrancePosition.x) - padding;
            var maxX = Mathf.Max(result.BasePosition.x, result.EntrancePosition.x) + padding;
            var minY = Mathf.Min(result.BasePosition.y, result.EntrancePosition.y) - padding;
            var maxY = Mathf.Max(result.BasePosition.y, result.EntrancePosition.y) + padding;
            return cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY;
        }

        private bool IsOutsideTerrainCell(Vector2Int cell)
        {
            return cell.x >= -MazeTerrain.PaddingCells
                && cell.y >= -MazeTerrain.PaddingCells
                && cell.x <= result.Grid.Width - 1 + MazeTerrain.PaddingCells
                && cell.y <= result.Grid.Height - 1 + MazeTerrain.PaddingCells
                && !result.Grid.InBounds(cell);
        }

        private bool IsNearMazeWall(Vector2Int cell, int padding)
        {
            return cell.x >= -1 - padding
                && cell.x <= result.Grid.Width + padding
                && cell.y >= -1 - padding
                && cell.y <= result.Grid.Height + padding;
        }

        private Vector2Int RandomTerrainCell(System.Random random)
        {
            var min = -MazeTerrain.PaddingCells;
            var maxX = result.Grid.Width - 1 + MazeTerrain.PaddingCells;
            var maxY = result.Grid.Height - 1 + MazeTerrain.PaddingCells;
            return new Vector2Int(random.Next(min, maxX + 1), random.Next(min, maxY + 1));
        }

        private void MarkOccupied(Vector2Int center, int radius)
        {
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var y = center.y - radius; y <= center.y + radius; y++)
                {
                    occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }

        private int CalculateOutsideCellCount()
        {
            var width = result.Grid.Width + MazeTerrain.PaddingCells * 2;
            var height = result.Grid.Height + MazeTerrain.PaddingCells * 2;
            return width * height - result.Grid.Width * result.Grid.Height;
        }

        private int CalculateRiverCount(int outsideCells)
        {
            if (outsideCells < 1600)
            {
                return 1;
            }

            return result.Grid.Width * result.Grid.Height >= 4200 ? 2 : 1;
        }

        private TerrainSide GetBaseSide()
        {
            if (result.BasePosition.x < 0)
            {
                return TerrainSide.Left;
            }

            if (result.BasePosition.x >= result.Grid.Width)
            {
                return TerrainSide.Right;
            }

            return result.BasePosition.y < 0 ? TerrainSide.Bottom : TerrainSide.Top;
        }

        private static TerrainSide GetOppositeSide(TerrainSide side)
        {
            switch (side)
            {
                case TerrainSide.Left:
                    return TerrainSide.Right;
                case TerrainSide.Right:
                    return TerrainSide.Left;
                case TerrainSide.Bottom:
                    return TerrainSide.Top;
                case TerrainSide.Top:
                default:
                    return TerrainSide.Bottom;
            }
        }

        private GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(type, name));
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(part);
            VoxelVisuals.ApplyBlockStyle(part, type, material, false);
            return part;
        }

        private Mesh EnsureRiverTileMesh()
        {
            if (riverTileMesh != null)
            {
                return riverTileMesh;
            }

            riverTileMesh = new Mesh
            {
                name = "Terrain River Tile Mesh",
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, 0.5f)
                },
                triangles = new[] { 0, 1, 2, 2, 1, 3 },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f)
                }
            };
            riverTileMesh.RecalculateNormals();
            riverTileMesh.RecalculateBounds();
            return riverTileMesh;
        }

        private Mesh EnsureRiverFlowMesh()
        {
            if (riverFlowMesh != null)
            {
                return riverFlowMesh;
            }

            riverFlowMesh = new Mesh
            {
                name = "Terrain River Flow Mesh",
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, 0.5f)
                },
                triangles = new[] { 0, 1, 2, 2, 1, 3 },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1.8f),
                    new Vector2(1.8f, 0f),
                    new Vector2(1.8f, 1.8f)
                }
            };
            riverFlowMesh.RecalculateNormals();
            riverFlowMesh.RecalculateBounds();
            return riverFlowMesh;
        }

        private int CreateSeed(int salt)
        {
            unchecked
            {
                return result.Settings.Seed
                    ^ (result.Grid.Width * 73856093)
                    ^ (result.Grid.Height * 19349663)
                    ^ salt;
            }
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

    }
}
