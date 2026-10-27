using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

namespace MarketShopandRetailSystem
{
    public class HeroPlayerScript : MonoBehaviour
    {
        public static HeroPlayerScript Instance;

        public FirstPersonController firstPersonController;
        public CharacterController characterController;
        public float Health = 100;
        [HideInInspector]
        public float TotalHealth = 100;
        public float Stamina = 1000;
        [HideInInspector]
        public float TotalStamina = 1000;
        public List<int> Keys_Grabbed = new List<int>();
        [HideInInspector]
        public GameObject FPSHands;

        [HideInInspector]
        public bool isHoldingBox = false;
        [Header("Box throwing")]
        [Min(0f)]
        public float baseBoxThrowForce = 3f;
        [Min(0f)]
        public float maxBoxThrowForce = 12f;
        [Min(0f)]
        [Tooltip("Upward direction factor relative to the camera: 0 = straight, 1 = 45 degrees upward. Actual height also depends on force and gravity.")]
        public float boxThrowHeight = 1f;
        [Min(0.01f)]
        [Tooltip("Seconds of charging after a 0.2 second tap window to reach maximum throw force.")]
        public float boxThrowChargeTime = 1.5f;
        private const float BoxThrowTapDuration = 0.2f;
        private ItemScript chargingBox;
        private float boxThrowChargeStartedAt;
        public GameObject MainCamera;
        private int currentKeyIdinHands = -1;
        private bool isHeroBusy = false;
        [HideInInspector]
        public float LastInteractedTime = 0;
        [HideInInspector]
        public ItemScript InteractableObject;
        [HideInInspector]
        public PhysicalEquipmentDetails GrableObject;
        public CapsuleCollider CapsuleCollider;
        public AudioSource AudioSource;
        [HideInInspector]
        public CashRegister currentCashRegister;

        void Start()
        {
            TotalHealth = Health;
            TotalStamina = Stamina;
            Time.timeScale = 1;
            GameCanvas.Instance.UpdateHealth();
            GameCanvas.Instance.UpdateStamina();
            InvokeRepeating("IncreaseStamina", 1, 0.25f);
        }

        public void IncreaseStamina()
        {
            if (Stamina < TotalStamina && Health > 0 && !AdvancedGameManager.Instance.sprintAction.IsPressed())
            {
                Stamina = Stamina + 1;
                GameCanvas.Instance.UpdateStamina();
            }
        }

        public void SetHeroBusy(bool b)
        {
            isHeroBusy = b;
        }

        public bool GetHeroBusy()
        {
            return isHeroBusy;
        }

        public int GetCurrentKey()
        {
            return currentKeyIdinHands;
        }

        private void Awake()
        {
            Instance = this;
        }

        public void Grab_Key(int ID)
        {
            Keys_Grabbed.Add(ID);
        }

        public void Heal()
        {
            Health = Health + 50;
            if (Health > TotalHealth) Health = 100;
            GameCanvas.Instance.UpdateHealth();
        }

        public void Rest()
        {
            Stamina = Stamina + 50;
            if (Stamina > TotalStamina) Stamina = 100;
            GameCanvas.Instance.UpdateStamina();
        }

        public void ActivatePlayer()
        {
            transform.eulerAngles = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            firstPersonController.enabled = true;
            characterController.enabled = true;
            transform.eulerAngles = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
        }

        public void DeactivatePlayer()
        {
            firstPersonController.enabled = false;
            characterController.enabled = false;
        }

        public void ChangeTag(bool hide)
        {
            if (hide)
            {
                gameObject.tag = "Untagged";
            }
            else
            {
                gameObject.tag = "Player";
            }
        }

        public void ActivatePlayerInputs()
        {
            firstPersonController.enabled = true;
            characterController.enabled = true;
        }

        public void RemovingBox()
        {
            StartCoroutine(RemovingBoxNow());
        }

        IEnumerator RemovingBoxNow()
        {
            HeroPlayerScript.Instance.CapsuleCollider.enabled = false;
            HeroPlayerScript.Instance.characterController.enabled = false;
            yield return new WaitForSeconds(0.2f);
            HeroPlayerScript.Instance.CapsuleCollider.enabled = true;
            HeroPlayerScript.Instance.characterController.enabled = true;
        }

        public LayerMask LayersToCheck;

        public bool isButtonInteractHeld = false;
        public bool isButtonGettingItemBackInteractHeld = false;

        public void PointerUp()
        {
            isButtonInteractHeld = false;
            if (chargingBox != null && chargingBox == InteractableObject && IsHoldingInteractableBox())
            {
                Interact();
            }
            chargingBox = null;
        }

