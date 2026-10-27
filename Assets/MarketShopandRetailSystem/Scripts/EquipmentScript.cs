using UnityEngine;
using UnityEngine.UI;

namespace MarketShopandRetailSystem
{
    public class EquipmentScript : MonoBehaviour
    {
        [HideInInspector]
        public PhysicalEquipmentDetails EquipmentDetail;
        public Sprite ImageSpriteInventory;
        public Sprite ImageSpriteCoin;
        public Sprite ImageSpriteExp;
        public Text Text_Money;
        public Text Text_Exp;
        public Image Image;
        public Text Text_Name;
        public Image Image_Money;
        public Image Image_Exp;
        [HideInInspector]
        public int CurrentInStorage = 0;

        public void Interact()
        {
            if (InventoryManager.Instance.inventoryMode == InventoryMode.SellerShopIsOpen)
            {
                InventoryManager.Instance.Buy(this);
            }
            else
            {
                InventoryManager.Instance.Use(this);
            }
        }

        public void AssignDetails(PhysicalEquipmentDetails details)
        {
            EquipmentDetail = details;
            OnEnable();
        }

        public void Activate()
        {
            CurrentInStorage = SaveLoadManager.Instance.GetInventoryItemAmount(EquipmentDetail.Name);
            if (CurrentInStorage > 0)
            {
                gameObject.SetActive(true);
            }
        }

        public void DecreaseItFromInventory()
        {
            CurrentInStorage = CurrentInStorage - 1;
            SaveLoadManager.Instance.SetInventoryItemAmount(EquipmentDetail.Name, CurrentInStorage);
        }

        private void OnEnable()
        {
            if(EquipmentDetail != null)
            {
                Activate();
                if (InventoryManager.Instance.inventoryMode == InventoryMode.SellerShopIsOpen)
                {
                    Text_Money.text = EquipmentDetail.Price.ToString();
                    if(EquipmentDetail.Experience > 0)
                    {
                        Text_Exp.text = EquipmentDetail.Experience.ToString();
                        Image_Exp.sprite = ImageSpriteExp;
                    }
                    else
                    {
                        Text_Exp.gameObject.SetActive(false);
                        Image_Exp.gameObject.SetActive(false);
                    }
                    Text_Name.text = EquipmentDetail.Name.ToString();
                    Image_Money.sprite = ImageSpriteCoin;
                    gameObject.SetActive(true);
                }
                else
                {
                    if (CurrentInStorage > 0)
                    {
                        gameObject.SetActive(true);
                        Text_Money.text = "You have " + CurrentInStorage.ToString();
                        if (EquipmentDetail.Experience > 0)
                        {
                            Text_Exp.text = EquipmentDetail.Experience.ToString();
                            Image_Exp.sprite = ImageSpriteExp;
                        }
                        else
                        {
                            Text_Exp.gameObject.SetActive(false);
                            Image_Exp.gameObject.SetActive(false);
                        }
                        Text_Name.text = EquipmentDetail.Name.ToString();
                        Image_Money.sprite = ImageSpriteInventory;
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }

                }
                Image.sprite = EquipmentDetail.ImageSprite;
            }
        }
    }

    public enum EquipmentType
    {
        Decoration,
        Poster,
        Sellable
    }
}