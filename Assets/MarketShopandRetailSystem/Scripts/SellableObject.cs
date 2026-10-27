using System.Collections;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class SellableObject : MonoBehaviour
    {
        [HideInInspector]
        public bool isAvailable = true;
        public string Name;
        public int BuyingPrice;
        public int SellingPrice;
        public Sprite sprite;
        [HideInInspector]
        public PlacablePoint putPoint;
        public int sellableObjectIndex;
        public StorageType storageType = StorageType.Normal;
        
        private IEnumerator Start()
        {
            GetSellingPrice();
            yield return new WaitForSeconds(2f);
            if(isAvailable && putPoint == null)
            {
                // Destroy this
                CollapseAndDestroy();
            }
        }

        public int GetSellingPrice()
        {
            SellingPrice = SaveLoadManager.Instance.GetProductSellingPrice(Name, SellingPrice);
            return SellingPrice;
        }

        public void CollapseAndDestroy()
        {
            GetComponent<Collider>().isTrigger = false;
            SaveLoadManager.Instance.RemoveSellableObject(Name, sellableObjectIndex);
            InventoryManager.Instance.CurrentEquipmentList.Remove(gameObject);
            gameObject.AddComponent<Rigidbody>();
            Destroy(gameObject, 4);
        }
    }

    [System.Flags]
    public enum StorageType
    {
        Normal = 1 << 0,
        Cold = 1 << 1,
        Drinks = 1 << 2
    }
}