        public void PointerDown()
        {
            isButtonInteractHeld = true;
            BeginBoxThrowCharge();
        }

        private bool IsHoldingInteractableBox()
        {
            return isHoldingBox && InteractableObject != null
                && (InteractableObject.itemType == ItemType.Box || InteractableObject.itemType == ItemType.OrderBox);
        }

        private void BeginBoxThrowCharge()
        {
            chargingBox = IsHoldingInteractableBox() ? InteractableObject : null;
            boxThrowChargeStartedAt = Time.time;
        }

        private void OnDisable()
        {
            chargingBox = null;
            isButtonInteractHeld = false;
        }

        public float GetBoxThrowForce()
        {
            float baseForce = Mathf.Max(0f, baseBoxThrowForce);
            if (chargingBox == null || chargingBox != InteractableObject)
            {
                return baseForce;
            }

            float charge = Mathf.Clamp01((Time.time - boxThrowChargeStartedAt - BoxThrowTapDuration)
                / Mathf.Max(0.01f, boxThrowChargeTime));
            return Mathf.Lerp(baseForce, Mathf.Max(baseForce, maxBoxThrowForce), charge);
        }

        public Vector3 GetBoxThrowDirection()
        {
            Transform cameraTransform = CameraScript.Instance.transform;
            return (cameraTransform.forward + cameraTransform.up * Mathf.Max(0f, boxThrowHeight)).normalized;
        }

        public void PointerUpForGettingItemBack()
        {
            isButtonGettingItemBackInteractHeld = false;
        }

        public void PointerDownForGettingItemBack()
        {
            isButtonGettingItemBackInteractHeld = true;
        }


        public void Interact()
        {
            if (InteractableObject != null)
            {
                if (InteractableObject.interactionType == InteractionType.Clean || InteractableObject.interactionType == InteractionType.Build)
                {
                    if (InteractableObject.NeededHandType == Hand_Type.Any || InteractableObject.NeededHandType == FPSHandRotator.Instance.Current_HandType)
                    {
                        if (Time.time > InteractableObject.lastTime + 0.25f)
                        {
                            InteractableObject.lastTime = Time.time;
                            InteractableObject.imageToFill.fillAmount = InteractableObject.imageToFill.fillAmount - (1f / InteractableObject.GetComponent<ItemToMaintainScript>().durationForMaintain);
                            InteractableObject.UpdateSprite();
                            HeroPlayerScript.Instance.SetHeroBusy(true);
                            FPSHandRotator.Instance.AnimateHand(InteractableObject.interactionType);
                            if (InteractableObject.imageToFill.fillAmount <= 0)
                            {
                                InteractableObject.Interact();
                                if (FPSHandRotator.Instance.Current_HandType == Hand_Type.Build)
                                {
                                    FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
                                }
                            }
                        }
                        InteractableObject.imageToFill.gameObject.SetActive(true);
                    }
                    else
                    {
                        GameCanvas.Instance.Show_Warning_Not(InteractableObject.NeededHandType.ToString() + " Needed!", false);
                    }
                }
                else
                {
                    if (InteractableObject.itemType == ItemType.Box && InteractableObject.GetComponent<BoxScript>().isHolding)
                    {
                        if (InteractableObject.NeededHandType == Hand_Type.Any || InteractableObject.NeededHandType == FPSHandRotator.Instance.Current_HandType)
                        {
                            InteractableObject.Interact();
                        }
                        else
                        {
                            GameCanvas.Instance.Show_Warning_Not(InteractableObject.NeededHandType.ToString() + " Needed!", false);
                        }
                    }
                    else
                    {
                        if (InteractableObject.NeededHandType == Hand_Type.Any || InteractableObject.NeededHandType == FPSHandRotator.Instance.Current_HandType)
                        {
                            if (!InteractableObject.isInteracted)
                            {
                                InteractableObject.Interact();
                                FPSHandRotator.Instance.AnimateHand(InteractableObject.interactionType);
                            }
                        }
                        else
                        {
                            GameCanvas.Instance.Show_Warning_Not(InteractableObject.NeededHandType.ToString() + " Needed!", false);
                        }
                    }
                }
            }
        }

        public void GrabBack()
        {
            GrabTheItemBackToInventory(GrableObject.GetComponent<PhysicalEquipmentDetails>());
            GameCanvas.Instance.Hide_GrabbingHint();
        }

