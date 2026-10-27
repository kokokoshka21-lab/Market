using System.Linq;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class FloorScript : MonoBehaviour
    {
        public GameObject[] Panel_Price;
        public string ProductNameOntheFloor;
        public TMPro.TMP_Text[] Text_Price;

        public void SetPriceDetails(SellableObject product)
        {
            for (int i = 0; i < Panel_Price.Length; i++)
            {
                Panel_Price[i].SetActive(true);
            }
            for (int i = 0; i < Text_Price.Length; i++)
            {
                Text_Price[i].text = AdvancedGameManager.Instance.CurrencySymbol + product.GetSellingPrice().ToString();
            }
            ProductNameOntheFloor = product.Name;
        }

        public void UpdatePriceDetails()
        {
            int price = InventoryManager.Instance.SellableObjects.Where(x => x.Name == ProductNameOntheFloor).FirstOrDefault().GetSellingPrice();
            for (int i = 0; i < Text_Price.Length; i++)
            {
                Text_Price[i].text = AdvancedGameManager.Instance.CurrencySymbol + price.ToString();
            }
        }

        public void CheckPriceOnTag()
        {
            // Get all PlacablePoints Of Parent
            PlacablePoint[] allPlaceablePointsOfCurrentFloor = transform.GetComponentsInChildren<PlacablePoint>();
            // Check do we have a different kind of Sellable Object?
            var differentObject = allPlaceablePointsOfCurrentFloor.Where(x => x.objectToGrab != null).FirstOrDefault();
            if (differentObject == null)
            {
                // None left!
                for (int i = 0; i < Panel_Price.Length; i++)
                {
                    Panel_Price[i].SetActive(false);
                }
                for (int i = 0; i < Text_Price.Length; i++)
                {
                    Text_Price[i].text = "";
                }
                ProductNameOntheFloor = "";
            }
        }

        public void Show_Panel_Price()
        {
            GameCanvas.Instance.Show_Panel_Product_Price(ProductNameOntheFloor);
        }
    }
}
