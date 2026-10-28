using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

namespace MarketShopandRetailSystem
{
    public class GameCanvas : MonoBehaviour
    {
        public static GameCanvas Instance;
        public Image image_Blinking;
        public Image Button_Build;
        public Image Button_Rotate;
        public Sprite Sprite_Touch;
        public GameObject Panel_WarningPanel;
        public LayerMask layerMaskForInteract;
        public GameObject Panel_GameUI;
        public GameObject Panel_Pause;
        public GameObject Panel_Settings;
        public GameObject Panel_Inventory;
        public Image Image_Sprite_Blood;
        public Image Image_Health;
        public Image Image_Stamina;
        public Text Text_Info;
        [HideInInspector]
        public GameObject LastClickedArea;
        [HideInInspector]
        private bool isGameOver = false;
        [HideInInspector]
        public bool isPaused = false;
        public GameObject Crosshair;

        public Text Text_Money;
        public Text Text_Experience;

        public Text Text_GrabHint;
        public Text Text_MoveHint;

        public GameObject[] Panels_Mobile;
        public GameObject Button_Interact;
        public GameObject Button_GetProductBackToOrderBox;
        public GameObject Button_Mechanics;
        public GameObject Button_Jump;
        public Text Text_Map;

        public GameObject Panel_ShopName;
        public InputField inputfield_ShopName;
        
        public GameObject Panel_Product_Price;
        public InputField InputField_Product_Price;
        public Text Text_Product_Cost;
        public Text Text_Product_Status;

        public GameObject Button_Hire_Cleaner;
        public GameObject Button_Hire_Cashier;
        public GameObject Button_Hire_Cleaner_Remover;
        public GameObject Button_Hire_Cashier_Remover;
        public Text Text_Cleaner_Amount;
        public Text Text_Cashier_Amount;
        public Text Button_Hire_Cleaner_SalaryText;
        public Text Button_Hire_Cashier_SalaryText;
        public GameObject Panel_Bill;
        public Text Text_Bill_Title;
        public Text Text_Bill_Amount;


        private void Awake()
        {
            Instance = this;
        }

        public void InputChange_TextShopName()
        {
            if(AdvancedGameManager.Instance.currentSelectedShop != null)
            {
                AdvancedGameManager.Instance.currentSelectedShop.Text_ShopSign.text = inputfield_ShopName.text;
            }
        }

