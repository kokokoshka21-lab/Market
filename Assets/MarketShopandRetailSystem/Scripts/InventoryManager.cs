using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.InputSystem;

namespace MarketShopandRetailSystem
{
    public class InventoryManager : MonoBehaviour
    {
        public List<PhysicalEquipmentDetails> Equipments;
        [HideInInspector]
        public List<EquipmentScript> Inventories;
        public List<SellableObject> SellableObjects;

        public OrderBox CurrentOrderBox;
        public GameObject EquipmentUIPrefab;
        public GameObject Container_Decoration;
        public GameObject Container_Poster;
        public GameObject Container_Sellables;

        public static InventoryManager Instance;
        public LayerMask layermask;

        [HideInInspector]
        public GameObject CreatedObject;

        public GameObject Panel_LocatingInfo;
        private EquipmentScript currentEquipment;
        public GameObject shop;
        [HideInInspector]
        public List<GameObject> CurrentEquipmentList;
        public InventoryMode inventoryMode;

        private void Awake()
        {
            Instance = this;
            CurrentEquipmentList = new List<GameObject>();
            LoadAllObjects();
        }

        public void Buy(EquipmentScript equipment)
        {
            GameCanvas.Instance.Hide_Warning();
            AdvancedGameManager.Instance.UpdatePlayerValues();
            if (AdvancedGameManager.Instance.playerValues.Money < equipment.EquipmentDetail.Price)
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Money!", false);
                return;
            }
            if (AdvancedGameManager.Instance.playerValues.Experience < equipment.EquipmentDetail.Experience)
            {
                GameCanvas.Instance.Show_Warning_Not("Insufficent Experience!", false);
                return;
            }
            AdvancedGameManager.Instance.Spend(equipment.EquipmentDetail.Price);
            equipment.CurrentInStorage = equipment.CurrentInStorage + 1;
            SaveLoadManager.Instance.SetInventoryItemAmount(equipment.EquipmentDetail.Name, equipment.CurrentInStorage);
            GameCanvas.Instance.Show_Warning_Not("Added to Inventory!", true);
        }

        public void Recover(string name)
        {
            EquipmentScript equipment = Inventories.Where(x => x.EquipmentDetail.Name == name).FirstOrDefault();
            equipment.CurrentInStorage = equipment.CurrentInStorage + 1;
            SaveLoadManager.Instance.SetInventoryItemAmount(equipment.EquipmentDetail.Name, equipment.CurrentInStorage);
            GameCanvas.Instance.Show_Warning_Not("Added to Inventory!", true);
        }

