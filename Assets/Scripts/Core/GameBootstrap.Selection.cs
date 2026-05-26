using Labyrinth.Hero;
using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private const float SelectionMarkerYOffset = 0.12f;

        private MobController selectedMob;
        private Transform mapSelectionMarker;
        private Material mapSelectionMarkerMaterial;
        private float mapSelectionMarkerCellSize;

        private bool TrySelectHeroOrMobFromHit(RaycastHit hit)
        {
            var heroView = hit.collider.GetComponentInParent<HeroView>();
            if (heroView != null && heroView.Controller != null && heroView.Controller.Model != null)
            {
                SelectHeroFromMap(heroView.Controller);
                return true;
            }

            var mobView = hit.collider.GetComponentInParent<MobView>();
            if (mobView != null && mobView.Controller != null)
            {
                SelectMobFromMap(mobView.Controller);
                return true;
            }

            return false;
        }

        private void SelectHeroFromMap(HeroController hero)
        {
            baseHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            mobHud.Hide();
            SelectHero(hero);
            heroHud.ShowSelectedPanel();
            RefreshMapSelectionMarker();
            GameAudioController.PlayUi(GameSfx.HudClick, 0.75f);
        }

        private void SelectMobFromMap(MobController mob)
        {
            if (selectedHero != null)
            {
                selectedHero.SetSelected(false);
                selectedHero = null;
            }

            selectedMob = mob;
            baseHud.Hide();
            heroHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            mobHud.Show(mob);
            RefreshSelectedHeroVisibility();
            RefreshMapSelectionMarker();
            GameAudioController.PlayUi(GameSfx.HudClick, 0.75f);
        }

        private void ClearSelectedMob()
        {
            selectedMob = null;
            RefreshMapSelectionMarker();
        }

        private void RefreshMapSelectionMarker()
        {
            if (currentMaze == null || mazeRenderer == null)
            {
                HideMapSelectionMarker();
                return;
            }

            if (selectedMob != null && selectedMob.Model != null && selectedMob.Model.IsAlive)
            {
                ShowMapSelectionMarker(selectedMob.Position, new Color(1f, 0.32f, 0.18f));
                return;
            }

            if (selectedHero != null && selectedHero.Model != null && selectedHero.Model.IsAlive)
            {
                ShowMapSelectionMarker(selectedHero.Model.Position, new Color(1f, 0.86f, 0.24f));
                return;
            }

            HideMapSelectionMarker();
        }

        private void ShowMapSelectionMarker(Vector2Int cell, Color color)
        {
            EnsureMapSelectionMarker();
            mapSelectionMarker.gameObject.SetActive(true);
            mapSelectionMarker.position = mazeRenderer.GridToWorld(cell) + new Vector3(0f, mazeRenderer.CellSize * SelectionMarkerYOffset, 0f);
            SetMapSelectionMarkerColor(color);
        }

        private void HideMapSelectionMarker()
        {
            if (mapSelectionMarker != null)
            {
                mapSelectionMarker.gameObject.SetActive(false);
            }
        }

        private void EnsureMapSelectionMarker()
        {
            if (mapSelectionMarker != null && Mathf.Approximately(mapSelectionMarkerCellSize, mazeRenderer.CellSize))
            {
                return;
            }

            if (mapSelectionMarker != null)
            {
                Destroy(mapSelectionMarker.gameObject);
            }

            mapSelectionMarkerCellSize = mazeRenderer.CellSize;
            mapSelectionMarkerMaterial = CreateMapSelectionMaterial();
            mapSelectionMarker = new GameObject("Map Cell Selection Marker").transform;
            mapSelectionMarker.SetParent(transform, false);
            var unit = mazeRenderer.CellSize;
            CreateSelectionEdge("Selection North", new Vector3(0f, 0f, unit * 0.48f), new Vector3(unit * 0.98f, unit * 0.035f, unit * 0.055f));
            CreateSelectionEdge("Selection South", new Vector3(0f, 0f, unit * -0.48f), new Vector3(unit * 0.98f, unit * 0.035f, unit * 0.055f));
            CreateSelectionEdge("Selection East", new Vector3(unit * 0.48f, 0f, 0f), new Vector3(unit * 0.055f, unit * 0.035f, unit * 0.98f));
            CreateSelectionEdge("Selection West", new Vector3(unit * -0.48f, 0f, 0f), new Vector3(unit * 0.055f, unit * 0.035f, unit * 0.98f));
        }

        private void CreateSelectionEdge(string name, Vector3 localPosition, Vector3 localScale)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = name;
            edge.transform.SetParent(mapSelectionMarker, false);
            edge.transform.localPosition = localPosition;
            edge.transform.localScale = localScale;
            edge.GetComponent<Renderer>().sharedMaterial = mapSelectionMarkerMaterial;
            var collider = edge.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void SetMapSelectionMarkerColor(Color color)
        {
            if (mapSelectionMarkerMaterial == null)
            {
                return;
            }

            mapSelectionMarkerMaterial.color = color;
            if (mapSelectionMarkerMaterial.HasProperty("_BaseColor"))
            {
                mapSelectionMarkerMaterial.SetColor("_BaseColor", color);
            }

            if (mapSelectionMarkerMaterial.HasProperty("_Color"))
            {
                mapSelectionMarkerMaterial.SetColor("_Color", color);
            }
        }

        private static Material CreateMapSelectionMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return new Material(shader) { name = "Map Cell Selection" };
        }
    }
}