        public void ShowBills(string billType, int Amount)
        {
            Panel_Bill.SetActive(true);
            Text_Bill_Title.text = "BILL: "+ billType.ToString();
            Text_Bill_Amount.text = "PRICE: " + AdvancedGameManager.Instance.CurrencySymbol + Amount.ToString();
            HeroPlayerScript.Instance.DeactivatePlayer();
            CameraScript.Instance.enabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void HideBillPanel()
        {
            Panel_Bill.SetActive(false);
            HeroPlayerScript.Instance.ActivatePlayer();
            CameraScript.Instance.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void Click_Bill_Pay()
        {
            AdvancedGameManager.Instance.UpdatePlayerValues();
            int currentBillAmount = SaveLoadManager.Instance.GetCurrentBillAmount();
            if (AdvancedGameManager.Instance.playerValues.Money >= currentBillAmount)
            {
                AdvancedGameManager.Instance.Spend(currentBillAmount);
                HideBillPanel();
                MailBoxController.Instance.RemoveTheBill();
            }
            else
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Money!", false);
            }
        }

        public void SelectColor(Color colorCode)
        {
            Material m = AdvancedGameManager.Instance.currentSelectedShop.rendererSign.material;
            AdvancedGameManager.Instance.currentSelectedShop.SignColor = colorCode;
            m.SetColor("_Color", colorCode);
            AdvancedGameManager.Instance.currentSelectedShop.rendererSign.material = m;
        }

        private string currentProductName = "";

        public void Show_Panel_Product_Price(String productName)
        {
            // Get Current Price of Sellable Product:
            SellableObject productDetails = InventoryManager.Instance.SellableObjects.Where(x => x.Name == productName).FirstOrDefault();
            if (productDetails == null) return;
            currentProductName = productName;
            int SellingPrice = productDetails.GetSellingPrice();
            Panel_Product_Price.SetActive(true);
            InputField_Product_Price.text = SellingPrice.ToString();
            Text_Product_Cost.text = AdvancedGameManager.Instance.CurrencySymbol + productDetails.BuyingPrice.ToString();
            if(SellingPrice <= productDetails.BuyingPrice)
            {
                Text_Product_Status.text = "Cheap!";
            }
            else if(SellingPrice < productDetails.BuyingPrice * AdvancedGameManager.Instance.ProductsProfitLimitForBeingExpensiveTimes)
            {
                Text_Product_Status.text = "Average! Good";
            }
            else
            {
                Text_Product_Status.text = "Expensive!";
            }
            HeroPlayerScript.Instance.DeactivatePlayer();
            CameraScript.Instance.enabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        public void Click_Product_Price_Increase()
        {
            SellableObject productDetails = InventoryManager.Instance.SellableObjects.Where(x => x.Name == currentProductName).FirstOrDefault();
            if (productDetails == null) return;
            int newPrice = Convert.ToInt32(InputField_Product_Price.text);
            newPrice++;
            InputField_Product_Price.text = newPrice.ToString();
            if (newPrice <= productDetails.BuyingPrice)
            {
                Text_Product_Status.text = "Cheap!";
            }
            else if (newPrice < productDetails.BuyingPrice * AdvancedGameManager.Instance.ProductsProfitLimitForBeingExpensiveTimes)
            {
                Text_Product_Status.text = "Average! Good";
            }
            else
            {
                Text_Product_Status.text = "Expensive!";
            }
        }

        public void Click_Product_Price_Decrease()
        {
            SellableObject productDetails = InventoryManager.Instance.SellableObjects.Where(x => x.Name == currentProductName).FirstOrDefault();
            if (productDetails == null) return;
            int newPrice = Convert.ToInt32(InputField_Product_Price.text);
            newPrice--;
            if (newPrice < 1) newPrice = 1;
            InputField_Product_Price.text = newPrice.ToString();
            if (newPrice <= productDetails.BuyingPrice)
            {
                Text_Product_Status.text = "Cheap!";
            }
            else if (newPrice < productDetails.BuyingPrice * AdvancedGameManager.Instance.ProductsProfitLimitForBeingExpensiveTimes)
            {
                Text_Product_Status.text = "Average! Good";
            }
            else
            {
                Text_Product_Status.text = "Expensive!";
            }
        }

        public void Click_Product_Price_Save()
        {
            int newPrice = Convert.ToInt32(InputField_Product_Price.text);
            SaveLoadManager.Instance.SetProductSellingPrice(currentProductName, newPrice);
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            HeroPlayerScript.Instance.ActivatePlayer();
            CameraScript.Instance.enabled = true;
            Panel_Product_Price.SetActive(false);
            // Broadcast to All Price Tags!
            List<FloorScript> allFloors = GameObject.FindObjectsByType<FloorScript>(FindObjectsSortMode.None).ToList();
            for (int i = 0; i < allFloors.Count; i++)
            {
                if(allFloors[i].ProductNameOntheFloor == currentProductName)
                {
                    // Update this Floor Price Detail!
                    allFloors[i].UpdatePriceDetails();
                }
            }
            InventoryManager.Instance.CheckCurrentEquipmentList();
            List<SellableObject> allSellableRelatedObjects = InventoryManager.Instance.CurrentEquipmentList.Where(x => x.CompareTag("Sellable") && x.GetComponent<SellableObject>().Name == currentProductName).Select(x => x.GetComponent<SellableObject>()).ToList();
            for (int i = 0; i < allSellableRelatedObjects.Count; i++)
            {
                allSellableRelatedObjects[i].SellingPrice = newPrice;
            }
            currentProductName = "";
        }

        public void Submit_ShopName()
        {
            AdvancedGameManager.Instance.currentSelectedShop.BuyAndSaveDetails();
            Panel_ShopName.SetActive(false);
            HeroPlayerScript.Instance.ActivatePlayer();
            HeroPlayerScript.Instance.ActivatePlayerInputs();
            AdvancedGameManager.Instance.currentSelectedShop = null;
            inputfield_ShopName.text = "";
            CameraScript.Instance.enabled = true;
            if(AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void Show_Panel_ShopName()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            Panel_ShopName.SetActive(true);
            HeroPlayerScript.Instance.DeactivatePlayer();
            CameraScript.Instance.enabled = false;
        }

        public void Click_BacktoMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void UpdateStatus()
        {
            AdvancedGameManager.Instance.UpdatePlayerValues();
            Text_Money.text = AdvancedGameManager.Instance.playerValues.Money.ToString();
            Text_Experience.text = AdvancedGameManager.Instance.playerValues.Experience.ToString();
        }

        public void Show_GrabbingHint()
        {
            if(AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                Text_GrabHint.text = AdvancedGameManager.Instance.GrabbingKey.ToString() + " TO GRAB";
            }
            else
            {
                Text_GrabHint.text = "GRAB";
            }
            Text_GrabHint.gameObject.SetActive(true);
            if (Text_MoveHint != null)
            {
                Text_MoveHint.text = AdvancedGameManager.Instance.controllerType == ControllerType.PC
                    ? AdvancedGameManager.Instance.RelocatingKey.ToString() + " TO MOVE" : "MOVE";
                Text_MoveHint.gameObject.SetActive(ShelfRelocation.Supports(HeroPlayerScript.Instance.GrableObject));
            }
        }

        public void Hide_GrabbingHint()
        {
            Text_GrabHint.gameObject.SetActive(false);
            if (Text_MoveHint != null) Text_MoveHint.gameObject.SetActive(false);
        }

        public void Show_ShelfMovingHint()
        {
            if (Text_MoveHint == null) return;
            Text_MoveHint.text = AdvancedGameManager.Instance.controllerType == ControllerType.PC
                ? "ESC TO CANCEL" : "CANCEL MOVE";
            Text_MoveHint.gameObject.SetActive(true);
        }

        float lastTabClick = 0;

         public void Click_Tab()
        {
            if(!isPaused && Time.time > lastTabClick + 0.25f && !isGameOver &&  (AdvancedGameManager.Instance.CurrentMode == Mode.Free || AdvancedGameManager.Instance.CurrentMode == Mode.InMechanicSelecting) && AdvancedGameManager.Instance.CameraMain.activeSelf)
            {
                lastTabClick = Time.time;
                MechanicSelectionManager.Instance.Toogle_Panel_MechanicSelection();
            }
        }
        public void Show_Panel_SellerShop()
        {
            Panel_Inventory.SetActive(true);
            AdvancedGameManager.Instance.CurrentMode = Mode.InSellerList;
            FirstPersonController.Instance.enabled = false;
            InventoryManager.Instance.LoadAllObjects();
            isPaused = true;
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            CameraScript.Instance.enabled = false;
        }
        public void Hide_Panel_SellerShop(bool showHammer)
        {
            if (!showHammer)
            {
                FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
            }
            Panel_Inventory.SetActive(false);
            AdvancedGameManager.Instance.CurrentMode = Mode.Free;
            FirstPersonController.Instance.enabled = true;
            HeroPlayerScript.Instance.ActivatePlayer();
            CameraScript.Instance.enabled = true;
            isPaused = false;
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }


        public void Show_Blood_Effect()
        {
            Image_Sprite_Blood.gameObject.SetActive(true);
            Image_Sprite_Blood.GetComponent<Animation>().Play("BloodEffect");
            StartCoroutine(HideEffect());
        }

        public void Blink()
        {
            image_Blinking.GetComponent<Animation>().Play();
        }

        IEnumerator HideEffect()
        {
            yield return new WaitForSeconds(1);
            Image_Sprite_Blood.gameObject.SetActive(false);
        }


        public void Click_ButtonPause()
        {
            if (isPaused)
            {
                Click_Continue();
            }
            else
            {
                Click_Pause();
            }
        }


        public void Click_Continue()
        {
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            HeroPlayerScript.Instance.ActivatePlayerInputs();
            HeroPlayerScript.Instance.ActivatePlayer();
            CameraScript.Instance.enabled = true;
            Time.timeScale = 1;
            isPaused = false;
            Panel_Pause.SetActive(false);
            Panel_Settings.SetActive(false);
            Panel_GameUI.SetActive(true);
        }

        public void ShowHint(string text)
        {
            StartCoroutine(ShowHintInTime(text));
        }

        IEnumerator ShowHintInTime(string text)
        {
            yield return new WaitForSeconds(0.25f);
            GameCanvas.Instance.Show_WarningShort(text);
            yield return new WaitForSeconds(3);
            GameCanvas.Instance.Hide_Warning();
        }

        public void Show_Inventory()
        {
            GameCanvas.Instance.Panel_Inventory.SetActive(true);
            InventoryManager.Instance.LoadMyInventory();
            PanelInventoryTabsSelector.Instance.Select(0);
        }

        public void Show_OrderBoxShipment()
        {
            GameCanvas.Instance.Panel_Inventory.SetActive(true);
            InventoryManager.Instance.LoadMyInventory();
            PanelInventoryTabsSelector.Instance.Select(2);
        }

        

        public void Hide_Inventory()
        {
            GameCanvas.Instance.Panel_Inventory.SetActive(false);
        }

        public void Click_Pause()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            HeroPlayerScript.Instance.DeactivatePlayer();
            CameraScript.Instance.enabled = false;
            Time.timeScale = 0;
            isPaused = true;
            Panel_Pause.SetActive(true);
            Panel_GameUI.SetActive(false);
        }

        public void UpdateHealth()
        {
            Image_Health.fillAmount = (HeroPlayerScript.Instance.Health / HeroPlayerScript.Instance.TotalHealth);
        }

        public void UpdateStamina()
        {
            Image_Stamina.fillAmount = (HeroPlayerScript.Instance.Stamina / HeroPlayerScript.Instance.TotalStamina);
        }

        void Update()
        {
            if (AdvancedGameManager.Instance.escapeAction.WasPressedThisFrame() && !isGameOver && AdvancedGameManager.Instance.controllerType == ControllerType.PC)
            {
                if (AdvancedGameManager.Instance.CurrentMode == Mode.InInventoryLocating)
                {
                    InventoryManager.Instance.CancelLocating();
                    return;
                }

                if(Panel_ShopName.activeInHierarchy)
                {
                    return;
                }

                if (AdvancedGameManager.Instance.CurrentMode == Mode.InMechanicSelecting)
                {
                    MechanicSelectionManager.Instance.Toogle_Panel_MechanicSelection();
                    return;
                }

                if (AdvancedGameManager.Instance.CurrentMode == Mode.InSellerList)
                {
                    Hide_Panel_SellerShop(false);
                    return;
                }

                if (MapPanel.activeSelf)
                {
                    MapPanel.SetActive(false);
                    MapPanelCamera.SetActive(false);
                    RenderSettings.fog = true;
                    HeroPlayerScript.Instance.ActivatePlayer();
                    CameraScript.Instance.enabled = true;
                    return;
                }


                if (isPaused)
                {
                    Click_Continue();
                }
                else
                {
                    Click_Pause();
                }
            }
            else if (AdvancedGameManager.Instance.selectionAction.WasPressedThisFrame() && !isGameOver)
            {
                MechanicSelectionManager.Instance.Toogle_Panel_MechanicSelection();
            }
            else if (AdvancedGameManager.Instance.mapAction.WasReleasedThisFrame() && !Panel_ShopName.activeInHierarchy)
            {
                ToogleMap();
            }
        }

        public void ToogleMap()
        {
            if (MapPanel.activeSelf)
            {
                MapPanel.SetActive(false);
                MapPanelCamera.SetActive(false);
                if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                RenderSettings.fog = true;
                HeroPlayerScript.Instance.ActivatePlayer();
                CameraScript.Instance.enabled = true;
            }
            else
            {
                MapPanel.SetActive(true);
                if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                }
                MapPanelCamera.SetActive(true);
                RenderSettings.fog = false;
                HeroPlayerScript.Instance.DeactivatePlayer();
                CameraScript.Instance.enabled = false;
            }
        }

        public GameObject MapPanel;
        public GameObject MapPanelCamera;

        public void Click_Settings()
        {
            Panel_Settings.SetActive(true);
            Panel_Pause.SetActive(false);
        }

        public void Click_Close_Settings()
        {
            Panel_Settings.SetActive(false);
            Panel_Pause.SetActive(true);
        }

        public void Click_ShowNote()
        {
            Panel_GameUI.SetActive(false);
            HeroPlayerScript.Instance.DeactivatePlayer();
        }

        private void Start()
        {
            UpdateStatus();
            Text_GrabHint.gameObject.SetActive(false);
            if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
            {
                Text_Map.text = "Map";
                for (int i = 0; i < Panels_Mobile.Length; i++)
                {
                    Panels_Mobile[i].SetActive(true);
                }
                Button_Build.sprite = Sprite_Touch;
                Button_Rotate.sprite = Sprite_Touch;
            }
            else
            {
                Text_Map.text = "Map (M)";
                for (int i = 0; i < Panels_Mobile.Length; i++)
                {
                    Panels_Mobile[i].SetActive(false);
                }
            }
            Button_Hire_Cashier.SetActive(AdvancedGameManager.Instance.canHireCashier);
            Button_Hire_Cleaner.SetActive(AdvancedGameManager.Instance.canHireCleaner);
            Button_Hire_Cashier_Remover.SetActive(AdvancedGameManager.Instance.cashierHired);
            Button_Hire_Cleaner_Remover.SetActive(AdvancedGameManager.Instance.cleanerHired);
            Button_Hire_Cleaner_SalaryText.text = "Salary:" +AdvancedGameManager.Instance.CurrencySymbol.ToString()+ AdvancedGameManager.Instance.cleanerDailySalary.ToString();
            Button_Hire_Cashier_SalaryText.text = "Salary:" + AdvancedGameManager.Instance.CurrencySymbol.ToString()+ AdvancedGameManager.Instance.cashierDailySalary.ToString();
        }

        public void Click_Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void Show_GameUI()
        {
            Panel_GameUI.SetActive(true);
        }

        public void Hide_GameUI()
        {
            Panel_GameUI.SetActive(false);
        }

        public void Show_WarningShort(string message)
        {
            GameCanvas.Instance.Text_Info.text = message;
        }

        public void Show_Warning(ItemScript item, string message = "")
        {
            string text = "";
            text = item.Name + "\n";
            if (string.IsNullOrEmpty(message))
            {
                if (item.interactionType != InteractionType.None && item.itemType != ItemType.Bin)
                {
                    if (AdvancedGameManager.Instance.controllerType == ControllerType.PC)
                    {
                        text += "(" + AdvancedGameManager.Instance.InteractingKey.ToString() + ") " + item.interactionType.ToString();
                    }
                    else
                    {
                        text += item.interactionType.ToString();
                    }
                }
            }
            else
            {
                text = message;
            }
            if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
            {
                Button_Interact.SetActive(item.GetComponent<CashRegister>() == null && item.itemType != ItemType.Bin);
            }
            GameCanvas.Instance.Text_Info.text = text;
        }

        public GameObject Panel_SpendGetNotification;
        public Text Text_SpendGetNotification;

        public void Show_SpendGet(int amount)
        {
            if (amount > 0)
            {
                Text_SpendGetNotification.color = Color.green;
                Text_SpendGetNotification.text = "+" + amount;
                Panel_SpendGetNotification.SetActive(true);
            }
            else if (amount < 0)
            {
                Text_SpendGetNotification.color = Color.red;
                Text_SpendGetNotification.text = "-" + amount;
                Panel_SpendGetNotification.SetActive(true);
            }
            Panel_SpendGetNotification.GetComponent<Animation>().Play();
        }

        public void Show_Warning_Not(String textID, bool isPositive)
        {
            ShowWarningPanel(textID, isPositive);
        }
        IEnumerator i;
        public Color color_positive;
        public Color color_negative;


        void ShowWarningPanel(String text, bool isPositive)
        {
            Panel_WarningPanel.SetActive(false);
            Panel_WarningPanel.SetActive(true);
            Panel_WarningPanel.GetComponentInChildren<Text>().text = text;
            if (isPositive)
            {
                Panel_WarningPanel.GetComponent<Image>().color = color_positive;
            }
            else
            {
                Panel_WarningPanel.GetComponent<Image>().color = color_negative;
            }
            if (i != null)
            {
                StopCoroutine(i);
            }
            i = CloseWarningNot();
            StartCoroutine(i);

        }

        IEnumerator CloseWarningNot()
        {
            yield return new WaitForSeconds(2f);
            Hide_Warning_Not();
        }

        public void Hide_Warning()
        {
            GameCanvas.Instance.Text_Info.text = "";
            if(AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
            {
                if(!HeroPlayerScript.Instance.isHoldingBox)
                {
                    Button_Interact.SetActive(false);
                    HeroPlayerScript.Instance.isButtonInteractHeld = false;
                }
            }
        }

        public void Hide_Warning_Not()
        {
            Panel_WarningPanel.SetActive(false);
            Panel_WarningPanel.GetComponentInChildren<Text>().text = "";
        }
    }
}
