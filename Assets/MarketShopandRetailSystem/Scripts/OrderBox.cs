using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class OrderBox : MonoBehaviour
    {
        public bool isHolding = false;
        public Rigidbody rigidbody;
        private Vector3 initialSize;
        public GameObject sellableObjectPrefab;
        public ArrayableArea arrayableArea;
        public Animation animation;
        public Collider collider;

        public SpriteRenderer spriteProduct;
        public TMPro.TMP_Text textCount;

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            initialSize = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            if (sellableObjectPrefab != null)
            {
                for (int i = 0; i < arrayableArea.PlacablePoints.Length; i++)
                {
                    GameObject item = Instantiate(sellableObjectPrefab, arrayableArea.PlacablePoints[i].transform.position, Quaternion.identity, transform);
                    item.transform.localEulerAngles = Vector3.zero;
                    arrayableArea.PlacablePoints[i].objectToGrab = item;
                    arrayableArea.PlacablePoints[i].isAvailable = false;
                    item.GetComponent<SellableObject>().isAvailable = false;
                }
                StartCoroutine(CheckTheSlots(1));
                spriteProduct.sprite = sellableObjectPrefab.GetComponent<SellableObject>().sprite;
                textCount.text = arrayableArea.PlacablePoints.Count().ToString();
            }
        }

        public void AddItemBack()
        {
            if (sellableObjectPrefab != null)
            {
                var availableEmptySlot = arrayableArea.PlacablePoints.Where(x => x.isAvailable && x.objectToGrab == null).FirstOrDefault();
                GameObject item = Instantiate(sellableObjectPrefab, availableEmptySlot.transform.position, Quaternion.identity, transform);
                item.transform.localEulerAngles = Vector3.zero;
                availableEmptySlot.objectToGrab = item;
                availableEmptySlot.isAvailable = false;
                item.GetComponent<SellableObject>().isAvailable = false;
                StartCoroutine(CheckTheSlots(1));
                spriteProduct.sprite = sellableObjectPrefab.GetComponent<SellableObject>().sprite;
                textCount.text = arrayableArea.PlacablePoints.Count().ToString();
                SaveLoadManager.Instance.SetShelfSlotStatus(
                    GetComponent<PhysicalEquipmentDetails>().Name,
                    GetComponent<PhysicalEquipmentDetails>().equipmentIndex,
                    Array.IndexOf(arrayableArea.PlacablePoints, availableEmptySlot),
                    0
                );
            }
        }

        public void DestroyTheOrderBox()
        {
            DestroyTheOrderBox(0.5f);
        }

        public void DestroyTheOrderBox(float destroyDelay)
        {
            InventoryManager.Instance.RemovefromSystem(GetComponent<PhysicalEquipmentDetails>().equipmentIndex, GetComponent<PhysicalEquipmentDetails>().Name);
            InventoryManager.Instance.CurrentEquipmentList.Remove(gameObject);
            textCount.text = "0";
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }

        public IEnumerator CheckTheSlots(int time)
        {
            yield return new WaitForSeconds(time);
            // Let's check the slots:
            var anySellableProduct = arrayableArea.PlacablePoints.Where(x => !x.isAvailable).FirstOrDefault();
            if (anySellableProduct == null)
            {
                textCount.text = "0";
            }
            else
            {
                textCount.text = arrayableArea.PlacablePoints.Where(x => !x.isAvailable).Count().ToString();
            }
        }

        public bool isEmpty()
        {
            var anySellableProduct = arrayableArea.PlacablePoints.Where(x => !x.isAvailable).FirstOrDefault();
            if (anySellableProduct != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Interact()
        {
            if (!isHolding && !HeroPlayerScript.Instance.isHoldingBox)
            {
                isHolding = true;
                HeroPlayerScript.Instance.isHoldingBox = true;
                InventoryManager.Instance.CurrentOrderBox = this;
                if(AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
                {
                    GameCanvas.Instance.Button_GetProductBackToOrderBox.SetActive(true);
                }
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                animation["Open"].time = 0f;
                animation["Open"].speed = 1;
                animation.Play();
                transform.parent = CameraScript.Instance.transform;
                transform.GetComponent<BoxCollider>().enabled = false;
                transform.localRotation = new Quaternion(0, 0, 0, 0);
                transform.localPosition = new Vector3(0, -0.35f, 0.4f);
                transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
                AdvancedGameManager.Instance.CurrentMode = Mode.HoldingForOrdering;
                AudioManager.Instance.Play_Item_Grab();
            }
            else if (isHolding && HeroPlayerScript.Instance.isHoldingBox)
            {
                if (TriggerScript.Instance.placablePoints.Count == 0)
                {
                    isHolding = false;
                    if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
                    {
                        GameCanvas.Instance.Button_GetProductBackToOrderBox.SetActive(false);
                    }
                    transform.localScale = new Vector3(initialSize.x, initialSize.y, initialSize.z);
                    HeroPlayerScript.Instance.isHoldingBox = false;
                    InventoryManager.Instance.CurrentOrderBox = null;
                    transform.localScale = Vector3.one;
                    rigidbody.isKinematic = false;
                    animation.Stop();
                    animation["Open"].time = 0f;
                    animation["Open"].speed = 0f;
                    animation.Play();
                    transform.GetComponent<BoxCollider>().enabled = true;
                    rigidbody.useGravity = true;
                    HeroPlayerScript.Instance.RemovingBox();
                    rigidbody.AddForce(HeroPlayerScript.Instance.GetBoxThrowDirection() * HeroPlayerScript.Instance.GetBoxThrowForce(), ForceMode.Impulse);
                    AdvancedGameManager.Instance.CurrentMode = Mode.Free;
                    transform.parent = null;
                    AudioManager.Instance.Play_Item_Grab();
                    StartCoroutine(SavetoSystemNewLocation());
                }
            }
        }

        public IEnumerator SavetoSystemNewLocation()
        {
            yield return new WaitForSeconds(2);
            PhysicalEquipmentDetails d = GetComponent<PhysicalEquipmentDetails>();
            int objectID = d.equipmentIndex;
            
            // Update the placed object's position
            var placedObject = SaveLoadManager.Instance.GetPlacedObject(d.Name, objectID);
            if (placedObject != null)
            {
                placedObject.position = new Vector3Data(transform.position);
                placedObject.rotation = new Vector3Data(transform.eulerAngles);
                SaveLoadManager.Instance.SaveGame(); // Save to file
            }
        }
    }
}