        private void Update()
        {
            if (!IsHoldingInteractableBox() || chargingBox != InteractableObject || Time.timeScale == 0f)
            {
                chargingBox = null;
            }
            if (AdvancedGameManager.Instance.controllerType != ControllerType.Mobile
                && AdvancedGameManager.Instance.interactAction.WasPressedThisFrame())
            {
                BeginBoxThrowCharge();
            }
            else if (AdvancedGameManager.Instance.controllerType != ControllerType.Mobile
                && !AdvancedGameManager.Instance.interactAction.IsPressed()
                && !AdvancedGameManager.Instance.interactAction.WasReleasedThisFrame())
            {
                chargingBox = null;
            }

            if (Camera.main != null)
            {
                if (currentCashRegister != null)
                {
                    if (AdvancedGameManager.Instance.mouseAction.WasPressedThisFrame())
                    {
                        Vector2 touchPosition;
                        if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
                        {
                            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                            {
                                touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                            }
                            else
                            {
                                touchPosition = Vector2.zero;
                            }
                        }
                        else // PC
                        {
                            touchPosition = Mouse.current.position.ReadValue();
                        }

                        Ray ray2 = Camera.main.ScreenPointToRay(touchPosition);
                        if (Physics.Raycast(ray2, out RaycastHit hit, Mathf.Infinity, LayersToCheck))
                        {
                            // We are sitting. Let's check for any item.
                            if (hit.collider.CompareTag("Sellable"))
                            {
                                SellableObject o = hit.collider.GetComponent<SellableObject>();
                                if (o.enabled)
                                {
                                    // Let's take this and add to price
                                    currentCashRegister.Scan(o);
                                    return;
                                }
                            }
                            // We are sitting. Let's check for any item.
                            else if (hit.collider.CompareTag("Key"))
                            {
                                KeyboardKey o = hit.collider.GetComponent<KeyboardKey>();
                                o.Click();
                                return;
                            }
                            else if (hit.collider.CompareTag("Banknote"))
                            {
                                BanknoteScript o = hit.collider.GetComponent<BanknoteScript>();
                                o.Select_Banknote();
                                return;
                            }
                        }
                    }
                }


                Ray ray = Camera.main.ScreenPointToRay(GameCanvas.Instance.Crosshair.transform.position);
                RaycastHit other;
                if (Physics.Raycast(ray, out other, 2, LayersToCheck) && !isHoldingBox)
                {
                    // Let's check that is it an Inventory Equipment or Not
                    if (other.collider.CompareTag("Item") && other.collider.GetComponent<PhysicalEquipmentDetails>() != null && other.collider.GetComponent<ObjectPlacing>() == null && other.collider.GetComponent<OrderBox>() == null)
                    {
                        if (GrableObject == null || other.collider.GetComponent<PhysicalEquipmentDetails>() != GrableObject)
                        {
                            GrableObject = other.collider.GetComponent<PhysicalEquipmentDetails>();
                            CashRegister c = other.collider.GetComponent<CashRegister>();
                            if (c == null || !c.isBusy)
                            {
                                GameCanvas.Instance.Show_GrabbingHint();
                            }
                        }
                        if (AdvancedGameManager.Instance.grabbingAction.WasPressedThisFrame())
                        {
                            GrabTheItemBackToInventory(other.collider.GetComponent<PhysicalEquipmentDetails>());
                            GameCanvas.Instance.Hide_GrabbingHint();
                        }
                    }

                    if ((other.collider.CompareTag("Item") || other.collider.CompareTag("Character")) && other.collider.GetComponent<ObjectPlacing>() == null)
                    {
                        var Item = other.collider.GetComponent<ItemScript>();
                        if (Item != null && Item.isInteracted) return;

                        if (Item == null || !Item.enabled) return;

                        if (InteractableObject != null && InteractableObject != Item)
                        {
                        }
                        InteractableObject = Item;

                        if (!isHoldingBox)
                        {
                            GameCanvas.Instance.Show_Warning(Item);
                        }
                    }
                }
                else
                {
                    if (GrableObject != null)
                    {
                        GrableObject = null;
                        GameCanvas.Instance.Hide_GrabbingHint();
                    }
                    if (InteractableObject != null)
                    {
                        if ((InteractableObject.CompareTag("Item") || InteractableObject.CompareTag("Character")))
                        {
                            if (!InteractableObject.enabled) return;

                            GameCanvas.Instance.Hide_Warning();
                            if ((InteractableObject.interactionType == InteractionType.Clean || InteractableObject.interactionType == InteractionType.Build) && InteractableObject.GetComponent<ItemToMaintainScript>() != null)
                            {
                                InteractableObject.imageToFill.fillAmount = 1;
                                InteractableObject.imageToFill.gameObject.SetActive(false);
                            }
                        }
                        if (!isHoldingBox)
                        {
                            InteractableObject = null;
                        }
                    }
                }

                if (InteractableObject == null)
                {
                    GameCanvas.Instance.Hide_Warning();
                }
            }
            if (InteractableObject != null)
            {
                if (InteractableObject.interactionType == InteractionType.Clean || InteractableObject.interactionType == InteractionType.Build)
                {
                    if (AdvancedGameManager.Instance.interactAction.IsPressed())
                    {
                        if (InteractableObject.NeededHandType == Hand_Type.Any || InteractableObject.NeededHandType == FPSHandRotator.Instance.Current_HandType)
                        {
                            if (Time.time > InteractableObject.lastTime + 0.25f)
                            {
                                InteractableObject.lastTime = Time.time;
                                InteractableObject.imageToFill.fillAmount = InteractableObject.imageToFill.fillAmount - (1f / InteractableObject.GetComponent<ItemToMaintainScript>().durationForMaintain);
                                InteractableObject.UpdateSprite();
                                HeroPlayerScript.Instance.SetHeroBusy(true);
                                FPSHandRotator.Instance.AnimateHand(InteractableObject.interactionType);
                                if (InteractableObject.imageToFill.fillAmount <= 0)
                                {
                                    InteractableObject.Interact();
                                    if (FPSHandRotator.Instance.Current_HandType == Hand_Type.Build)
                                    {
                                        FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
                                    }
                                }
                            }
                            InteractableObject.imageToFill.gameObject.SetActive(true);
                        }
                        else
                        {
                            GameCanvas.Instance.Show_Warning_Not(InteractableObject.NeededHandType.ToString() + " Needed!", false);
                        }
                    }
                    else if (AdvancedGameManager.Instance.mouseAction.WasReleasedThisFrame())
                    {
                        InteractableObject.imageToFill.fillAmount = 1;
                        InteractableObject.imageToFill.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (AdvancedGameManager.Instance.interactAction.WasReleasedThisFrame())
                    {
                        if (InteractableObject.itemType == ItemType.Box && InteractableObject.GetComponent<BoxScript>().isHolding)
                        {
                            if (InteractableObject.NeededHandType == Hand_Type.Any || InteractableObject.NeededHandType == FPSHandRotator.Instance.Current_HandType)
                            {
                                InteractableObject.Interact();
                            }
                            else
                            {
                                GameCanvas.Instance.Show_Warning_Not(InteractableObject.NeededHandType.ToString() + " Needed!", false);
                            }
                        }
                        else
                        {
                            if (InteractableObject.NeededHandType == Hand_Type.Any || InteractableObject.NeededHandType == FPSHandRotator.Instance.Current_HandType)
                            {
                                if (!InteractableObject.isInteracted)
                                {
                                    InteractableObject.Interact();
                                    FPSHandRotator.Instance.AnimateHand(InteractableObject.interactionType);
                                }
                            }
                            else
                            {
                                GameCanvas.Instance.Show_Warning_Not(InteractableObject.NeededHandType.ToString() + " Needed!", false);
                            }
                        }
                    }
                }
            }
            if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile && isButtonInteractHeld
                && !IsHoldingInteractableBox())
            {
                Interact();
            }
            if (AdvancedGameManager.Instance.controllerType != ControllerType.Mobile
                && AdvancedGameManager.Instance.interactAction.WasReleasedThisFrame())
            {
                chargingBox = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Item") && other.GetComponent<CashRegister>() != null)
            {
                currentCashRegister = other.GetComponent<CashRegister>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Item") && other.GetComponent<CashRegister>() != null)
            {
                currentCashRegister = null;
            }
        }

        public void GrabTheItemBackToInventory(PhysicalEquipmentDetails itemDetail)
        {
            var placeablePoints = itemDetail.gameObject.GetComponentsInChildren<PlacablePoint>();
            if (placeablePoints != null && placeablePoints.Where(x => x.objectToGrab != null).FirstOrDefault() != null)
            {
                GameCanvas.Instance.Show_Warning_Not("There are still products on the Equipment. You can't grab it yet.", false);
                return;
            }
            AudioManager.Instance.Play_Item_Grab();
            itemDetail.NotifyAllSellablesIfYouHaveAny();
            InventoryManager.Instance.Recover(itemDetail.Name);
            InventoryManager.Instance.CurrentEquipmentList.Remove(itemDetail.gameObject);
            InventoryManager.Instance.RemovefromSystem(itemDetail.equipmentIndex, itemDetail.Name);
            Destroy(itemDetail.gameObject);
        }
    }
}
