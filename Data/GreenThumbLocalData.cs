using System.Collections.Generic;
using UnityEngine;

namespace FishTacoGames
{
    public class GreenThumbLocalData : ScriptableObject
    {
        public TerrainData terraindata1;
        public Vector3 terrainPosition;
        public Vector3 terrainSize;
        public Vector3 cellSize;
        public int[] layers;
        public int activeCell = 0;
        [HideInInspector] // 0 based index will show 9999 if 10k
        public int totalTreeIndexCount = 0;

        public List<GreenThumbCellProfile> GreenCellProfiles;
    }
}