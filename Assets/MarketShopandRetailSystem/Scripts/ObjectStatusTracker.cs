using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class ObjectStatusTracker : MonoBehaviour
    {
        [Tooltip("This string must be a unique id for this item if you want to track it")]
        public string ItemUniqueID;

        void Start()
        {
            if (!string.IsNullOrEmpty(ItemUniqueID) && SaveLoadManager.Instance.IsObjectDestroyed(ItemUniqueID))
            {
                Destroy(gameObject);
            }
        }

        public void DestroyObjectForever()
        {
            DestroyObjectForever(0.5f);
        }

        public void DestroyObjectForever(float destroyDelay)
        {
            SaveLoadManager.Instance.MarkObjectAsDestroyed(ItemUniqueID);
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }
}