        public void CheckCurrentEquipmentList()
        {
            for (int i = 0; i < InventoryManager.Instance.CurrentEquipmentList.Count; i++)
            {
                if (InventoryManager.Instance.CurrentEquipmentList[i] == null)
                {
                    InventoryManager.Instance.CurrentEquipmentList.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Use(EquipmentScript equipment)
        {
            if (CreatedObject != null || IsMovingShelf) return;
            if (equipment.CurrentInStorage > 0)
            {
                if (equipment.EquipmentDetail.equipmentType == EquipmentType.Sellable)
                {
                    GameObject o = Instantiate(equipment.EquipmentDetail.gameObject);
                    FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
                    AudioManager.Instance.Play_Item_Grab();
                    o.transform.position = new Vector3(AdvancedGameManager.Instance.incomingOrderBoxPoint.position.x + Random.Range(-2, 2), AdvancedGameManager.Instance.incomingOrderBoxPoint.position.y + 3, AdvancedGameManager.Instance.incomingOrderBoxPoint.position.z + Random.Range(-2, 2));
                    o.layer = LayerMask.NameToLayer("Default");
                    Panel_LocatingInfo.SetActive(false);
                    SavetoSystem(o, equipment);
                    equipment.DecreaseItFromInventory();
                    CurrentEquipmentList.Add(o);
                    LoadMyInventory();
                }
                else
                {
                    FPSHandRotator.Instance.Switch_Hand(Hand_Type.Build);
                    CreatedObject = Instantiate(equipment.EquipmentDetail.gameObject);
                    currentPlacingScript = CreatedObject.GetComponent<ObjectPlacing>();
                    currentPlacingScript.ConfigurePlacementPreview();
                    currentEquipment = equipment;
                    hasPlacementSurface = false;
                    wheelAcc = 0f;
                    Panel_LocatingInfo.SetActive(true);
                    GameCanvas.Instance.Hide_Panel_SellerShop(true);
                    AdvancedGameManager.Instance.CurrentMode = Mode.InInventoryLocating;
                }
            }
        }

        float wheelAcc;
        const float notch = 0.05f;
        const float stepDeg = 45f;

        private ObjectPlacing currentPlacingScript;
        private ShelfRelocation relocatingShelf;
        private bool hasPlacementSurface;
        private bool shelvesReady;
        private int placementEndedFrame = -1;
        private readonly HashSet<Transform> productsInTransit = new HashSet<Transform>();
        public bool IsMovingShelf => relocatingShelf != null;
        public bool IsPlacingThisFrame => AdvancedGameManager.Instance.CurrentMode == Mode.InInventoryLocating
            || Time.frameCount == placementEndedFrame;

        public void BeginMovingShelf(PhysicalEquipmentDetails shelf)
        {
            if (!ShelfRelocation.Supports(shelf) || IsMovingShelf || CreatedObject != null
                || AdvancedGameManager.Instance.CurrentMode != Mode.Free || GameCanvas.Instance.isPaused
                || HeroPlayerScript.Instance.isHoldingBox) return;
            if (!shelvesReady || SaveLoadManager.Instance.GetPlacedObject(shelf.Name, shelf.equipmentIndex) == null)
            {
                GameCanvas.Instance.Show_Warning_Not("Wait until the shelf is loaded and saved.", false);
                return;
            }
            if (shelf.GetComponent<ArrayableArea>().PlacablePoints.Any(point => point.objectToGrab != null
                && (productsInTransit.Contains(point.objectToGrab.transform)
                    || (point.objectToGrab.GetComponent<OrderBox>() != null
                        && point.objectToGrab.GetComponent<OrderBox>().IsMovingToShelf))))
            {
                GameCanvas.Instance.Show_Warning_Not("Wait until the products are placed.", false);
                return;
            }
            relocatingShelf = new ShelfRelocation(shelf);
            try
            {
                currentPlacingScript = relocatingShelf.CreatePreview();
                CreatedObject = relocatingShelf.Preview;
                currentEquipment = null;
                hasPlacementSurface = false;
                wheelAcc = 0f;
                FPSHandRotator.Instance.Switch_Hand(Hand_Type.Build);
                AdvancedGameManager.Instance.CurrentMode = Mode.InInventoryLocating;
                Panel_LocatingInfo.SetActive(true);
                GameCanvas.Instance.Hide_GrabbingHint();
                GameCanvas.Instance.Hide_Warning();
                HeroPlayerScript.Instance.InteractableObject = null;
                HeroPlayerScript.Instance.GrableObject = null;
                GameCanvas.Instance.Show_ShelfMovingHint();
            }
            catch
            {
                EndRelocation(true);
                throw;
            }
        }

        private void EndRelocation(bool cancel)
        {
            try
            {
                if (!cancel && !relocatingShelf.TryCommit(SaveLoadManager.Instance))
                    GameCanvas.Instance.Show_Warning_Not("Could not save the move. The shelf was restored.", false);
            }
            finally
            {
                relocatingShelf.Cancel(); // No-op after a successful commit; rolls back failures.
                relocatingShelf = null;
                ResetPlacementPresentation();
            }
        }

        private void ResetPlacementPresentation(bool freeHands = true)
        {
            CreatedObject = null;
            currentPlacingScript = null;
            currentEquipment = null;
            hasPlacementSurface = false;
            wheelAcc = 0f;
            placementEndedFrame = Time.frameCount;
            if (AdvancedGameManager.Instance != null) AdvancedGameManager.Instance.CurrentMode = Mode.Free;
            if (Panel_LocatingInfo != null) Panel_LocatingInfo.SetActive(false);
            if (GameCanvas.Instance != null)
            {
                GameCanvas.Instance.Hide_GrabbingHint();
                GameCanvas.Instance.Hide_Warning();
            }
            if (HeroPlayerScript.Instance != null)
            {
                // Never discard the held-box reference when a placement is interrupted externally.
                if (!HeroPlayerScript.Instance.isHoldingBox) HeroPlayerScript.Instance.InteractableObject = null;
                HeroPlayerScript.Instance.GrableObject = null;
                HeroPlayerScript.Instance.isButtonInteractHeld = false;
                HeroPlayerScript.Instance.isButtonGettingItemBackInteractHeld = false;
                HeroPlayerScript.Instance.SetHeroBusy(false);
            }
            if (freeHands && FPSHandRotator.Instance != null) FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
        }

        private void OnDisable()
        {
            if (relocatingShelf != null) EndRelocation(true);
        }

        private void UpdatePlacementPreview()
        {
            if (CreatedObject == null || currentPlacingScript == null) { CancelLocating(); return; }
            if (IsMovingShelf && relocatingShelf.Shelf == null) { EndRelocation(true); return; }
            bool poster = currentPlacingScript.GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster;
            RaycastHit? surface = null;
            if (Camera.main != null)
            {
                Vector3 aim = GameCanvas.Instance.Crosshair.transform.position - Vector3.up * HologramOffset;
                RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(aim), 6f, layermask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.transform.IsChildOf(CreatedObject.transform) || hit.collider.CompareTag("Shop")
                        || hit.collider.CompareTag("Player")) continue;
                    if (poster || hit.normal.y >= 0.7f) surface = hit;
                    break; // Do not select a floor through a wall.
                }
            }
            hasPlacementSurface = surface.HasValue;
            if (!hasPlacementSurface)
            {
                CreatedObject.SetActive(false);
                currentPlacingScript.CanLocate = false;
                return;
            }
            CreatedObject.transform.position = surface.Value.point;
            CreatedObject.SetActive(true);
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC && Mouse.current != null)
            {
                wheelAcc += Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(wheelAcc) >= notch)
                {
                    CreatedObject.transform.Rotate(poster ? Vector3.right : Vector3.up, Mathf.Sign(wheelAcc) * stepDeg);
                    wheelAcc = 0f;
                }
            }
            if (AdvancedGameManager.Instance.controllerType == ControllerType.PC
                && AdvancedGameManager.Instance.mouseAction.WasPressedThisFrame()) Click_Button_Locate();
        }

        public float HologramOffset = 10;

        private void Update()
        {
            if (IsMovingShelf && AdvancedGameManager.Instance.CurrentMode != Mode.InInventoryLocating)
            {
                Mode nextMode = AdvancedGameManager.Instance.CurrentMode;
                EndRelocation(true);
                AdvancedGameManager.Instance.CurrentMode = nextMode;
                return;
            }
            if (AdvancedGameManager.Instance.CurrentMode == Mode.InInventoryLocating)
            {
                UpdatePlacementPreview();
            }
            else if (AdvancedGameManager.Instance.CurrentMode == Mode.HoldingForOrdering)
            {
                if(AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
                {
                    if (HeroPlayerScript.Instance.isButtonInteractHeld && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_Locate();
                    }
                    else if (HeroPlayerScript.Instance.isButtonGettingItemBackInteractHeld && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_GetBackToOrderBox();
                    }
                }
                else
                {
                    if (AdvancedGameManager.Instance.mouseAction.IsPressed() && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_Locate();
                    }
                    else if (AdvancedGameManager.Instance.mouseRightAction.IsPressed() && Time.time > lastTimeLocating + 0.15f)
                    {
                        lastTimeLocating = Time.time;
                        Click_Button_GetBackToOrderBox();
                    }
                }
            }
        }

        float lastTimeLocating = 0;

        private IEnumerator TransferProduct(SellableObject product, PlacablePoint destination, bool intoBox, Vector3 scale)
        {
            Transform target = product.transform;
            productsInTransit.Add(target);
            try
            {
                target.SetParent(intoBox ? destination.GetComponentInParent<OrderBox>().transform : null, true);
                target.localScale = scale;
                target.rotation = intoBox ? target.parent.rotation : destination.transform.rotation;
                Vector3 startPosition = target.position;
                float elapsed = 0f;
                while (target != null && destination != null && elapsed < 0.25f)
                {
                    target.position = Vector3.Lerp(startPosition, destination.transform.position, elapsed / 0.25f);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (target != null && destination != null)
                {
                    target.position = destination.transform.position;
                    if (!intoBox && destination.objectToGrab == product.gameObject) product.isAvailable = true;
                }
            }
            finally
            {
                productsInTransit.Remove(target);
            }
        }

        public void Click_Button_Rotate()
        {
            if (CreatedObject == null || currentPlacingScript == null || !CreatedObject.activeInHierarchy) return;
            if (currentPlacingScript.GetComponent<PhysicalEquipmentDetails>().equipmentType == EquipmentType.Poster)
            {
                CreatedObject.transform.Rotate(0.1f * Vector3.right * 100);
            }
            else
            {
                CreatedObject.transform.Rotate(0.1f * Vector3.up * 100);
            }
        }

        public void Click_Button_GetBackToOrderBox()
        {
            if (AdvancedGameManager.Instance.CurrentMode != Mode.HoldingForOrdering
                || CurrentOrderBox == null || !CurrentOrderBox.ContentsReady) return;
            PlacablePoint destination = CurrentOrderBox.arrayableArea.PlacablePoints
                .FirstOrDefault(x => x.objectToGrab == null && x.isAvailable);
            if (destination == null) return;
            PlacablePoint source = TriggerScript.Instance.placablePoints.FirstOrDefault(x => x != null
                && !x.isAvailable && x.objectToGrab != null
                && x.GetComponentInParent<OrderBox>() == null
                && !productsInTransit.Contains(x.objectToGrab.transform)
                && CurrentOrderBox.CanStoreProduct(x.objectToGrab.GetComponent<SellableObject>()));
            if (source == null) return;
            SellableObject product = source.objectToGrab.GetComponent<SellableObject>();
            if (!CurrentOrderBox.CanStoreProduct(product)) return;
            PhysicalEquipmentDetails shelf = source.GetComponentInParent<PhysicalEquipmentDetails>();
            PhysicalEquipmentDetails box = CurrentOrderBox.GetComponent<PhysicalEquipmentDetails>();
            if (shelf == null || !SaveLoadManager.Instance.MoveShelfProductToBox(shelf.Name, shelf.equipmentIndex,
                source.ID, product.Name, product.sellableObjectIndex, box.Name, box.equipmentIndex, destination.ID)) return;

            source.objectToGrab = null;
            source.isAvailable = true;
            destination.objectToGrab = product.gameObject;
            destination.isAvailable = false;
            product.isAvailable = false;
            product.putPoint = null;
            product.sellableObjectIndex = -1;
            CurrentEquipmentList.Remove(product.gameObject);
            if (source.Floor != null) source.Floor.CheckPriceOnTag();
            StartCoroutine(TransferProduct(product, destination, true, CurrentOrderBox.GetProductScale(product)));
            CurrentOrderBox.RefreshContentsDisplay();
            AudioManager.Instance.Play_audioClip_Locate();
        }

        private bool CanStockProduct(SellableObject product, PlacablePoint point)
        {
            if (point == null || !point.isAvailable || point.objectToGrab != null || point.Floor == null
                || point.GetComponentInParent<OrderBox>() != null) return false;
            ArrayableArea area = point.GetComponentInParent<ArrayableArea>();
            return area != null && !area.boxesOnly && area.storageType.HasFlag(product.storageType)
                && !area.PlacablePoints.Any(x => x.Floor == point.Floor && x.objectToGrab != null
                    && (x.objectToGrab.GetComponent<SellableObject>() == null
                        || x.objectToGrab.GetComponent<SellableObject>().Name != product.Name));
        }

        private void StockFromCurrentBox()
        {
            if (CurrentOrderBox == null || !CurrentOrderBox.ContentsReady) return;
            foreach (PlacablePoint source in CurrentOrderBox.arrayableArea.PlacablePoints)
            {
                if (source.objectToGrab == null || productsInTransit.Contains(source.objectToGrab.transform)) continue;
                SellableObject product = source.objectToGrab.GetComponent<SellableObject>();
                if (product == null) continue;
                PlacablePoint destination = TriggerScript.Instance.placablePoints.FirstOrDefault(x => CanStockProduct(product, x));
                if (destination == null) continue; // A later slot in a mixed box may contain a compatible product.
                PhysicalEquipmentDetails shelf = destination.GetComponentInParent<PhysicalEquipmentDetails>();
                PhysicalEquipmentDetails box = CurrentOrderBox.GetComponent<PhysicalEquipmentDetails>();
                if (shelf == null || !SaveLoadManager.Instance.MoveBoxProductToShelf(box.Name, box.equipmentIndex,
                    source.ID, product.Name, shelf.Name, shelf.equipmentIndex, destination.ID,
                    destination.transform.position, destination.transform.eulerAngles, out int productIndex)) return;

                source.objectToGrab = null;
                source.isAvailable = true;
                destination.objectToGrab = product.gameObject;
                destination.isAvailable = false;
                product.sellableObjectIndex = productIndex;
                product.putPoint = destination;
                product.isAvailable = false;
                destination.Floor.SetPriceDetails(product);
                if (!CurrentEquipmentList.Contains(product.gameObject)) CurrentEquipmentList.Add(product.gameObject);
                StartCoroutine(TransferProduct(product, destination, false, CurrentOrderBox.GetProductScale(product)));
                CurrentOrderBox.RefreshContentsDisplay();
                AudioManager.Instance.Play_audioClip_Locate();
                return;
            }
        }

        public void Click_Button_Locate()
        {
            if (AdvancedGameManager.Instance.CurrentMode == Mode.InInventoryLocating)
            {
                if (currentPlacingScript != null && CreatedObject != null && CreatedObject.activeInHierarchy
                    && hasPlacementSurface && currentPlacingScript.CanConfirmPlacement())
                {
                    if (IsMovingShelf) { EndRelocation(false); return; }
                    AdvancedGameManager.Instance.CurrentMode = Mode.Free;
                    CreatedObject.transform.parent = shop.transform;
                    CreatedObject.layer = LayerMask.NameToLayer("Default");
                    Collider[] colliders = currentPlacingScript.GetComponents<Collider>();
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        colliders[i].isTrigger = false;
                    }
                    Destroy(currentPlacingScript.Rigidbody_Placing);
                    Destroy(currentPlacingScript);
                    Panel_LocatingInfo.SetActive(false);
                    FPSHandRotator.Instance.AnimateHand(InteractionType.None);
                    StartCoroutine(FPSHandRotator.Instance.Switch_Hand_InTime(Hand_Type.Free, 1.5f));
                    SavetoSystem(CreatedObject, currentEquipment);
                    currentEquipment.DecreaseItFromInventory();
                    CurrentEquipmentList.Add(CreatedObject);
                    ResetPlacementPresentation(false);
                }
            }
            else if (AdvancedGameManager.Instance.CurrentMode == Mode.HoldingForOrdering)
            {
                if (CurrentOrderBox != null && CurrentOrderBox.TryPlaceOnShelf()) return;
                StockFromCurrentBox();
            }
        }

        public void CancelLocating()
        {
            if (IsMovingShelf) { EndRelocation(true); return; }
            if (CreatedObject != null)
            {
                Destroy(CreatedObject);
            }
            ResetPlacementPresentation();
        }

        public void SavetoSystem(GameObject newOne, EquipmentScript equipment)
        {
            // Find next available index
            int objectCount = 0;
            var existingObjects = SaveLoadManager.Instance.GetAllPlacedObjects();
            var sameTypeObjects = existingObjects.FindAll(x => x.equipmentName == equipment.EquipmentDetail.Name);
            if (sameTypeObjects.Count > 0)
            {
                objectCount = sameTypeObjects.Max(x => x.objectIndex) + 1;
            }

            SaveLoadManager.Instance.AddPlacedObject(
                equipment.EquipmentDetail.Name,
                objectCount,
                newOne.transform.position,
                newOne.transform.eulerAngles,
                newOne.transform.localScale
            );
            newOne.GetComponent<PhysicalEquipmentDetails>().equipmentIndex = objectCount;
        }

        public void RemovefromSystem(int i, string name)
        {
            EquipmentScript equipment = Inventories.Where(x => x.EquipmentDetail.Name == name).FirstOrDefault();
            if (equipment != null)
            {
                SaveLoadManager.Instance.RemovePlacedObject(equipment.EquipmentDetail.Name, i);
            }
        }

        public void LoadAllObjects()
        {
            inventoryMode = InventoryMode.SellerShopIsOpen;
            Inventories.Clear();
            foreach (Transform child in Container_Decoration.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (Transform child in Container_Poster.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in Container_Sellables.transform)
            {
                Destroy(child.gameObject);
            }

            // Let's load the Objects to the Inventory UI
            for (int i = 0; i < Equipments.Count; i++)
            {
                if (Equipments[i].equipmentType == EquipmentType.Decoration)
                {
                    GameObject listItem = Instantiate(EquipmentUIPrefab, Container_Decoration.transform);
                    listItem.GetComponent<EquipmentScript>().AssignDetails(Equipments[i]);
                    Inventories.Add(listItem.GetComponent<EquipmentScript>());
                }
                else if (Equipments[i].equipmentType == EquipmentType.Poster)
                {
                    GameObject listItem = Instantiate(EquipmentUIPrefab, Container_Poster.transform);
                    listItem.GetComponent<EquipmentScript>().AssignDetails(Equipments[i]);
                    Inventories.Add(listItem.GetComponent<EquipmentScript>());
                }
                else if (Equipments[i].equipmentType == EquipmentType.Sellable)
                {
                    GameObject listItem = Instantiate(EquipmentUIPrefab, Container_Sellables.transform);
                    listItem.GetComponent<EquipmentScript>().AssignDetails(Equipments[i]);
                    Inventories.Add(listItem.GetComponent<EquipmentScript>());
                }
            }
        }

        public void LoadMyInventory()
        {
            inventoryMode = InventoryMode.PlayerInventoryIsOpen;
            foreach (var item in Inventories)
            {
                item.CurrentInStorage = SaveLoadManager.Instance.GetInventoryItemAmount(item.EquipmentDetail.Name);
                if (item.CurrentInStorage > 0)
                {
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item.gameObject.SetActive(false);
                }

                if (item.CurrentInStorage > 0)
                {
                    item.gameObject.SetActive(true);
                    item.Text_Money.text = "You have " + item.CurrentInStorage.ToString();
                    item.Text_Name.text = item.EquipmentDetail.Name.ToString();
                    item.Image_Money.sprite = item.ImageSpriteInventory;
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
                item.Image.sprite = item.EquipmentDetail.ImageSprite;
            }
        }

        public IEnumerator ArrangeEverything()
        {
            shelvesReady = false;
            yield return new WaitForSeconds(1);
            // First find all placeablePoints
            PlacablePoint[] placablePoints = GameObject.FindObjectsOfType<PlacablePoint>();
            // Restore whole boxes before assigning loose products to shelf slots.
            foreach (GameObject equipment in CurrentEquipmentList.Where(x => x != null))
            {
                OrderBox box = equipment.GetComponent<OrderBox>();
                PhysicalEquipmentDetails detail = equipment.GetComponent<PhysicalEquipmentDetails>();
                if (box == null || detail == null) continue;
                PlacedObjectData saved = SaveLoadManager.Instance.GetPlacedObject(detail.Name, detail.equipmentIndex);
                if (saved == null || (string.IsNullOrEmpty(saved.boxShelfName)
                    && string.IsNullOrEmpty(saved.boxShelfSceneId))) continue;
                ArrayableArea area = null;
                ArrayableArea[] sceneShelves = GameObject.FindObjectsOfType<ArrayableArea>()
                    .Where(x => x.GetComponent<OrderBox>() == null
                        && !CurrentEquipmentList.Contains(x.gameObject)).ToArray();
                if (!string.IsNullOrEmpty(saved.boxShelfSceneId))
                    area = sceneShelves.FirstOrDefault(x => x.SceneShelfId == saved.boxShelfSceneId);
                else
                {
                    PhysicalEquipmentDetails shelf = CurrentEquipmentList.Where(x => x != null)
                        .Select(x => x.GetComponent<PhysicalEquipmentDetails>()).FirstOrDefault(x => x != null
                            && x.Name == saved.boxShelfName && x.equipmentIndex == saved.boxShelfIndex);
                    area = shelf != null ? shelf.GetComponent<ArrayableArea>() : null;
                    // Compatibility with attachments saved before scene identities were recorded.
                    if (area == null)
                    {
                        var matches = sceneShelves.Where(x => {
                            PhysicalEquipmentDetails d = x.GetComponent<PhysicalEquipmentDetails>();
                            return d != null && d.Name == saved.boxShelfName && d.equipmentIndex == saved.boxShelfIndex;
                        }).ToArray();
                        if (matches.Length == 1) area = matches[0];
                    }
                }
                if (area == null || saved.boxShelfSlot < 0 || saved.boxShelfSlot >= area.PlacablePoints.Length
                    || !area.CanPlaceBox(area.PlacablePoints[saved.boxShelfSlot]))
                {
                    Debug.LogWarning("Cannot restore box shelf slot for " + detail.Name
                        + ". Keeping the saved box stationary; check the shelf and its placement points.", box);
                    continue;
                }
                box.AttachToShelf(area.PlacablePoints[saved.boxShelfSlot]);
            }
            SellableObject[] sellableObjects = GameObject.FindObjectsOfType<SellableObject>();
            foreach (SellableObjectData saved in SaveLoadManager.Instance.GetAllSellableObjects())
            {
                if (string.IsNullOrEmpty(saved.shelfName) || saved.slotLocalPosition == null
                    || saved.slotLocalRotation == null) continue;
                PhysicalEquipmentDetails shelf = CurrentEquipmentList.Where(x => x != null)
                    .Select(x => x.GetComponent<PhysicalEquipmentDetails>()).FirstOrDefault(x => x != null
                        && x.Name == saved.shelfName && x.equipmentIndex == saved.shelfIndex);
                ArrayableArea area = shelf != null ? shelf.GetComponent<ArrayableArea>() : null;
                if (area == null || area.boxesOnly || saved.slotIndex < 0 || saved.slotIndex >= area.PlacablePoints.Length) continue;
                PlacablePoint point = area.PlacablePoints[saved.slotIndex];
                SellableObject product = sellableObjects.FirstOrDefault(x => x.Name == saved.productName
                    && x.sellableObjectIndex == saved.objectIndex && x.isAvailable && x.putPoint == null);
                if (product == null || point.objectToGrab != null) continue;
                product.transform.SetPositionAndRotation(point.transform.TransformPoint(saved.slotLocalPosition.ToVector3()),
                    point.transform.rotation * Quaternion.Euler(saved.slotLocalRotation.ToVector3()));
                point.isAvailable = false;
                point.objectToGrab = product.gameObject;
                product.putPoint = point;
                point.Floor.SetPriceDetails(product);
            }
            for (int i = 0; i < placablePoints.Length; i++)
            {
                ArrayableArea area = placablePoints[i].GetComponentInParent<ArrayableArea>();
                if (placablePoints[i].objectToGrab != null || area == null || area.boxesOnly
                    || area.GetComponent<OrderBox>() != null) continue;
                var found = sellableObjects.Where(x => (x.transform.position - placablePoints[i].transform.position).sqrMagnitude < 0.0001f
                    && x.isAvailable && x.putPoint == null).FirstOrDefault();
                if (found != null)
                {
                    placablePoints[i].isAvailable = false;
                    placablePoints[i].objectToGrab = found.gameObject;
                    found.putPoint = placablePoints[i];
                    placablePoints[i].Floor.SetPriceDetails(found);
                }
            }
            shelvesReady = true;
        }


        public void LoadSellableObjects()
        {
            var allSellableObjects = SaveLoadManager.Instance.GetAllSellableObjects();
            foreach (var savedObj in allSellableObjects)
            {
                var item = SellableObjects.Find(x => x.Name == savedObj.productName);
                if (item != null)
                {
                    Vector3 position = savedObj.position.ToVector3();
                    Vector3 rotation = savedObj.rotation.ToVector3();

                    GameObject newEquipment = Instantiate(item.gameObject, position, Quaternion.Euler(rotation));
                    newEquipment.layer = LayerMask.NameToLayer("Default");
                    newEquipment.GetComponent<SellableObject>().sellableObjectIndex = savedObj.objectIndex;
                    CurrentEquipmentList.Add(newEquipment);
                }
            }
        }

        public void LoadBuiltObjects()
        {
            var allPlacedObjects = SaveLoadManager.Instance.GetAllPlacedObjects();
            foreach (var savedObj in allPlacedObjects)
            {
                var item = Inventories.Find(x => x.EquipmentDetail.Name == savedObj.equipmentName);
                if (item != null)
                {
                    Vector3 position = savedObj.position.ToVector3();
                    Vector3 rotation = savedObj.rotation.ToVector3();
                    Vector3 scale = savedObj.scale.ToVector3();

                    GameObject newEquipment = Instantiate(item.EquipmentDetail.gameObject, position, Quaternion.Euler(rotation), shop.transform);
                    newEquipment.layer = LayerMask.NameToLayer("Default");
                    OrderBox loadedBox = newEquipment.GetComponent<OrderBox>();
                    if (loadedBox == null)
                        Destroy(newEquipment.GetComponent<Rigidbody>());
                    else
                        loadedBox.PrepareShelfRestore(savedObj);
                    Collider[] colliders = newEquipment.GetComponents<Collider>();
                    for (int z = 0; z < colliders.Length; z++)
                    {
                        colliders[z].isTrigger = false;
                    }
                    Destroy(newEquipment.GetComponent<ObjectPlacing>());
                    newEquipment.transform.localScale = scale;
                    newEquipment.GetComponent<PhysicalEquipmentDetails>().equipmentIndex = savedObj.objectIndex;
                    CurrentEquipmentList.Add(newEquipment);
                }
            }
        }
    }

    public enum InventoryMode
    {
        SellerShopIsOpen,
        PlayerInventoryIsOpen
    }
}
