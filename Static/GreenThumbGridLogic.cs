using System;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.TerrainUtils;
namespace FishTacoGames
{
    public static class GreenThumbGridLogic
    {
        private static int S => GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal;
        private static float sizeW;
        private static float sizeL;
        private static float sizeH;
        public static Vector3 CS => new(sizeW / S, sizeH, sizeL / S);
        private static Vector3 worldPoint;
        private const int MaxQueueSize = 100;
        private static CallData callData = new();
        private struct CallData
        {
            public Vector3 position;
            public int distance;
        }
        private static readonly Queue<CallData> callQueue = new(MaxQueueSize);
        /// <summary>
        /// you will need to pass the current terrains size and positon to get the active cells
        /// you can quickly do this by calling the GetCurrentTerrain Method in the Manager instance or getting the x and y in the global terrain map
        /// </summary>
        /// <param name="hitPosition"></param>
        /// <param name="terrainSize"></param>
        /// <param name="terrainPosition"></param>
        /// <returns></returns>
        public static List<int> HandleCall(Vector3 hitPosition, Vector3 terrainSize, Vector3 terrainPosition)
        {
            List<int> activeCells = new();
            sizeW = terrainSize.x;
            sizeL = terrainSize.z;
            sizeH = terrainSize.y;
            worldPoint = terrainPosition;
            callData.position = hitPosition;
            if (callQueue.Count >= MaxQueueSize)
                callQueue.Dequeue();
            callQueue.Enqueue(callData);

            while (callQueue.Count > 0)
            {
                ProcessCell(cellIndex =>
                {
                    GreenThumbManager.Instance.greenThumbGlobal.activeCellID = cellIndex;

                    if (GreenThumbManager.Instance.greenThumbGlobal.extendPhysics)
                        activeCells = ProcessSurroundingDoubleCells(cellIndex);
                    else
                        activeCells = ProcessSurroundingCells(cellIndex);
                    activeCells.Add(cellIndex);
                });
                callQueue.Dequeue();
            }
            return activeCells;
        }
        private static void ProcessCell(Action<int> cellIndexCallback) =>
        cellIndexCallback?.Invoke(Mathf.FloorToInt((callData.position.z - worldPoint.z) / (sizeW / S)) +
                    Mathf.FloorToInt((callData.position.x - worldPoint.x) / (sizeL / S)) * S);
        public static int GetCurrentCell(Vector3 hitPosition, Vector3 terrainSize, Vector3 terrainPosition)
        {
            sizeW = terrainSize.x;
            sizeL = terrainSize.z;
            sizeH = terrainSize.y;
            worldPoint = terrainPosition;
            callData.position = hitPosition;
            return Mathf.FloorToInt((callData.position.z - worldPoint.z) / (sizeW / S)) +
                    Mathf.FloorToInt((callData.position.x - worldPoint.x) / (sizeL / S)) * S;
        }
        private static List<int> ProcessSurroundingCells(int cellIndex)
        {
            List<int> cellIndices = new();
            for (int dx = -1; dx <= 1; dx++) // do some grid dancing       
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int neighborIndex = cellIndex + dx + dz * S; // account for neighbouring
                    if (neighborIndex >= 0 && neighborIndex < S * S && Mathf.Abs(neighborIndex % S - cellIndex % S) <= 1 && neighborIndex != cellIndex)
                        cellIndices.Add(neighborIndex);
                }
            }

            return cellIndices;
        }
        private static List<int> ProcessSurroundingDoubleCells(int cellIndex)
        {
            List<int> cellIndices = new();
            for (int dx = -1; dx <= 1; dx++) // do some grid dancing
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int neighborIndex = cellIndex + dx + dz * S; // account for neighbouring
                    if (neighborIndex >= 0 && neighborIndex < S * S && Mathf.Abs(neighborIndex % S - cellIndex % S) <= 1 && neighborIndex != cellIndex)
                    {
                        cellIndices.Add(neighborIndex);
                        if (neighborIndex % S != 0 && (neighborIndex + 1) % S != 0 && neighborIndex >= S && neighborIndex < S * (S - 1))
                        {
                            for (int ddx = -1; ddx <= 1; ddx++) // do it again!
                            {
                                for (int ddz = -1; ddz <= 1; ddz++)
                                {
                                    int secondLayerNeighborIndex = neighborIndex + ddx + ddz * S;
                                    if (secondLayerNeighborIndex >= 0 && secondLayerNeighborIndex < S * S && secondLayerNeighborIndex != neighborIndex && secondLayerNeighborIndex != cellIndex)
                                        cellIndices.Add(secondLayerNeighborIndex);
                                }
                            }
                        }
                    }
                }
            }
            return cellIndices;
        }
    }
}
