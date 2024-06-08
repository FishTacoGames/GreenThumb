using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace FishTacoGames
{
    public class GreenThumbDataManager : Editor
    {
        private static readonly string GlobalDataPath = "Assets/FishTacoGames/GreenThumb/Saving/GreenThumbGlobalData.asset";
        private static readonly string LocalDataPath = "Assets/FishTacoGames/GreenThumb/Saving/GreenThumbLocalData";

        public static GreenThumbGlobalData globalData;
        public static List<GreenThumbLocalData> LocalDataList;

        [MenuItem("FishTacoGames/GreenThumb/Create Data")]
        public static void CreateDataManager()
        {
            CreateIfNull();
            AssignToScene();
        }
        private static void CreateIfNull()
        {
            LoadGlobalData();
            CheckGlobalData();
            if (globalData != null)
            {
                globalData.terrainCount = Terrain.activeTerrains.Length;
                LoadAllLocalData();
                CheckLocalData();
            }
        }
        private static void AssignToScene()
        {
            GreenThumbManager.Instance.greenThumbGlobal = globalData;
            GreenThumbManager.Instance.m_player = FindFirstObjectByType<Camera>().transform;
            // get our scene manager instance
            // assign the global data asset to it.
            // get each terrain in the scene 
            // add a GreenThumbCellManager to it
            for (int i = 0; i < LocalDataList.Count; i++)
            {
                if (!Terrain.activeTerrains[i].TryGetComponent<GreenThumbCellManager>(out var cache))
                {
                    cache = Terrain.activeTerrains[i].AddComponent<GreenThumbCellManager>();
                }
                cache.greenThumbLocal = LocalDataList[i];
                cache.RefreshData();
            }
        }
        private void OnEnable()
        {
            CreateIfNull();
        }
        #region Saving
        private static void CheckGlobalData()
        {
            if (globalData == null)
            {
                globalData = CreateInstance<GreenThumbGlobalData>();
                AssetDatabase.CreateAsset(globalData, GlobalDataPath);
                AssetDatabase.SaveAssetIfDirty(globalData);
            }
            else
                LoadGlobalData();
        }
        private static void LoadGlobalData()
        {
            globalData = AssetDatabase.LoadAssetAtPath<GreenThumbGlobalData>(GlobalDataPath);
        }

        private static void SaveGlobalData()
        {
            AssetDatabase.SaveAssetIfDirty(globalData);
        }

        private static void SaveLocalData()
        {
            for (int i = 0; i < LocalDataList.Count; i++)
            {
                AssetDatabase.SaveAssetIfDirty(LocalDataList[i]);
            }
        }

        private static void LoadAllLocalData()
        {
            LocalDataList ??= new List<GreenThumbLocalData>();
            for (int i = 0; i < globalData.terrainCount; i++)
            {
                string path = LocalDataPath + "_" + i + ".asset";
                GreenThumbLocalData localData = AssetDatabase.LoadAssetAtPath<GreenThumbLocalData>(path);
                if (localData != null)
                {
                    // If data exists, update it with terrain info
                    localData.terraindata1 = Terrain.activeTerrains[i].terrainData;
                    localData.terrainPosition = Terrain.activeTerrains[i].transform.position;
                    LocalDataList.Add(localData);
                }
                else
                {
                    // If data doesn't exist, create new and assign terrain info
                    localData = CreateInstance<GreenThumbLocalData>();
                    localData.terraindata1 = Terrain.activeTerrains[i].terrainData;
                    localData.terrainPosition = Terrain.activeTerrains[i].transform.position;
                    AssetDatabase.CreateAsset(localData, path);
                    LocalDataList.Add(localData);
                }
            }
        }

        private static void LoadSingleLocalData(int TerrainIndex)
        {
            string path = LocalDataPath + "_" + TerrainIndex + ".asset";
            LocalDataList[TerrainIndex] = AssetDatabase.LoadAssetAtPath<GreenThumbLocalData>(path);
        }

        private static void CheckLocalData()
        {

            if (LocalDataList.Count != globalData.terrainCount)
            {
                // Delete excess local data
                for (int i = globalData.terrainCount; i < LocalDataList.Count; i++)
                {
                    string path = LocalDataPath + "_" + i + ".asset";
                    AssetDatabase.DeleteAsset(path);
                }
                // Create missing local data
                for (int i = LocalDataList.Count; i < globalData.terrainCount; i++)
                {
                    GreenThumbLocalData localData = CreateInstance<GreenThumbLocalData>();
                    string path = LocalDataPath + "_" + i + ".asset";
                    AssetDatabase.CreateAsset(localData, path);
                    AssetDatabase.SaveAssetIfDirty(localData);
                    LocalDataList.Add(localData);
                    LocalDataList[i].terraindata1 = Terrain.activeTerrains[i].terrainData;
                    LocalDataList[i].terrainPosition = Terrain.activeTerrains[i].transform.position;
                }
            }
        }
        #endregion Saving
    }
}