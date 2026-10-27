using System.Globalization;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class ShopScript : MonoBehaviour
    {
        public int ShopID = 0;
        [HideInInspector]
        public string ShopName = "";
        public Color SignColor = Color.blue;
        public bool isRented = false;
        public int RentPrice = 0;
        public int RentExperience = 1;
        public Collider collider;
        public TMPro.TMP_Text Text_ShopSign;
        public MeshRenderer rendererSign;
        public Camera camera;
        public GameObject keys;

        public GameObject ShopLight;

        void Start()
        {
            var shopData = SaveLoadManager.Instance.GetShopData(ShopID);
            isRented = shopData != null ? shopData.isRented : false;
            collider.enabled = !isRented;
            if (!isRented)
            {
                Text_ShopSign.text = "FOR RENT: "+ AdvancedGameManager.Instance.CurrencySymbol + RentPrice.ToString();
                keys.SetActive(true);
            }
            else
            {
                keys.SetActive(false);
                ShopName = shopData.shopName;
                Text_ShopSign.text = ShopName;
                string colorString = shopData.signColorHex;
                if (!string.IsNullOrEmpty(colorString))
                {
                    string[] colorValues = colorString.Split(',');
                    if (colorValues.Length == 4 &&
               float.TryParse(colorValues[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
               float.TryParse(colorValues[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
               float.TryParse(colorValues[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
               float.TryParse(colorValues[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
                    {
                        SignColor = new Color(r, g, b, a);
                        Material m = rendererSign.material;
                        m.SetColor("_Color", SignColor);
                        rendererSign.material = m;
                    }
                }
            }
            InvokeRepeating("CheckDayTime", 1, 10);
        }

        public void CheckDayTime()
        {
            if (DayNightManager.Instance != null)
            {
                ShopLight.SetActive(DayNightManager.Instance.isDark);
            }
        }

        public void BuyAndSaveDetails()
        {
            ShopName = Text_ShopSign.text;
            string colorString = string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.###},{3:0.###}", SignColor.r, SignColor.g, SignColor.b, SignColor.a);
            
            if (!isRented)
            {
                AdvancedGameManager.Instance.UpdatePlayerValues();
                AdvancedGameManager.Instance.Spend(RentPrice);
                AdvancedGameManager.Instance.Get(CollactableType.Experience, 1);
                keys.SetActive(false);
            }
            isRented = true;
            
            SaveLoadManager.Instance.SetShopData(ShopID, isRented, ShopName, colorString);
            
            collider.enabled = !isRented;
            camera.gameObject.SetActive(false);
            AdvancedGameManager.Instance.currentSelectedShop = null;
        }

        public void TryToRentTheShop()
        {
            if (AdvancedGameManager.Instance.playerValues.Money < RentPrice)
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Money!", false);
                return;
            }
            else if (AdvancedGameManager.Instance.playerValues.Experience < RentExperience)
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Experience!", false);
                return;
            }
            else
            {
                AdvancedGameManager.Instance.currentSelectedShop = this;
                GameCanvas.Instance.Show_Panel_ShopName();
                camera.gameObject.SetActive(true);
            }
        }
    }
}
