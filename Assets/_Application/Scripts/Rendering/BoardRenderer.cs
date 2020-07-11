﻿using DG.Tweening;
using GMTK2020.Data;
using GMTK2020.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GMTK2020.Rendering
{
    public class BoardRenderer : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera = null;
        [SerializeField] private TileData tileData = null;
        [SerializeField] private StepRenderer[] stepRenderers = null;
        [SerializeField] private Button retryButton = null;
        [SerializeField] private Button nextButton = null;

        [SerializeField] private float postMatchDelay = 0.25f;
        [SerializeField] private float postFallDelay = 0.1f;

        private Dictionary<Tile, TileRenderer> tileDictionary = new Dictionary<Tile, TileRenderer>();
        private Tile[,] initialGrid;
        int width;
        int height;

        public void RenderInitial(Tile[,] grid)
        {
            initialGrid = grid;

            width = grid.GetLength(0);
            height = grid.GetLength(1);

            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)
                {
                    Tile tile = grid[x, y];
                    TileRenderer tileRenderer = Instantiate(tileData.PrefabMap[tile.Color], transform);

                    tileRenderer.transform.localPosition = new Vector3(x, y, 0);
                    tileDictionary[tile] = tileRenderer;
                }
        }

        public async void KickOffRenderSimulation(Simulation simulation, int correctPredictions)
        {
            await RenderSimulationAsync(simulation, correctPredictions);
        }

        public async Task RenderSimulationAsync(Simulation simulation, int correctPredictions)
        {
            for (int i = 0; i < simulation.Steps.Count; ++i)
            {
                if (i > 0)
                    await new WaitForSeconds(postFallDelay);

                SimulationStep step = simulation.Steps[i];
                Sequence seq = DOTween.Sequence();

                if (i < correctPredictions)
                {
                    seq.Join(stepRenderers[i].ShowSuccess());
                }
                else
                {
                    seq.Join(stepRenderers[i].ShowFailure());
                }

                foreach (Tile tile in step.MatchedTiles)
                {
                    // TODO: indicate incorrect guesses
                    TileRenderer tileRenderer = tileDictionary[tile];
                    var spriteRenderer = tileRenderer.GetComponent<SpriteRenderer>();
                    seq.Insert(0, spriteRenderer.DOFade(0.0f, 0.25f));
                    seq.AppendCallback(() => Destroy(tileRenderer.gameObject));
                }

                await CompletionOf(seq);

                await new WaitForSeconds(postMatchDelay);

                if (i >= correctPredictions)
                    break;

                seq = DOTween.Sequence();

                foreach ((Tile tile, Vector2Int newPosition) in step.MovingTiles)
                {
                    TileRenderer tileRenderer = tileDictionary[tile];
                    seq.Join(tileRenderer.transform.DOLocalMove(new Vector3Int(newPosition.x, newPosition.y, 0), 0.75f));
                }
                
                await CompletionOf(seq);
            }

            if (correctPredictions < Simulator.MAX_SIMULATION_STEPS)
            {
                retryButton.gameObject.SetActive(true);
            }
            else
            {
                nextButton.gameObject.SetActive(true);
            }
        }

        System.Collections.IEnumerator CompletionOf(Tween tween)
        {
            yield return tween.WaitForCompletion();
        }

        public Vector2Int? PixelSpaceToGridCoordinates(Vector3 mousePosition)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePosition);
            Vector3 localPos = worldPos - transform.position;

            var gridPos = new Vector2Int(Mathf.RoundToInt(localPos.x), Mathf.RoundToInt(localPos.y));

            if (gridPos.x < 0 || gridPos.y < 0 || gridPos.x >= width || gridPos.y >= width)
                return null;

            return gridPos;
        }

        public void UpdatePrediction(Vector2Int pos, int value)
        {
            Tile tile = initialGrid[pos.x, pos.y];
            tileDictionary[tile].UpdatePrediction(value);
        }
    }
}
