using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TerrainUtils;
using Random = System.Random;
using RangeAttribute = UnityEngine.RangeAttribute;

namespace FishTacoGames
{
    public class GreenThumbManager : MonoBehaviour
    {
        #region Internal
        public static GreenThumbManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                if (_instance == null)
                {
                    if (_instance = FindFirstObjectByType<GreenThumbManager>()) // couldnt find instance checking scene and remove duplicates
                    {
                        var instanceList = FindObjectsByType<GreenThumbManager>(FindObjectsSortMode.InstanceID);
                        if (instanceList != null)
                        {
                            foreach (var possibleInstance in instanceList)
                            {
                                if (_instance != possibleInstance)
                                    Destroy(possibleInstance);
                            }
                        }
                    }
                    else // was null
                    {
                        GameObject singletonObject = new("TerrainPrefabManager");
                        _instance = singletonObject.AddComponent<GreenThumbManager>();
                    }
                }
                return _instance;
            }
        }
        private static GreenThumbManager _instance;
        private void Awake()
        {
            // Ensure only one instance exists
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            var terrains = FindFirstObjectByType<Terrain>();
            TerrainMap = TerrainMap.CreateFromConnectedNeighbors(terrains);
        }
        [SerializeField]
        private Transform Player;
        public Transform m_player
        {
            get { return Player; }
            set { Player = value; }
        }
        #endregion Internal
        public TerrainMap TerrainMap;
        public GreenThumbGlobalData greenThumbGlobal;
        public int terrainsampleIndex;
        public int cellSampleIndex;
        public bool RemoveIfNoReplacement = true;
        #region Misc
        public string GetLetters() => GetRandomLetters(25);
        private static string GetRandomLetters(int count, bool isCapitalized = true)
        {
            Random random = new();
            string allLetters = isCapitalized ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "abcdefghijklmnopqrstuvwxyz";
            char[] shuffledLetters = allLetters.OrderBy(x => random.Next()).ToArray();
            string result = new(shuffledLetters.Take(count).ToArray());
            return result;
        }
        private void OnDrawGizmosSelected()
        {
            if (greenThumbGlobal.terrainCount == 0)
                return;
            var cache = Terrain.activeTerrains[terrainsampleIndex].GetComponent<GreenThumbCellManager>();
            if (cache == null || cache.greenThumbLocal.GreenCellProfiles == null || cache.greenThumbLocal.GreenCellProfiles.Count == 0)
                return;
            if (cellSampleIndex < cache.greenThumbLocal.GreenCellProfiles.Count)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(cache.greenThumbLocal.GreenCellProfiles[cellSampleIndex].cellBounds.center, cache.greenThumbLocal.GreenCellProfiles[cellSampleIndex].cellBounds.size);
            }
            else
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < cache.greenThumbLocal.GreenCellProfiles.Count - 1; i++)
                {
                    Gizmos.DrawWireCube(cache.greenThumbLocal.GreenCellProfiles[i].cellBounds.center, cache.greenThumbLocal.GreenCellProfiles[i].cellBounds.size);
                }
            }
        }
        #endregion Misc
        /// <summary>
        /// check for null
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Transform GetCurrentTerrain(Vector3 position)
        {
            float closestDistance = 10000;
            Transform closestT = null;
            foreach (var terrain in TerrainMap.terrainTiles)
            {
                if (Vector3.Distance(terrain.Value.transform.position, position) < closestDistance)
                {
                    closestT = terrain.Value.transform;
                    closestDistance = Vector3.Distance(terrain.Value.transform.position, position);
                }
            }
            return closestT;
        }
        #region TreeStats

        public GameObject GetTreeReplacement(string tID, int lID)
        {
            if (greenThumbGlobal.usePooling && greenThumbGlobal.PoolExists)
            {
                GetFirstReplacementFromPool(lID, tID);
            }
            if (greenThumbGlobal.replacementLists == null || lID == -1)
                return null;
            foreach (var GO in greenThumbGlobal.replacementLists[lID].mappings)
            {
                if (GO.replacement == null) continue;
                if (GO.prototypeName == tID) return GO.replacement;
            }
            return null;
        }
        public bool GetTreeRules(string tID)
        {
            if (greenThumbGlobal.replacementLists == null || greenThumbGlobal.replacementLists.Count <= 0) return false;
            foreach (var list in greenThumbGlobal.replacementLists)
            {
                if (list.mappings == null || list.mappings.Count == 0) continue;
                foreach (var GO in list.mappings)
                    if (GO.prototypeName == tID) return GO.doEffect;
            }
            return false;
        }
        public (string, int) GetTreeCategory(string tID)
        {
            foreach (var map in greenThumbGlobal.replacementLists)
            {
                if (map.mappings == null || map.mappings.Count == 0) continue;
                foreach (var treeRules in map.mappings)
                    if (treeRules.prototypeName == tID)
                        return (map.categoryID.Name, map.categoryID.Index);
            }
            return ("", -1);
        }
        #endregion TreeStats

        #region Pooling
        public void CreateReplacmentPool()
        {
            DestroyPool();
            if (greenThumbGlobal.replacementLists == null || greenThumbGlobal.replacementLists.Count == 0)
                return;
            for (int i = 0; i < greenThumbGlobal.replacementLists.Count; i++)
            {
                var list = greenThumbGlobal.replacementLists[i];
                if (list.mappings.Count == 0 || list.mappings[0].replacement == null)
                {
                    continue;
                }

                GameObject categoryParent = new(list.categoryID.Name);
                categoryParent.transform.SetParent(transform);
                int categoryIndex = 0;
                for (int j = 0; j < greenThumbGlobal.poolSizeGlobal; j++)
                {
                    GameObject poolMember = Instantiate(list.mappings[categoryIndex].replacement, categoryParent.transform);
                    poolMember.name = list.mappings[categoryIndex].replacement.name + " " + j;
                    poolMember.AddComponent<GreenThumbTreePoolable>();
                    poolMember.SetActive(false);
                    greenThumbGlobal.activePoolCount++;
                    categoryIndex++;
                    if (categoryIndex >= list.mappings.Count)
                    {
                        categoryIndex = 0;
                    }
                }
            }
        }
        public void DestroyPool()
        {
            for (int i = 0; i < greenThumbGlobal.activePoolCount; i++)
            {
                if (transform.childCount == 0)
                    break;
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            greenThumbGlobal.activePoolCount = 0;
        }
        public GameObject GetFirstReplacementFromPool(int categoryIndex, string name)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (categoryIndex == i && transform.GetChild(i).childCount > 0)
                {
                    for (int j = 0; j < transform.GetChild(i).childCount; j++)
                    {
                        if (j == transform.GetChild(i).childCount - 1)
                        {
                            j = 0;
                            transform.GetChild(i).GetChild(j).GetComponent<GreenThumbTreePoolable>().ReturnToPool();
                            transform.GetChild(i).GetChild(j).gameObject.SetActive(false);
                        }
                        if (transform.GetChild(i).GetChild(j).gameObject.activeSelf)
                        {
                            continue;
                        }
                        else if (transform.GetChild(i).GetChild(j).name.Contains(name))
                        {
                            if (transform.GetChild(i).GetChild(j).TryGetComponent<Rigidbody>(out var rb))
                            {
                                rb.ResetInertiaTensor();
                                rb.isKinematic = true;
                                rb.isKinematic = false;
                            }
                            transform.GetChild(i).GetChild(j).gameObject.SetActive(true);
                            transform.GetChild(i).GetChild(j).GetComponent<GreenThumbTreePoolable>().LeavePool(180);
                            return transform.GetChild(i).GetChild(j).gameObject;
                        }
                    }
                }
            }
            return null;
        }

        #endregion Pooling

        #region Replacement
        public struct TreeReaction
        {
            public bool doEffects;
            public string category;
            public string Name;
            public Vector3 Position;
            public Transform Transform;
            public int ProtoID;

            public TreeReaction(bool doEffect, string item1, string treeName, Vector3 treePosition, Transform transform, int protoID)
            {
                doEffects = doEffect;
                category = item1;
                Name = treeName;
                Position = treePosition;
                Transform = transform;
                ProtoID = protoID;
            }
        }
        private int instanceID;
        public bool TryDestroyTree(out TreeReaction treeReaction, ControllerColliderHit hit, Transform Controller, int cellIndex = -1, bool RemoveDetailAtBase = false)
        {
            treeReaction = new();
            var terrainData2 = GetCurrentTerrain(hit.point).GetComponent<GreenThumbCellManager>();
            instanceID = -1;
            if (!hit.collider.CompareTag("GreenThumb"))
            {
                return false;
            }
            if (cellIndex == -1)
            {
                cellIndex = GreenThumbGridLogic.GetCurrentCell(hit.transform.position, terrainData2.greenThumbLocal.terrainSize, terrainData2.greenThumbLocal.terrainPosition);
            }
            instanceID = terrainData2.GetTreeInstanceIDFast(hit.transform);
            if (instanceID == -1)
            {
                if (hit.collider.gameObject.CompareTag("GreenThumb"))
                    Destroy(hit.collider.gameObject);
                return false;
            }

            var instance = terrainData2.greenThumbLocal.terraindata1.GetTreeInstance(instanceID);
            if (instance.heightScale == 0f)
            {
                if (hit.collider.gameObject.CompareTag("GreenThumb"))
                    Destroy(hit.collider.gameObject);
                return false;
            }
            string treeName = terrainData2.greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.name;
            var category = GetTreeCategory(treeName);
            var treePrefab = greenThumbGlobal.PoolExists ? GetFirstReplacementFromPool(category.Item2, treeName) : GetTreeReplacement(treeName, category.Item2);
            if (treePrefab != null)
            {
                Quaternion treeRotation = Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0);
                Vector3 treePosition = Vector3.Scale(instance.position, terrainData2.greenThumbLocal.terraindata1.size) + terrainData2.greenThumbLocal.terrainPosition;
                float treeScaleXZ = instance.widthScale;

                treePrefab.transform.localScale = new(treeScaleXZ, instance.heightScale, treeScaleXZ);
                instance.heightScale = 0f;
                instance.widthScale = 0f;
                if (RemoveDetailAtBase && terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length > 0)
                {
                    if (TryRemoveTerrainDetailPatch(GetComponent<TerrainCollider>(), treePosition, greenThumbGlobal.sizeOfPatchesGlobal, terrainData2, true, greenThumbGlobal.debuggingGlobal))
                    {
                        // TODO detail remove callback
                    }
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Contains(instanceID))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Remove(instanceID);
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Contains(hit.transform.position))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Remove(hit.transform.position);
                }
                terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(instanceID, instance);
                if (greenThumbGlobal.PoolExists)
                {
                    treePrefab.transform.SetPositionAndRotation(treePosition, treeRotation);
                }
                else
                    Instantiate(treePrefab, treePosition, treeRotation, terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].cellMesh);
                Destroy(hit.collider.gameObject);

                if (greenThumbGlobal.debuggingGlobal)
                    Debug.Log("Replaced terrain tree: " + treeName + " at position " + treePosition + " With " + treePrefab + " category was " + category.Item1 + " in cell id " + cellIndex + " tree id was " + instanceID);

                treeReaction = new TreeReaction(GetTreeRules(terrainData2.greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.name), category.Item1, treeName, treePosition, treePrefab.transform, instance.prototypeIndex);
                return true;
            }
            else if (RemoveIfNoReplacement && category.Item2 != -1)
            {
                Vector3 treePosition = Vector3.Scale(instance.position, terrainData2.greenThumbLocal.terraindata1.size) + terrainData2.greenThumbLocal.terrainPosition;
                instance.heightScale = 0f;
                instance.widthScale = 0f;
                if (RemoveDetailAtBase && terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length > 0)
                {
                    if (TryRemoveTerrainDetailPatch(GetComponent<TerrainCollider>(), treePosition, greenThumbGlobal.sizeOfPatchesGlobal, terrainData2, true, greenThumbGlobal.debuggingGlobal))
                    {
                        // TODO detail remove callback
                    }
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Contains(instanceID))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Remove(instanceID);
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Contains(hit.transform.position))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Remove(hit.transform.position);
                }
                terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(instanceID, instance);
                Destroy(hit.collider.gameObject);
            }

            if (greenThumbGlobal.debuggingGlobal)
                Debug.Log("prefab was null");

            return false;
        }
        public bool TryDestroyTree(out TreeReaction treeReaction, RaycastHit hit, Transform Controller, int cellIndex = -1, bool RemoveDetailAtBase = false)
        {
            treeReaction = new();
            var terrainData2 = GetCurrentTerrain(hit.point).GetComponent<GreenThumbCellManager>();
            instanceID = -1;
            if (!hit.collider.CompareTag("GreenThumb"))
            {
                return false;
            }
            if (cellIndex == -1)
            {
                cellIndex = GreenThumbGridLogic.GetCurrentCell(hit.transform.position, terrainData2.greenThumbLocal.terrainSize, terrainData2.greenThumbLocal.terrainPosition);
            }
            instanceID = terrainData2.GetTreeInstanceIDFast(hit.transform);
            if (instanceID == -1)
            {
                if (hit.collider.gameObject.CompareTag("GreenThumb"))
                    Destroy(hit.collider.gameObject);
                return false;
            }

            var instance = terrainData2.greenThumbLocal.terraindata1.GetTreeInstance(instanceID);
            if (instance.heightScale == 0f)
            {
                if (hit.collider.gameObject.CompareTag("GreenThumb"))
                    Destroy(hit.collider.gameObject);
                return false;
            }
            string treeName = terrainData2.greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.name;
            var category = GetTreeCategory(treeName);
            var treePrefab = greenThumbGlobal.PoolExists ? GetFirstReplacementFromPool(category.Item2, treeName) : GetTreeReplacement(treeName, category.Item2);
            if (treePrefab != null)
            {
                Quaternion treeRotation = Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0);
                Vector3 treePosition = Vector3.Scale(instance.position, terrainData2.greenThumbLocal.terraindata1.size) + terrainData2.greenThumbLocal.terrainPosition;
                float treeScaleXZ = instance.widthScale;

                treePrefab.transform.localScale = new(treeScaleXZ, instance.heightScale, treeScaleXZ);
                instance.heightScale = 0f;
                instance.widthScale = 0f;
                if (RemoveDetailAtBase && terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length > 0)
                {
                    if (TryRemoveTerrainDetailPatch(GetComponent<TerrainCollider>(), treePosition, greenThumbGlobal.sizeOfPatchesGlobal, terrainData2, true, greenThumbGlobal.debuggingGlobal))
                    {
                        // TODO detail remove callback
                    }
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Contains(instanceID))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Remove(instanceID);
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Contains(hit.transform.position))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Remove(hit.transform.position);
                }
                terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(instanceID, instance);
                if (greenThumbGlobal.PoolExists)
                {
                    treePrefab.transform.SetPositionAndRotation(treePosition, treeRotation);
                }
                else
                    Instantiate(treePrefab, treePosition, treeRotation, terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].cellMesh);
                Destroy(hit.collider.gameObject);

                if (greenThumbGlobal.debuggingGlobal)
                    Debug.Log("Replaced terrain tree: " + treeName + " at position " + treePosition + " With " + treePrefab + " category was " + category.Item1 + " in cell id " + cellIndex + " tree id was " + instanceID);

                treeReaction = new TreeReaction(GetTreeRules(terrainData2.greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.name), category.Item1, treeName, treePosition, treePrefab.transform, instance.prototypeIndex);
                return true;
            }
            else if (RemoveIfNoReplacement && category.Item2 != -1)
            {
                Vector3 treePosition = Vector3.Scale(instance.position, terrainData2.greenThumbLocal.terraindata1.size) + terrainData2.greenThumbLocal.terrainPosition;
                instance.heightScale = 0f;
                instance.widthScale = 0f;
                if (RemoveDetailAtBase && terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length > 0)
                {
                    if (TryRemoveTerrainDetailPatch(GetComponent<TerrainCollider>(), treePosition, greenThumbGlobal.sizeOfPatchesGlobal, terrainData2, true, greenThumbGlobal.debuggingGlobal))
                    {
                        // TODO detail remove callback
                    }
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Contains(instanceID))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Remove(instanceID);
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Contains(hit.transform.position))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Remove(hit.transform.position);
                }
                terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(instanceID, instance);
                Destroy(hit.collider.gameObject);
            }

            if (greenThumbGlobal.debuggingGlobal)
                Debug.Log("prefab was null");

            return false;
        }
        public bool TryDestroyTree(out TreeReaction treeReaction, Collision hit, Transform Controller, int cellIndex = -1, bool RemoveDetailAtBase = false)
        {
            treeReaction = new();
            var terrainData2 = GetCurrentTerrain(hit.transform.position).GetComponent<GreenThumbCellManager>();
            instanceID = -1;
            if (!hit.collider.CompareTag("GreenThumb"))
            {
                return false;
            }
            if (cellIndex == -1)
            {
                cellIndex = GreenThumbGridLogic.GetCurrentCell(hit.transform.position, terrainData2.greenThumbLocal.terrainSize, terrainData2.greenThumbLocal.terrainPosition);
            }
            instanceID = terrainData2.GetTreeInstanceIDFast(hit.transform);
            if (instanceID == -1)
            {
                if (hit.collider.gameObject.CompareTag("GreenThumb"))
                    Destroy(hit.collider.gameObject);
                return false;
            }

            var instance = terrainData2.greenThumbLocal.terraindata1.GetTreeInstance(instanceID);
            if (instance.heightScale == 0f)
            {
                if (hit.collider.gameObject.CompareTag("GreenThumb"))
                    Destroy(hit.collider.gameObject);
                return false;
            }
            string treeName = terrainData2.greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.name;
            var category = GetTreeCategory(treeName);
            var treePrefab = greenThumbGlobal.PoolExists ? GetFirstReplacementFromPool(category.Item2, treeName) : GetTreeReplacement(treeName, category.Item2);
            if (treePrefab != null)
            {
                Quaternion treeRotation = Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0);
                Vector3 treePosition = Vector3.Scale(instance.position, terrainData2.greenThumbLocal.terraindata1.size) + terrainData2.greenThumbLocal.terrainPosition;
                float treeScaleXZ = instance.widthScale;

                treePrefab.transform.localScale = new(treeScaleXZ, instance.heightScale, treeScaleXZ);
                instance.heightScale = 0f;
                instance.widthScale = 0f;
                if (RemoveDetailAtBase && terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length > 0)
                {
                    if (TryRemoveTerrainDetailPatch(GetComponent<TerrainCollider>(), treePosition, greenThumbGlobal.sizeOfPatchesGlobal, terrainData2, true, greenThumbGlobal.debuggingGlobal))
                    {
                        // TODO detail remove callback
                    }
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Contains(instanceID))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Remove(instanceID);
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Contains(hit.transform.position))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Remove(hit.transform.position);
                }
                terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(instanceID, instance);
                if (greenThumbGlobal.PoolExists)
                {
                    treePrefab.transform.SetPositionAndRotation(treePosition, treeRotation);
                }
                else
                    Instantiate(treePrefab, treePosition, treeRotation, terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].cellMesh);
                Destroy(hit.collider.gameObject);

                if (greenThumbGlobal.debuggingGlobal)
                    Debug.Log("Replaced terrain tree: " + treeName + " at position " + treePosition + " With " + treePrefab + " category was " + category.Item1 + " in cell id " + cellIndex + " tree id was " + instanceID);

                treeReaction = new TreeReaction(GetTreeRules(terrainData2.greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.name), category.Item1, treeName, treePosition, treePrefab.transform, instance.prototypeIndex);
                return true;
            }
            else if (RemoveIfNoReplacement && category.Item2 != -1)
            {
                Vector3 treePosition = Vector3.Scale(instance.position, terrainData2.greenThumbLocal.terraindata1.size) + terrainData2.greenThumbLocal.terrainPosition;
                instance.heightScale = 0f;
                instance.widthScale = 0f;
                if (RemoveDetailAtBase && terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length > 0)
                {
                    if (TryRemoveTerrainDetailPatch(GetComponent<TerrainCollider>(), treePosition, greenThumbGlobal.sizeOfPatchesGlobal, terrainData2, true, greenThumbGlobal.debuggingGlobal))
                    {
                        // TODO detail remove callback
                    }
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Contains(instanceID))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].indexOfPositionsList.Remove(instanceID);
                }
                if (terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Contains(hit.transform.position))
                {
                    terrainData2.greenThumbLocal.GreenCellProfiles[cellIndex].positionList.Remove(hit.transform.position);
                }
                terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(instanceID, instance);
                Destroy(hit.collider.gameObject);
            }

            if (greenThumbGlobal.debuggingGlobal)
                Debug.Log("prefab was null");

            return false;
        }
        public static bool TryRemoveTerrainDetailPatch(Collider collider, Vector3 hitpoint, int targetSize, GreenThumbCellManager terrainData2, bool Crunch, bool debugging = false, bool useGivenWorldPosition = false)
        {
            if (collider != null || terrainData2.greenThumbLocal.layers.Length == 0)
            {
                if (terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length == 0)
                    return false;

                Vector3 localPoint;
                // Convert the point to terrain local coordinates
                if (useGivenWorldPosition)
                {
                    localPoint = hitpoint;
                }
                else
                    localPoint = terrainData2.transform.InverseTransformPoint(hitpoint);
                if (debugging)
                    Debug.DrawLine(localPoint, terrainData2.transform.position, Color.red, 22);

                // Calculate the position within the terrain
                int terrainDetailX = Mathf.RoundToInt(localPoint.x / terrainData2.greenThumbLocal.terrainSize.x * terrainData2.greenThumbLocal.terraindata1.detailResolution);
                int terrainDetailY = Mathf.RoundToInt(localPoint.z / terrainData2.greenThumbLocal.terrainSize.z * terrainData2.greenThumbLocal.terraindata1.detailResolution);

                // Calculate the fractional part of the position within the terrain
                float fracX = localPoint.x / terrainData2.greenThumbLocal.terrainSize.x * terrainData2.greenThumbLocal.terraindata1.detailResolution - terrainDetailX;
                float fracY = localPoint.z / terrainData2.greenThumbLocal.terrainSize.z * terrainData2.greenThumbLocal.terraindata1.detailResolution - terrainDetailY;

                // Adjust the indices based on the fractional part
                if (fracX > 0.5f) terrainDetailX++;
                if (fracY > 0.5f) terrainDetailY++;

                bool allowItemToAdd = false;
                int[,] combinedDetailLayer = new int[1, 1]; // Combined detail layer changes for all layers
                for (int i = 0; i < terrainData2.greenThumbLocal.layers.Length; i++)
                {
                    int targetLayer = terrainData2.greenThumbLocal.layers[i];
                    int[,] detailLayer = terrainData2.greenThumbLocal.terraindata1.GetDetailLayer(terrainDetailX, terrainDetailY, targetSize, targetSize, targetLayer);
                    for (int y = 0; y < detailLayer.GetLength(1); y++)
                    {
                        for (int x = 0; x < detailLayer.GetLength(0); x++)
                        {
                            int trueDensity = detailLayer[x, y];
                            if (trueDensity != 0)
                            {
                                allowItemToAdd = true;
                                detailLayer[x, y] = 0;
                                if (debugging)
                                    Debug.Log("Layer " + targetLayer + " density:" + trueDensity);
                            }
                        }
                    }
                    // To combine changes from this layer with previous layers
                    // combinedDetailLayer = CombineDetailLayers(combinedDetailLayer, detailLayer);
                    if (Crunch)
                        combinedDetailLayer = CrunchDetailLayers(combinedDetailLayer.GetLength(0), combinedDetailLayer.GetLength(1));
                    else
                        combinedDetailLayer = CombineDetailLayers(combinedDetailLayer, detailLayer);
                    // Apply changes to the terrain for this layer
                    terrainData2.greenThumbLocal.terraindata1.SetDetailLayer(terrainDetailX, terrainDetailY, targetLayer, detailLayer);
                }
                if (debugging)
                {
                    // Visualize terrainDetailX and terrainDetailY
                    Vector3 detailPoint = new(
                        terrainDetailX * terrainData2.greenThumbLocal.terrainSize.x / terrainData2.greenThumbLocal.terraindata1.detailResolution, 0, terrainDetailY * terrainData2.greenThumbLocal.terrainSize.z / terrainData2.greenThumbLocal.terraindata1.detailResolution
                    );
                    detailPoint = terrainData2.transform.TransformPoint(detailPoint);
                    Debug.DrawLine(detailPoint - Vector3.right * 0.1f, detailPoint + Vector3.right * 0.1f, Color.green, 10f);
                    Debug.DrawLine(detailPoint - Vector3.forward * 0.1f, detailPoint + Vector3.forward * 0.1f, Color.green, 10f);
                }

                if (allowItemToAdd)
                {
                    if (debugging)
                        Debug.Log("Combined detail layers were set.");
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        private static int[,] CrunchDetailLayers(int width, int height)
        {
            int[,] clearedLayer = new int[width, height];
            // Set all elements to 0
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    clearedLayer[x, y] = 0;
                }
            }
            return clearedLayer;
        }
        private static int[,] CombineDetailLayers(int[,] existingLayer, int[,] newLayer)
        {
            int[,] combinedLayer = new int[existingLayer.GetLength(0), existingLayer.GetLength(1)];
            for (int y = 0; y < existingLayer.GetLength(1); y++)
            {
                for (int x = 0; x < existingLayer.GetLength(0); x++)
                {
                    // Combine densities from existing and new layers
                    combinedLayer[x, y] = existingLayer[x, y] + newLayer[x, y];
                }
            }
            return combinedLayer;
        }
        #endregion Replacement

        #region Creation
        public bool TrySetTerrainDetailPatchMaxDensity(Collider collider, Vector3 hitpoint, int targetSize, int targetLayer, GreenThumbCellManager terrainData2, bool debugging = false, bool useGivenWorldPosition = false)
        {
            if (collider != null)
            {
                if (terrainData2.greenThumbLocal.terraindata1.detailPrototypes.Length == 0)
                    return false;
                Vector3 localPoint;
                // Convert the point to terrain local coordinates
                if (useGivenWorldPosition)
                {
                    localPoint = hitpoint;
                }
                else
                    localPoint = terrainData2.transform.InverseTransformPoint(hitpoint);
                if (debugging)
                    Debug.DrawLine(localPoint, terrainData2.transform.position, Color.red, 22);

                // Calculate the position within the terrain
                int terrainDetailX = Mathf.RoundToInt(localPoint.x / terrainData2.greenThumbLocal.terrainSize.x * terrainData2.greenThumbLocal.terraindata1.detailResolution);
                int terrainDetailY = Mathf.RoundToInt(localPoint.z / terrainData2.greenThumbLocal.terrainSize.z * terrainData2.greenThumbLocal.terraindata1.detailResolution);
                float fracX = localPoint.x / terrainData2.greenThumbLocal.terrainSize.x * terrainData2.greenThumbLocal.terraindata1.detailResolution - terrainDetailX;
                float fracY = localPoint.z / terrainData2.greenThumbLocal.terrainSize.z * terrainData2.greenThumbLocal.terraindata1.detailResolution - terrainDetailY;
                if (fracX > 0.5f) terrainDetailX++;
                if (fracY > 0.5f) terrainDetailY++;

                bool allowItemToAdd = false;

                int[,] detailLayer = terrainData2.greenThumbLocal.terraindata1.GetDetailLayer(terrainDetailX, terrainDetailY, targetSize, targetSize, targetLayer);
                for (int y = 0; y < detailLayer.GetLength(1); y++)
                {
                    for (int x = 0; x < detailLayer.GetLength(0); x++)
                    {
                        detailLayer[x, y] = terrainData2.greenThumbLocal.terraindata1.maxDetailScatterPerRes;
                    }
                }
                // Apply changes to the terrain for this layer
                terrainData2.greenThumbLocal.terraindata1.SetDetailLayer(terrainDetailX, terrainDetailY, targetLayer, detailLayer);
                if (debugging)
                {
                    Vector3 detailPoint = new(
                        terrainDetailX * terrainData2.greenThumbLocal.terrainSize.x / terrainData2.greenThumbLocal.terraindata1.detailResolution, 0, terrainDetailY * terrainData2.greenThumbLocal.terrainSize.z / terrainData2.greenThumbLocal.terraindata1.detailResolution
                    );
                    detailPoint = terrainData2.transform.TransformPoint(detailPoint);
                    Debug.DrawLine(detailPoint - Vector3.right * 0.1f, detailPoint + Vector3.right * 0.1f, Color.green, 10f);
                    Debug.DrawLine(detailPoint - Vector3.forward * 0.1f, detailPoint + Vector3.forward * 0.1f, Color.green, 10f);
                }

                if (allowItemToAdd)
                {
                    if (debugging)
                        Debug.Log("new detail density was set.");
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        public void CreateNewTree(int cellID, Vector3 instanceTargetPosition, int protoIndex, float height, float width, Color color, GreenThumbCellManager terrainData2)
        {
            // since we do not alter the total index
            // we can continue adding by using the next integer in totalIndexCount
            // this is always reset when regenerating terrain data
            // It is crucial that the position is calculated using the terrain height
            var offset = terrainData2.transform.InverseTransformPoint(instanceTargetPosition);
            offset.y = terrainData2.greenThumbLocal.terraindata1.GetInterpolatedHeight(offset.x, offset.z);
            Vector2 newtreenormalizedPos = new(Mathf.InverseLerp(0.0f, terrainData2.greenThumbLocal.terraindata1.size.x, offset.x), Mathf.InverseLerp(0.0f, terrainData2.greenThumbLocal.terraindata1.size.z, offset.z));
            var offset2 = new Vector3(newtreenormalizedPos.x, 0, newtreenormalizedPos.y);
            if (greenThumbGlobal.debuggingGlobal)
                Debug.DrawLine(offset, offset + Vector3.up * 10, Color.magenta, 10);
            TreeInstance treeInstance = new()
            {
                position = offset2,
                prototypeIndex = protoIndex,
                heightScale = height,
                widthScale = width,
                lightmapColor = color
            };
            var Coffset = terrainData2.greenThumbLocal.terrainPosition + Vector3.Scale(treeInstance.position, terrainData2.greenThumbLocal.terraindata1.size);
            terrainData2.greenThumbLocal.totalTreeIndexCount++;
            // set
            List<TreeInstance> updatedTreeInstances = new(terrainData2.greenThumbLocal.terraindata1.treeInstances)
        {
            treeInstance
        };
            terrainData2.greenThumbLocal.terraindata1.SetTreeInstances(updatedTreeInstances.ToArray(), true);
            //terrainData2.greenThumbLocal.terraindata1.SetTreeInstance(terrainData2.greenThumbLocal.totalTreeIndexCount, treeInstance); // cant set only one :(
            // use this if we remove the actual index in tree instances 
            // for example if we update the array without the index
            // we will shift down all indexes above that index 
            // must be done for all cells in a terrain!
            //for (int i = 0; i < cell.indexList.Count; i++)
            //{
            //    if (cell.indexList[i] >= tree.Item1)
            //    {
            //        cell.indexList[i]--;
            //    }
            //}
            //
            // store data
            terrainData2.greenThumbLocal.GreenCellProfiles[cellID].indexOfPositionsList.Add(terrainData2.greenThumbLocal.totalTreeIndexCount);
            //GetTreeRules(terrainData2.greenThumbLocal.terraindata1.treePrototypes[treeInstance.prototypeIndex].prefab.name) // can store data per category if needed
            if (terrainData2.greenThumbLocal.terraindata1.treePrototypes[protoIndex].prefab.TryGetComponent<CapsuleCollider>(out var capsuleCollider))
            {
                // mid point of the collider is added to the position the collider at terrain surface, uses the tree instance position scaled while we use the other logic to place it
                float verticalOffset = capsuleCollider.height * 0.5f;
                GameObject treeCollider = new("FakeTreeCollider")
                {
                    tag = "GreenThumb"
                };
                var realC = treeCollider.AddComponent<CapsuleCollider>();
                realC.height = capsuleCollider.height;
                realC.radius = capsuleCollider.radius;
                treeCollider.transform.position = Coffset + new Vector3(0, verticalOffset + instanceTargetPosition.y, 0);
                treeCollider.transform.parent = terrainData2.greenThumbLocal.GreenCellProfiles[cellID].cellMesh;
                treeCollider.transform.SetAsLastSibling();
                terrainData2.greenThumbLocal.GreenCellProfiles[cellID].positionList.Add(treeCollider.transform.position);
            }
            else
                terrainData2.greenThumbLocal.GreenCellProfiles[cellID].positionList.Add(Coffset);
        }
        #endregion Creation
    }
}