using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishTacoGames
{
    public class GreenThumbGlobalData : ScriptableObject
    {
        public int terrainCount;
        public List<MappingList> replacementLists;
        [Serializable]
        public struct CategoryIDWrapper
        {
            public string Name;
            public int Index;

            public CategoryIDWrapper(string str, int num)
            {
                Name = str;
                Index = num;
            }
        }
        [Serializable]
        public struct PrefabMapping
        {
            public bool doEffect;
            public string prototypeName;
            public GameObject replacement;
        }
        [Serializable]
        public struct MappingList
        {
            public CategoryIDWrapper categoryID;
            public List<PrefabMapping> mappings;
        }

        // persistant variables
        [Range(1, 10)]
        public int sizeOfPatchesGlobal = 1;

        [Range(1, 5)]
        public int renderPhysicsDistanceGlobal = 2;
        public bool extendPhysics = false;
        public bool disableCells = true;
        public int poolSizeGlobal = 20;
        public bool usePooling = true;

        public int gridSizesGlobal = 4;

        public bool debuggingGlobal = false;

        public bool generateMeshForCells = true;

        public int activePoolCount = 0;
        public int activeCellID;
        public bool PoolExists => activePoolCount > 0;
    }
}