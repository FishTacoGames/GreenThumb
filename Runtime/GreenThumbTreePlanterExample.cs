using UnityEngine;
using UnityEngine.InputSystem;
using FishTacoGames;
/// <summary>
/// Simple example of adding trees and detail
/// </summary>
public class GreenThumbTreePlanterExample : MonoBehaviour
{
    public InputActionReference plantTreeAction;
    public InputActionReference plantGrassAction;
    private GreenThumbCellManager ActiveCellManager;
    public Camera playerCamera;
    private void Start()
    {
        plantTreeAction.action.performed += PlantTree;
        plantGrassAction.action.performed += PlantGrass;
    }

    void PlantTree(InputAction.CallbackContext _)
    {
        var hit = CameraCast();
        if (hit.collider == null)
            return;
        int cell = GreenThumbGridLogic.GetCurrentCell(hit.point, ActiveCellManager.greenThumbLocal.terrainSize, ActiveCellManager.greenThumbLocal.terrainPosition);
        GreenThumbManager.Instance.CreateNewTree(cell, hit.point, 0, 1, 1, new(1, 1, 1), ActiveCellManager);
    }
    void PlantGrass(InputAction.CallbackContext _)
    {
        var hit = CameraCast();
        if (hit.collider == null)
            return;
        GreenThumbManager.Instance.TrySetTerrainDetailPatchMaxDensity(hit.collider, hit.point, 5, 0, ActiveCellManager);
    }
    RaycastHit CameraCast()
    {
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, 25f))
        {
            ActiveCellManager = GreenThumbManager.Instance.GetCurrentTerrain(hit.point).GetComponent<GreenThumbCellManager>();
            return hit;
        }
        else return default;
    }
}
