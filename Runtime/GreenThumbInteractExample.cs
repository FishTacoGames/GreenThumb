using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using FishTacoGames;
/// <summary>
/// Example of using the removal method
/// </summary>
public class GreenThumbInteractExample : MonoBehaviour
{
    public bool searchForSmallTrees = false;
    public bool removeDetailAtTreeBase = false;
    public bool debugging = false;
    public VisualEffectAsset particles;
    private readonly float cooldownTime = 0.05f;
    private float lastTriggerTime;
    private int siblingindex = -1;
    private GreenThumbCellManager terrainData2;
    private List<int> cellsToEnable;
    // listen for changes in the ground
    private void Start()
    {
        if (GreenThumbManager.Instance.greenThumbGlobal.disableCells)
        {
            terrainData2 = GreenThumbManager.Instance.GetCurrentTerrain(transform.position).GetComponent<GreenThumbCellManager>();
            StartCoroutine(TerrainSearchLoop());
        }
        else
        {
            terrainData2 = GreenThumbManager.Instance.GetCurrentTerrain(transform.position).GetComponent<GreenThumbCellManager>();
            for (int i = 0; i < terrainData2.greenThumbLocal.GreenCellProfiles.Count; i++)
            {
                if (terrainData2.greenThumbLocal.GreenCellProfiles[i].cellMesh == null || terrainData2.greenThumbLocal.GreenCellProfiles[i].cellMesh.gameObject == null)
                    continue;
                terrainData2.SetCellActive(i);
            }
        }
    }
    // Handles which cells to set active
    private IEnumerator TerrainSearchLoop()
    {
        while (true)
        {
            cellsToEnable = GreenThumbGridLogic.HandleCall(transform.position, terrainData2.greenThumbLocal.terraindata1.size, terrainData2.transform.position);
            for (int i = 0; i < terrainData2.greenThumbLocal.GreenCellProfiles.Count; i++)
            {
                if (terrainData2.greenThumbLocal.GreenCellProfiles[i].cellMesh == null || terrainData2.greenThumbLocal.GreenCellProfiles[i].cellMesh.gameObject == null)
                    continue;
                if (cellsToEnable.Contains(i))
                {
                    terrainData2.SetCellActive(i);
                }
                else
                    terrainData2.SetCellInactive(i);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }


    /// <summary>
    /// Example of spawning effects based on the tree removal
    /// </summary>
    /// <param name="category"></param>
    /// <param name="protoType"></param>
    /// <param name="effectParentTransform"></param>
    private void DoTreeEffect(string category,int protoType, Transform effectParentTransform)
    {
        GameObject particleObject = new("ParticleObject");
        VisualEffect visualEffect = particleObject.AddComponent<VisualEffect>();
        visualEffect.visualEffectAsset = particles;
        particleObject.transform.SetPositionAndRotation(effectParentTransform.position, Quaternion.identity);
        visualEffect.Play();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // since we call so often with "ControllerColliderHit" we need these null checks
        if (hit.collider == null || hit.collider.transform == null)
            return;
        if (Time.time - lastTriggerTime < cooldownTime)
            return;

        // get our active cell/terrain
        if (hit.collider.GetType() == typeof(TerrainCollider) && hit.collider.TryGetComponent(out terrainData2))
        {
            siblingindex = GreenThumbGridLogic.GetCurrentCell(hit.point, terrainData2.greenThumbLocal.terraindata1.size, terrainData2.transform.position);
        }
        else
        {
            terrainData2 = GreenThumbManager.Instance.GetCurrentTerrain(transform.position).GetComponent<GreenThumbCellManager>();
            siblingindex = GreenThumbGridLogic.GetCurrentCell(hit.point, terrainData2.greenThumbLocal.terraindata1.size, terrainData2.transform.position);
        }
        // make sure to not hit the generated ground or rb's
        if (hit.collider.GetType() == typeof(MeshCollider) || hit.collider.TryGetComponent<Rigidbody>(out _))
            return;

        if (GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal)
            Debug.Log("siblingindex was " + siblingindex);

        if (GreenThumbManager.Instance.TryDestroyTree(out var info,hit,transform, siblingindex,true))
        {
            // use some of the tree variables
            if (info.doEffects)
                DoTreeEffect(info.category,info.ProtoID,info.Transform);

            if (GreenThumbManager.Instance.greenThumbGlobal.debuggingGlobal)
                Debug.Log("Tree removal was a success!");
            siblingindex = -1;
            lastTriggerTime = Time.time;
            return;
        }
    }
}
