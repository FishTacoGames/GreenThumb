using System.Collections;
using UnityEngine;

namespace FishTacoGames
{
    /// <summary>
    /// Simple pool specifically for the tree replacements
    /// </summary>
    public class GreenThumbTreePoolable : MonoBehaviour
    {
        public void LeavePool(int TimeInSeconds)
        {
            StartCoroutine(PoolTimer(TimeInSeconds));
        }
        public void ReturnToPool()
        {
            StopAllCoroutines();
            transform.SetAsLastSibling();
        }
        private IEnumerator PoolTimer(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            ReturnToPool();
        }
    }
}