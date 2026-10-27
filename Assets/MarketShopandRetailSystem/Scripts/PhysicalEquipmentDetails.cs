using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class PhysicalEquipmentDetails : MonoBehaviour
    {
        public int equipmentIndex = 0;
        public string Name;
        public EquipmentType equipmentType;
        public int Price;
        public int Experience;
        public Sprite ImageSprite;

        [HideInInspector]
        public int CurrentInStorage = 0;

        public void NotifyAllSellablesIfYouHaveAny()
        {
            if (GetComponent<ArrayableArea>() != null)
            {
                GetComponent<ArrayableArea>().DestroyAllProducts();
            }
        }
    }
}