using System.Collections;
using System.Linq;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class ArrayableArea : MonoBehaviour
    {
        public PlacablePoint[] PlacablePoints;
        private OrderBox OrderBox;
        private PhysicalEquipmentDetails detail;
        public StorageType storageType;
        [Tooltip("Use the existing placement points for whole order boxes instead of individual products.")]
        public bool boxesOnly = false;

        public bool CanPlaceBox(PlacablePoint point)
        {
            return boxesOnly && GetComponent<OrderBox>() == null
                && point != null && PlacablePoints.Contains(point)
                && point.isAvailable && point.objectToGrab == null;
        }

        private void Awake()
        {
            detail = GetComponent<PhysicalEquipmentDetails>();
            OrderBox = GetComponent<OrderBox>();
            for (int i = 0; i < PlacablePoints.Length; i++)
            {
                PlacablePoints[i].ID = i;
            }
        }

        IEnumerator Start()
        {
            OrderBox = GetComponent<OrderBox>();
            // OrderBox restores product identities itself, including legacy empty-slot flags.
            if (OrderBox != null) yield break;
            yield return new WaitForSeconds(0.5f);
            detail = GetComponent<PhysicalEquipmentDetails>();
            int objectID = detail.equipmentIndex;
            for (int i = 0; i < PlacablePoints.Length; i++)
            {
                if (SaveLoadManager.Instance.GetShelfSlotStatus(detail.Name, objectID, i, 0) == -1)
                {
                    Destroy(PlacablePoints[i].objectToGrab);
                    PlacablePoints[i].isAvailable = true;
                    PlacablePoints[i].objectToGrab = null;
                }
            }
            OrderBox = GetComponent<OrderBox>();
        }

        public void RemoveItem(int i)
        {
            if (OrderBox != null)
            {
                PlacablePoints[i].isAvailable = true;
                PlacablePoints[i].objectToGrab = null;
                OrderBox.SaveContents();
                OrderBox.RefreshContentsDisplay();
                return;
            }
            int objectID = detail.equipmentIndex;
            SaveLoadManager.Instance.SetShelfSlotStatus(detail.Name, objectID, i, -1);
            PlacablePoints[i].isAvailable = true;
            PlacablePoints[i].objectToGrab = null;

            if (OrderBox != null)
            {
                OrderBox.textCount.text = PlacablePoints.Where(x => !x.isAvailable).Count().ToString();
            }
        }

        public void RemoveItemFromSelftForPuttingOrderBox(PlacablePoint placable)
        {
            SellableObject d = placable.objectToGrab.GetComponent<SellableObject>();
            SaveLoadManager.Instance.RemoveSellableObject(d.Name, d.sellableObjectIndex);
            placable.isAvailable = true;
            placable.objectToGrab = null;

            if (OrderBox != null)
            {
                OrderBox.textCount.text = PlacablePoints.Where(x => !x.isAvailable).Count().ToString();
            }
        }

        

        public void DestroyAllProducts()
        {
            for (int i = 0; i < PlacablePoints.Length; i++)
            {
                if(PlacablePoints[i].objectToGrab != null && PlacablePoints[i].objectToGrab.GetComponent<SellableObject>() != null)
                {
                    PlacablePoints[i].objectToGrab.GetComponent<SellableObject>().CollapseAndDestroy();
                }
            }
        }

        public void AddItem(int i, SellableObject equipment)
        {
            int objectID = detail.equipmentIndex;
            SaveLoadManager.Instance.SetShelfSlotStatus(detail.Name, objectID, i, 1);

            int objectCount = 0;
            var existingSellableObjects = SaveLoadManager.Instance.GetAllSellableObjects();
            var sameTypeObjects = existingSellableObjects.FindAll(x => x.productName == equipment.Name);
            if (sameTypeObjects.Count > 0)
            {
                objectCount = sameTypeObjects.Max(x => x.objectIndex) + 1;
            }

            SaveLoadManager.Instance.AddSellableObject(
                equipment.Name,
                objectCount,
                PlacablePoints[i].transform.position,
                PlacablePoints[i].transform.eulerAngles
            );

            equipment.sellableObjectIndex = objectCount;
            equipment.isAvailable = true;
            equipment.putPoint = PlacablePoints[i];
        }
    }
}
