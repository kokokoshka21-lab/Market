using UnityEngine;

namespace MarketShopandRetailSystem
{
    [RequireComponent(typeof(BoxCollider))]
    public class BinDisposalZone : MonoBehaviour
    {
        public BinScript bin;

        private void OnTriggerEnter(Collider other)
        {
            if (bin != null) bin.TryDisposeBox(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (bin != null) bin.TryDisposeBox(other);
        }
    }
}
