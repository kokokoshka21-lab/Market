using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class OrderBox : MonoBehaviour
    {
        public bool isHolding = false;
        public Rigidbody rigidbody;
        private Vector3 initialSize;
        [Tooltip("Initial shipment product only. Empty boxes accept any product; filled boxes accept only the same product.")]
        public GameObject sellableObjectPrefab;
        public bool ContentsReady { get; private set; }
        public ArrayableArea arrayableArea;
        public Animation animation;
        public Collider collider;

        public SpriteRenderer spriteProduct;
        public TMPro.TMP_Text textCount;

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            initialSize = transform.localScale;
            if (arrayableArea == null) arrayableArea = GetComponent<ArrayableArea>();
            PhysicalEquipmentDetails detail = GetComponent<PhysicalEquipmentDetails>();
            PlacedObjectData saved = SaveLoadManager.Instance.GetPlacedObject(detail.Name, detail.equipmentIndex);
            var contents = new List<BoxSlotData>();
            if (saved != null && saved.hasBoxContents)
            {
                if (saved.boxContents != null) contents.AddRange(saved.boxContents);
            }
            else if (sellableObjectPrefab != null)
            {
                // Migrate old saves once; a saved empty box must never refill from its shipment prefab.
                string productName = sellableObjectPrefab.GetComponent<SellableObject>().Name;
                for (int i = 0; i < arrayableArea.PlacablePoints.Length; i++)
                    if (SaveLoadManager.Instance.GetShelfSlotStatus(detail.Name, detail.equipmentIndex, i, 0) != -1)
                        contents.Add(new BoxSlotData { slotIndex = i, productName = productName });
            }
            var used = new HashSet<int>();
            foreach (BoxSlotData slot in contents)
            {
                if (slot.slotIndex < 0 || slot.slotIndex >= arrayableArea.PlacablePoints.Length
                    || !used.Add(slot.slotIndex) || FindProductPrefab(slot.productName) == null)
                {
                    Debug.LogError("Cannot restore box contents: invalid slot or missing product " + slot.productName);
                    if (textCount != null) textCount.text = "?";
                    return; // Preserve the save rather than replacing an unknown product with another type.
                }
            }
            foreach (PlacablePoint point in arrayableArea.PlacablePoints)
            {
                point.objectToGrab = null;
                point.isAvailable = true;
            }
            foreach (BoxSlotData slot in contents)
            {
                PlacablePoint point = arrayableArea.PlacablePoints[slot.slotIndex];
                GameObject prefab = FindProductPrefab(slot.productName);
                GameObject item = Instantiate(prefab, point.transform.position,
                    transform.rotation, transform);
                item.transform.localScale = prefab.transform.localScale;
                item.transform.localRotation = Quaternion.identity;
                SellableObject product = item.GetComponent<SellableObject>();
                product.isAvailable = false;
                product.putPoint = null;
                product.sellableObjectIndex = -1;
                point.objectToGrab = item;
                point.isAvailable = false;
            }
            ContentsReady = true;
            RefreshContentsDisplay();
            if (saved != null && !saved.hasBoxContents) SaveContents();
        }

        private GameObject FindProductPrefab(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return null;
            SellableObject product = InventoryManager.Instance != null && InventoryManager.Instance.SellableObjects != null
                ? InventoryManager.Instance.SellableObjects.FirstOrDefault(x => x != null && x.Name == productName) : null;
            if (product != null) return product.gameObject;
            return sellableObjectPrefab != null && sellableObjectPrefab.GetComponent<SellableObject>().Name == productName
                ? sellableObjectPrefab : null;
        }

        public bool SaveContents()
        {
            if (!ContentsReady) return false;
            var contents = new List<BoxSlotData>();
            for (int i = 0; i < arrayableArea.PlacablePoints.Length; i++)
            {
                GameObject item = arrayableArea.PlacablePoints[i].objectToGrab;
                if (item != null) contents.Add(new BoxSlotData { slotIndex = i, productName = item.GetComponent<SellableObject>().Name });
            }
            PhysicalEquipmentDetails detail = GetComponent<PhysicalEquipmentDetails>();
            return SaveLoadManager.Instance.SaveBoxContents(detail.Name, detail.equipmentIndex, contents);
        }

        public bool CanStoreProduct(SellableObject product)
        {
            if (!ContentsReady || product == null || FindProductPrefab(product.Name) == null) return false;
            return arrayableArea.PlacablePoints.All(point => point.objectToGrab == null
                || point.objectToGrab.GetComponent<SellableObject>().Name == product.Name);
        }

        public Vector3 GetProductScale(SellableObject product)
        {
            GameObject prefab = FindProductPrefab(product.Name);
            return prefab != null ? prefab.transform.localScale : Vector3.one;
        }

        public void RefreshContentsDisplay()
        {
            SellableObject[] contents = arrayableArea.PlacablePoints.Where(x => x.objectToGrab != null)
                .Select(x => x.objectToGrab.GetComponent<SellableObject>()).Where(x => x != null).ToArray();
            if (textCount != null) textCount.text = contents.Length.ToString();
            if (spriteProduct != null)
            {
                bool homogeneous = contents.Length > 0 && contents.All(x => x.Name == contents[0].Name);
                spriteProduct.enabled = homogeneous;
                spriteProduct.sprite = homogeneous ? contents[0].sprite : null;
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
            RefreshContentsDisplay();
        }

        public bool isEmpty()
        {
            return arrayableArea.PlacablePoints.All(x => x.objectToGrab == null);
        }

        public void Interact()
        {
            if (!ContentsReady) return;
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
                    float throwForce = HeroPlayerScript.Instance.GetBoxThrowForce();
                    HeroPlayerScript.Instance.RemovingBox();
                    rigidbody.AddForce(HeroPlayerScript.Instance.GetBoxThrowDirection() * throwForce, ForceMode.Impulse);
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
