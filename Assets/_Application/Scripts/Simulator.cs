﻿using GMTK2020.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace GMTK2020
{
    public class Simulator
    {
        private readonly List<HashSet<Vector2Int>> matchingPatterns;
        private readonly Random rng;

        public Simulator(HashSet<Vector2Int> matchingPattern, Random rng)
        {
            this.rng = rng;
            matchingPatterns = new List<HashSet<Vector2Int>>() { matchingPattern };

            for (int i = 0; i < 3; ++i)
            {
                matchingPattern = RotatePattern(matchingPattern);
                matchingPatterns.Add(matchingPattern);
            }

            matchingPattern = MirrorPattern(matchingPattern);
            matchingPatterns.Add(matchingPattern);

            for (int i = 0; i < 3; ++i)
            {
                matchingPattern = RotatePattern(matchingPattern);
                matchingPatterns.Add(matchingPattern);
            }
        }

        public Simulation Simulate(Tile[,] initialGrid, Prediction prediction = null)
        {
            var simulationSteps = new List<SimulationStep>();

            Tile[,] workingGrid = initialGrid.Clone() as Tile[,];
            HashSet<Tile> remainingPredictions = prediction is null ? null : new HashSet<Tile>(prediction.PredictedTiles);

            List<(Tile, Vector2Int)> movingTiles;
            List<(Tile, Vector2Int)> newTiles;
            while (true)
            {
                HashSet<(Tile, Vector2Int)> matchedTiles = RemoveMatchedTiles(workingGrid, remainingPredictions);
                if (matchedTiles.Count == 0)
                {
                    break;
                }

                movingTiles = MoveTilesDown(workingGrid);

                newTiles = FillWithNewTiles(workingGrid, movingTiles);

                simulationSteps.Add(new SimulationStep(matchedTiles, movingTiles, newTiles));
            }

            int clearedRows = Mathf.Clamp(simulationSteps.Count - 1, 0, workingGrid.GetLength(1));

            if (remainingPredictions != null)
                foreach (Tile tile in remainingPredictions)
                    tile.Petrify();

            List<(Tile, Vector2Int)> clearedTiles = ClearBottomRows(workingGrid, clearedRows);
            movingTiles = MoveTilesDown(workingGrid);
            newTiles = FillWithNewTiles(workingGrid, movingTiles);

            var clearBoardStep = new ClearBoardStep(clearedRows, clearedTiles, movingTiles, newTiles);

            return new Simulation(simulationSteps, remainingPredictions?.ToList() ?? new List<Tile>(), clearBoardStep);
        }

        public HashSet<(Tile, Vector2Int)> RemoveMatchedTiles(Tile[,] workingGrid, HashSet<Tile> remainingPredictions)
        {
            var matchedTiles = new HashSet<(Tile, Vector2Int)>();

            int width = workingGrid.GetLength(0);
            int height = workingGrid.GetLength(1);

            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)
                {
                    Tile tile = workingGrid[x, y];
                    if (tile is null || tile.IsStone || remainingPredictions != null && !remainingPredictions.Contains(tile))
                        continue;

                    Vector2Int origin = new Vector2Int(x, y);

                    bool isMatch = false;
                    foreach (HashSet<Vector2Int> pattern in matchingPatterns)
                    {
                        bool doesThisSymmetryMatch = true;
                        foreach (Vector2Int offset in pattern)
                        {
                            if (offset == Vector2Int.zero)
                                continue;

                            Vector2Int pos = origin + offset;
                            if (pos.x < 0
                                || pos.y < 0
                                || pos.x >= width
                                || pos.y >= height
                                || remainingPredictions != null && !remainingPredictions.Contains(workingGrid[pos.x, pos.y])
                                || workingGrid[pos.x, pos.y]?.Color != tile.Color)
                            {
                                doesThisSymmetryMatch = false;
                                break;
                            }
                        }

                        if (doesThisSymmetryMatch)
                        {
                            isMatch = true;
                            foreach (Vector2Int offset in pattern)
                            {
                                Vector2Int pos = origin + offset;
                                matchedTiles.Add((workingGrid[pos.x, pos.y], pos));
                            }
                            break;
                        }
                    }

                    if (!isMatch)
                        continue;
                }

            foreach ((Tile tile, Vector2Int pos) in matchedTiles)
            {
                workingGrid[pos.x, pos.y] = null;
                remainingPredictions?.Remove(tile);
            }

            return matchedTiles;
        }

        public List<(Tile, Vector2Int)> MoveTilesDown(Tile[,] workingGrid)
        {
            int width = workingGrid.GetLength(0);
            int height = workingGrid.GetLength(1);

            var movedTiles = new List<(Tile, Vector2Int)>();

            for (int x = 0; x < width; ++x)
            {
                int top = 0;
                for (int y = 0; y < height; ++y)
                {
                    Tile tile = workingGrid[x, y];
                    if (tile is null)
                        continue;

                    if (y > top)
                    {
                        movedTiles.Add((tile, new Vector2Int(x, top)));
                        workingGrid[x, top] = tile;
                        workingGrid[x, y] = null;
                    }

                    ++top;
                }
            }

            return movedTiles;
        }

        private List<(Tile, Vector2Int)> FillWithNewTiles(Tile[,] workingGrid, List<(Tile, Vector2Int)> movedTiles)
        {
            int width = workingGrid.GetLength(0);
            int height = workingGrid.GetLength(1);

            var newTiles = new List<(Tile, Vector2Int)>();

            for (int x = 0; x < width; ++x)
            {
                int nNewTiles = 0;
                for (int y = 0; y < height; ++y)
                {
                    Tile tile = workingGrid[x, y];
                    if (tile == null)
                        ++nNewTiles;
                }

                for (int y = height; y < height + nNewTiles; ++y)
                {
                    // TODO: Should this be the responsibility of the level generator?
                    Tile tile = workingGrid[x, y - nNewTiles] = new Tile(rng.Next(5));

                    newTiles.Add((tile, new Vector2Int(x, y)));
                    movedTiles.Add((tile, new Vector2Int(x, y - nNewTiles)));
                }
            }

            return newTiles;
        }

        private List<(Tile, Vector2Int)> ClearBottomRows(Tile[,] workingGrid, int clearedRows)
        {
            int width = workingGrid.GetLength(0);

            var clearedTiles = new List<(Tile, Vector2Int)>();

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < clearedRows; ++y)
                {
                    Tile tile = workingGrid[x, y];

                    clearedTiles.Add((tile, new Vector2Int(x, y)));
                    workingGrid[x, y] = null;
                }
            }

            return clearedTiles;
        }

        private HashSet<Vector2Int> MirrorPattern(HashSet<Vector2Int> pattern)
        {
            return new HashSet<Vector2Int>(pattern.Select((vec2) =>
            {
                (int x, int y) = vec2;
                return new Vector2Int(x, -y);
            }));
        }

        private HashSet<Vector2Int> RotatePattern(HashSet<Vector2Int> pattern)
        {
            return new HashSet<Vector2Int>(pattern.Select((vec2) =>
            {
                (int x, int y) = vec2;
                return new Vector2Int(y, -x);
            }));
        }
    }
}