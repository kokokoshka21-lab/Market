using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MarketShopandRetailSystem
{
    public class AdvancedGameManager : MonoBehaviour
    {
        public GameObject CameraMain;
        public static AdvancedGameManager Instance;
        public PlayerValues playerValues;
        public ControllerType controllerType;
        public Transform incomingOrderBoxPoint;

        [HideInInspector]
        public Mode CurrentMode = Mode.Free;
        public int StartingMoneyAmount = 1000;
        public int StartingExperienceAmount = 1;


        public KeyCode InteractingKey;
        public KeyCode GrabbingKey;
        public KeyCode RelocatingKey = KeyCode.R;
        public KeyCode SelectionKey;
        public KeyCode MapKey;
        [Range(0, 100)]
        public int NPCShoppingPercentageRate = 25;
        [Range(0f, 1f)]
        [Tooltip("Chance of choosing self-service when both checkout types are available.")]
        public float selfServiceCheckoutChance = 0.5f;
        [Min(1f)] [Tooltip("Seconds a customer waits at an unattended ordinary checkout.")]
        public float unattendedCheckoutWaitTime = 30f;
        [Min(1f)] [Tooltip("Maximum seconds waiting for a turn, reaching a checkout, or without scanning progress.")]
        public float checkoutQueueMaxWaitTime = 120f;
        private int BaseNPCShoppingPercentageRate = 0;
        public bool postersAmountCanEffectNPCShoppingPercentageRate = false;
        public string CurrencySymbol = "$";
        public GameObject[] DustPrefabs;

        [HideInInspector]
        public ShopScript currentSelectedShop;
        List<GameObject> dusts = new List<GameObject>();
        public int ProductsProfitLimitForBeingExpensiveTimes = 3;

        public bool canHireCashier = true;
        public int cashierDailySalary = 5;
        public bool canHireCleaner = true;
        public int cleanerDailySalary = 5;

        [HideInInspector]
        public bool cashierHired = false;
        [HideInInspector]
        public int cashierHiredAmount = 0;
        [HideInInspector]
        public bool cleanerHired = false;
        [HideInInspector]
        public int cleanerHiredAmount = 0;

        public GameObject[] Cleaners;
        public GameObject[] Cashiers;

        public int EarnXPPerEarnings = 100;
        public GivingChangeOption givingChangeOption;

        [HideInInspector]
        public bool isShopOpen = false;
        public bool isHangingSignActive = true;

        public bool isPayingBillsSystemActivated = true;
        public GameObject MailBox;

        public InputAction moveAction;
        public InputAction sprintAction;
        public InputAction jumpAction;
        public InputAction interactAction;
        public InputAction grabbingAction;
        public InputAction relocatingAction;
        public InputAction selectionAction;
        public InputAction mapAction;
        public InputAction mouseAction;
        public InputAction mouseRightAction;
        public InputAction escapeAction;
        

        private void Awake()
        {
            Instance = this;
            isShopOpen = !isHangingSignActive;
            playerValues = new PlayerValues();

            cashierHired = SaveLoadManager.Instance.GetCashierHired();
            cashierHiredAmount = SaveLoadManager.Instance.GetCashierHiredAmount();
            cleanerHired = SaveLoadManager.Instance.GetCleanerHired();
            cleanerHiredAmount = SaveLoadManager.Instance.GetCleanerHiredAmount();

            for (int i = 0; i < cashierHiredAmount; i++)
            {
                Cashiers[i].SetActive(true);
            }
            for (int i = 0; i < cleanerHiredAmount; i++)
            {
                Cleaners[i].SetActive(true);
            }
            Instance = this;
            if (interactAction == null || interactAction.bindings.Count == 0)
            {
                interactAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + InteractingKey.ToString());
                interactAction.AddBinding("<Gamepad>/buttonSouth");
            }
            interactAction.Enable();

            if (sprintAction == null || sprintAction.bindings.Count == 0)
            {
                sprintAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/leftShift");
                sprintAction.AddBinding("<Gamepad>/leftStickPress");
            }
            sprintAction.Enable();

            if (jumpAction == null || jumpAction.bindings.Count == 0)
            {
                jumpAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/space");
                jumpAction.AddBinding("<Gamepad>/buttonEast");
            }
            jumpAction.Enable();

            if (selectionAction == null || selectionAction.bindings.Count == 0)
            {
                selectionAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + SelectionKey.ToString());
                selectionAction.AddBinding("<Gamepad>/buttonWest");
            }
            selectionAction.Enable();

            if (grabbingAction == null || grabbingAction.bindings.Count == 0)
            {
                grabbingAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + GrabbingKey.ToString());
                grabbingAction.AddBinding("<Gamepad>/rightTrigger");
            }
            grabbingAction.Enable();

            if (relocatingAction == null || relocatingAction.bindings.Count == 0)
            {
                relocatingAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/" + RelocatingKey.ToString());
                relocatingAction.AddBinding("<Gamepad>/leftShoulder");
            }
            relocatingAction.Enable();

            if (mapAction == null || mapAction.bindings.Count == 0)
            {
                mapAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + MapKey.ToString());
                mapAction.AddBinding("<Gamepad>/buttonNorth");
            }
            mapAction.Enable();

            if (moveAction == null || moveAction.bindings.Count == 0)
            {
                moveAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
            }
            moveAction.Enable();

            if (mouseAction == null || mouseAction.bindings.Count == 0)
            {
                mouseAction = new InputAction(type: InputActionType.Button);
                mouseAction.AddBinding("<Mouse>/leftButton");
                mouseAction.AddBinding("<Touchscreen>/primaryTouch/press");
                mouseAction.AddBinding("<Gamepad>/rightTrigger");
            }
            mouseAction.Enable();

            if (mouseRightAction == null || mouseRightAction.bindings.Count == 0)
            {
                mouseRightAction = new InputAction(type: InputActionType.Button);
                mouseRightAction.AddBinding("<Mouse>/rightButton");
                mouseRightAction.AddBinding("<Gamepad>/leftTrigger");
            }
            mouseRightAction.Enable();

            if (escapeAction == null || escapeAction.bindings.Count == 0)
            {
                escapeAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/escape");
                escapeAction.AddBinding("<Gamepad>/start");
            }
            escapeAction.Enable();
        }
        public void UpdatePlayerValues()
        {
            playerValues.Money = SaveLoadManager.Instance.GetMoney();
            playerValues.Experience = SaveLoadManager.Instance.GetExperience();
            playerValues.Name = SaveLoadManager.Instance.GetProfileName();
            playerValues.Health = HeroPlayerScript.Instance.Health;
        }

        public void CheckTheRate()
        {
            InventoryManager.Instance.CheckCurrentEquipmentList();
            List<Poster> allPosters = InventoryManager.Instance.CurrentEquipmentList.Where(x => x.GetComponent<Poster>() != null).Select(x => x.GetComponent<Poster>()).ToList();
            float postersTotalAttraction = 0;
            for (int i = 0; i < allPosters.Count; i++)
            {
                postersTotalAttraction = postersTotalAttraction + allPosters[i].CustomerAttractionPoint;
            }

            NPCShoppingPercentageRate = BaseNPCShoppingPercentageRate + Mathf.RoundToInt(postersTotalAttraction);
            if (NPCShoppingPercentageRate > 100) NPCShoppingPercentageRate = 100;
        }

        public void Set_ProfileName(string Name)
        {
            SaveLoadManager.Instance.SetProfileName(Name);
            UpdatePlayerValues();
        }

        public void Spend(int price)
        {
            SaveLoadManager.Instance.SubtractMoney(price);
            GameCanvas.Instance.UpdateStatus();
            AudioManager.Instance.Play_audioClip_Coin();
            GameCanvas.Instance.Show_SpendGet(price*-1);
        }

        public void CreateDust(Transform t)
        {
            for (int i = 0; i < dusts.Count; i++)
            {
                if (dusts[i] == null)
                {
                    dusts.RemoveAt(i);
                    i--;
                }
            }
            if (dusts.Count < 5)
            {
                GameObject dd = Instantiate(DustPrefabs[UnityEngine.Random.Range(0, DustPrefabs.Length)], new Vector3(t.position.x, t.position.y + 0.05f, t.position.z), Quaternion.identity);
                dusts.Add(dd);
            }
        }

        void Start()
        {
            UpdatePlayerValues();
            GameCanvas.Instance.Crosshair.SetActive(true);
            if(controllerType == ControllerType.Mobile)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            GameCanvas.Instance.Text_Cashier_Amount.text = cashierHiredAmount.ToString();
            GameCanvas.Instance.Text_Cleaner_Amount.text = cleanerHiredAmount.ToString();
            MailBox.SetActive(isPayingBillsSystemActivated);

            DailyStart();
            InventoryManager.Instance.LoadBuiltObjects();
            InventoryManager.Instance.LoadSellableObjects();
            StartCoroutine(InventoryManager.Instance.ArrangeEverything());
            Application.targetFrameRate = 60;
            BaseNPCShoppingPercentageRate = NPCShoppingPercentageRate;
            if(postersAmountCanEffectNPCShoppingPercentageRate)
            {
                InvokeRepeating("CheckTheRate", 5, 5);
            }
        }


        public void DailyStart()
        {
            CameraMain.SetActive(true);
            GameCanvas.Instance.image_Blinking.gameObject.SetActive(true);
            GameCanvas.Instance.image_Blinking.GetComponent<Animation>().Play();
            FirstPersonController.Instance.enabled = true;
            CameraScript.Instance.enabled = true;
        }

        public void Get(CollactableType type, int amount)
        {
            if (type == CollactableType.Money)
            {
                SaveLoadManager.Instance.AddMoney(amount);
                PlayerPrefs.Save();
                AudioManager.Instance.Play_audioClip_Coin();
                GameCanvas.Instance.UpdateStatus();
                GameCanvas.Instance.Show_SpendGet(amount);

                UpdatePlayerValues();
                int lastRecorded = SaveLoadManager.Instance.GetLastXPEarnedMoney();
                if (playerValues.Money >= lastRecorded + EarnXPPerEarnings)
                {
                    Get(CollactableType.Experience, 1);
                    SaveLoadManager.Instance.SetLastXPEarnedMoney(playerValues.Money);
                }
            }
            else if (type == CollactableType.Experience)
            {
                SaveLoadManager.Instance.AddExperience(amount);
                AudioManager.Instance.Play_audioClip_Experience();
                GameCanvas.Instance.UpdateStatus();
                GameCanvas.Instance.Show_SpendGet(amount);
            }
        }
    }

    public enum CollactableType
    {
        Money,
        Experience
    }

    public class PlayerValues
    {
        public float Health;
        public int Money;
        public string Name;
        public int Experience;
    }

    public enum Mode
    {
        Free,
        InInventoryLocating,
        InMechanicSelecting,
        ReadingNote,
        InSellerList,
        HoldingForOrdering
    }

    public enum ControllerType
    {
        PC,
        Mobile
    }

    public enum GivingChangeOption
    {
        Keyboard,
        Banknotes,
        Both
    }
}
