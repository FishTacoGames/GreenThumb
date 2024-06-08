using System.Collections.Generic;
using UnityEngine;

namespace FishTacoGames
{
    public class GreenThumbCellManager : MonoBehaviour
    {
        public GreenThumbLocalData greenThumbLocal;
        public GameObject CellProfileObj;
        public int ProfileCount => GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal * GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal;
        public void RefreshData()
        {
            greenThumbLocal.layers = new int[greenThumbLocal.terraindata1.detailPrototypes.Length];
            greenThumbLocal.terrainSize = greenThumbLocal.terraindata1.size;
            greenThumbLocal.cellSize = new Vector3(greenThumbLocal.terrainSize.x / GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal, greenThumbLocal.terrainSize.y, greenThumbLocal.terrainSize.z / GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal);
            greenThumbLocal.GreenCellProfiles = new(ProfileCount);
        }
        public void SetCellInactive(int cellID)
        {
            if (CellProfileObj == null)
            {
                if (transform.GetChild(0).CompareTag("GreenThumb"))
                    Destroy(transform.GetChild(0).gameObject);
                Debug.Log("missing profiles");
                GenerateGrid();
                return;
            }
            if (greenThumbLocal.GreenCellProfiles[cellID].cellMesh == null)
            {
                GreenThumbCellProfile cellProfile = greenThumbLocal.GreenCellProfiles[cellID];
                cellProfile.cellMesh = CellProfileObj.transform.GetChild(cellID).transform;
                cellProfile.cellMesh.gameObject.SetActive(false);
                greenThumbLocal.GreenCellProfiles[cellID] = cellProfile;
            }
            else
                greenThumbLocal.GreenCellProfiles[cellID].cellMesh.gameObject.SetActive(false);
        }
        public void SetCellActive(int cellID)
        {
            if (CellProfileObj == null)
                return;
            if (greenThumbLocal.GreenCellProfiles[cellID].cellMesh == null)
            {
                GreenThumbCellProfile cellProfile = greenThumbLocal.GreenCellProfiles[cellID];
                cellProfile.cellMesh = CellProfileObj.transform.GetChild(cellID).transform;
                cellProfile.cellMesh.gameObject.SetActive(true);
                greenThumbLocal.GreenCellProfiles[cellID] = cellProfile;
            }
            else
                greenThumbLocal.GreenCellProfiles[cellID].cellMesh.gameObject.SetActive(true);
        }
        public void ClearCellsGlobal()
        {
            if (CellProfileObj != null)
            {
                DestroyImmediate(CellProfileObj);
            }
            RefreshData();
        }
        public void GenerateGrid()
        {
            RefreshData();
            if (CellProfileObj != null)
            {
                DestroyImmediate(CellProfileObj);
            }
            CellProfileObj = new("CellProfile_" + transform.GetInstanceID());
            // First we must remove old data 
            greenThumbLocal.GreenCellProfiles ??= new(ProfileCount);
            // reset tree count
            var cellID = 0;
            for (int x = 0; x < GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal; x++)
            {
                for (int z = 0; z < GreenThumbManager.Instance.greenThumbGlobal.gridSizesGlobal; z++)
                {
                    // construct our cell
                    Vector3 cellPosition = new Vector3(x * greenThumbLocal.cellSize.x, 0, z * greenThumbLocal.cellSize.z) + new Vector3(greenThumbLocal.cellSize.x / 2, 0, greenThumbLocal.cellSize.z / 2); // important offset!
                    cellPosition += greenThumbLocal.terrainPosition;
                    Bounds bounds = new(cellPosition, greenThumbLocal.cellSize);
                    // set as child
                    CellProfileObj.transform.parent = transform;
                    // either generate mesh or create an empty transform to hold colliders
                    var meshC = GreenThumbMeshGeneratorTerrainCell.GenerateMeshColliderObjectFromBoundsold(bounds, greenThumbLocal.terraindata1, greenThumbLocal.terrainPosition, cellID, false, CellProfileObj.transform, GreenThumbManager.Instance.greenThumbGlobal.generateMeshForCells);
                    // intitilize the cell
                    GreenThumbCellProfile GreencellProfile = new(new List<int>(), cellID, new List<Vector3>(), meshC, bounds);
                    greenThumbLocal.GreenCellProfiles.Add(GreencellProfile);
                    // assign trees per cell
                    greenThumbLocal.totalTreeIndexCount = 0;
                    for (int i = 0; i < greenThumbLocal.terraindata1.treeInstanceCount - 1; i++)
                    {
                        var instance = greenThumbLocal.terraindata1.GetTreeInstance(i);
                        var offset = greenThumbLocal.terrainPosition + Vector3.Scale(instance.position, greenThumbLocal.terraindata1.size);
                        if (bounds.Contains(offset))
                        {
                            if (GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal)
                                Debug.DrawLine(offset, greenThumbLocal.GreenCellProfiles[cellID].cellBounds.center, Color.blue, 15f);
                            greenThumbLocal.GreenCellProfiles[cellID].indexOfPositionsList.Add(greenThumbLocal.totalTreeIndexCount);
                            if (greenThumbLocal.terraindata1.treePrototypes[instance.prototypeIndex].prefab.TryGetComponent<CapsuleCollider>(out var capsuleCollider))
                            {
                                // create and assign size/positions
                                float verticalOffset = capsuleCollider.height * 0.5f - capsuleCollider.radius;
                                GameObject treeCollider = new("FakeTreeCollider")
                                {
                                    tag = "GreenThumb"
                                };
                                var realC = treeCollider.AddComponent<CapsuleCollider>();
                                realC.height = capsuleCollider.height;
                                realC.radius = capsuleCollider.radius;
                                treeCollider.transform.position = offset + Vector3.up * verticalOffset;
                                treeCollider.transform.parent = greenThumbLocal.GreenCellProfiles[cellID].cellMesh;
                                greenThumbLocal.GreenCellProfiles[cellID].positionList.Add(treeCollider.transform.position);
                            }
                            else
                                greenThumbLocal.GreenCellProfiles[cellID].positionList.Add(offset);
                            // each cell uses a unique list
                            // this means we must use the tree count for every list so that it stays consistant
                            // only adding indexes within the bounds
                        }
                        greenThumbLocal.totalTreeIndexCount++;
                    }
                    if (GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal)
                        Debug.Log("Created a list for a single cell, tree count was: " + greenThumbLocal.GreenCellProfiles[cellID].indexOfPositionsList.Count);
                    // quick enable/disable 
                    if (GreenThumbManager.Instance.m_player != null && bounds.Contains(GreenThumbManager.Instance.m_player.position))
                        greenThumbLocal.activeCell = cellID;
                    else
                        SetCellInactive(cellID);
                    cellID++;
                }
            };

            if (GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal)
                Debug.Log("Created " + greenThumbLocal.GreenCellProfiles.Count + " Total cells");
            // Finally check if we need to disable collision of the terrain
            if (GreenThumbManager.Instance.greenThumbGlobal.generateMeshForCells)
                GetComponent<TerrainCollider>().enabled = false;
            else
                GetComponent<TerrainCollider>().enabled = true;
        }
        public int GetTreeInstanceIDFast(Transform T)
        {
            int cell = T.parent.GetSiblingIndex();
            if (greenThumbLocal.GreenCellProfiles[cell].positionList.Contains(T.position))
                return greenThumbLocal.GreenCellProfiles[cell].indexOfPositionsList[GetIndexOfVector(T.position, cell)];
            return -1;
        }
        public int GetIndexOfVector(Vector3 vecT, int Cellindex) => greenThumbLocal.GreenCellProfiles[Cellindex].positionList.IndexOf(vecT);
    }
}