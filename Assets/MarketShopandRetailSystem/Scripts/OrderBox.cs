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
        public PlacablePoint ShelfPoint { get; private set; }
        private Coroutine locationSave;
        public bool IsMovingToShelf { get; private set; }
        private GameObject placementPreview;
        private readonly List<Material> placementPreviewMaterials = new List<Material>();

        private sealed class HighlightState
        {
            public Renderer renderer;
            public int materialIndex;
            public MaterialPropertyBlock original;
        }

        private readonly List<HighlightState> highlightStates = new List<HighlightState>();
        private bool aimHighlighted;

        public void SetAimHighlight(bool highlighted)
        {
            if (aimHighlighted == highlighted) return;
            aimHighlighted = highlighted;
            if (!highlighted)
            {
                foreach (HighlightState state in highlightStates)
                    if (state.renderer != null)
                        state.renderer.SetPropertyBlock(state.original.isEmpty ? null : state.original, state.materialIndex);
                highlightStates.Clear();
                return;
            }
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                // Tint the box itself, keeping the products and their labels unchanged.
                if (renderer.GetComponentInParent<SellableObject>() != null) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (material == null) continue;
                    string property = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                    if (!material.HasProperty(property)) continue;
                    int colorId = Shader.PropertyToID(property);
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, index);
                    var tint = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(tint, index);
                    // Preserve renderer-wide overrides when there is no per-material block.
                    if (tint.isEmpty) renderer.GetPropertyBlock(tint);
                    Color color = tint.HasColor(colorId) ? tint.GetColor(colorId) : material.GetColor(colorId);
                    Color yellow = Color.Lerp(color, Color.yellow, 0.25f);
                    yellow.a = color.a;
                    tint.SetColor(colorId, yellow);
                    highlightStates.Add(new HighlightState { renderer = renderer, materialIndex = index, original = original });
                    renderer.SetPropertyBlock(tint, index);
                }
            }
        }

        private void OnDisable()
        {
            SetAimHighlight(false);
            DestroyPlacementPreview();
        }

        private void OnDestroy()
        {
            DestroyPlacementPreview();
        }

        private PlacablePoint FindShelfPlacementPoint()
        {
            if (!isHolding || IsMovingToShelf || TriggerScript.Instance == null) return null;
            return TriggerScript.Instance.placablePoints
                .Where(x => x != null && !x.transform.IsChildOf(transform))
                .OrderBy(x => (x.transform.position - CameraScript.Instance.transform.position).sqrMagnitude)
                .FirstOrDefault(x => {
                    ArrayableArea area = x.GetComponentInParent<ArrayableArea>();
                    return area != null && area.CanPlaceBox(x)
                        && area.GetComponent<PhysicalEquipmentDetails>() != null;
                });
        }

        private void LateUpdate()
        {
            if (!isHolding)
            {
                DestroyPlacementPreview();
                return;
            }
            PlacablePoint point = Time.timeScale > 0f
                && AdvancedGameManager.Instance.CurrentMode == Mode.HoldingForOrdering
                ? FindShelfPlacementPoint() : null;
            PhysicalEquipmentDetails self = GetComponent<PhysicalEquipmentDetails>();
            if (point == null || self == null
                || SaveLoadManager.Instance.GetPlacedObject(self.Name, self.equipmentIndex) == null)
            {
                if (placementPreview != null) placementPreview.SetActive(false);
                return;
            }
            if (placementPreview == null) CreatePlacementPreview();
            // Match AttachToShelf, including shelf scaling and the configured box size.
            placementPreview.transform.SetParent(point.GetComponentInParent<ArrayableArea>().transform, false);
            placementPreview.transform.localScale = initialSize;
            placementPreview.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
            placementPreview.SetActive(true);
        }

        private void CreatePlacementPreview()
        {
            placementPreview = new GameObject(name + " (Box Placement Preview)");
            placementPreview.SetActive(false);
            CopyPreviewVisuals(transform, placementPreview.transform);
            // The held box is open. Show the closed pose that it will have on the shelf.
            AnimationClip closedPose = animation != null ? animation.GetClip("Open") : null;
            if (closedPose != null) closedPose.SampleAnimation(placementPreview, 0f);
        }

        private void CopyPreviewVisuals(Transform source, Transform destination)
        {
            // Copy only visual components; never clone gameplay scripts, colliders or box contents.
            MeshFilter sourceMesh = source.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
            if (sourceMesh != null && sourceRenderer != null)
            {
                destination.gameObject.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
                MeshRenderer renderer = destination.gameObject.AddComponent<MeshRenderer>();
                Material[] materials = sourceRenderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    Material white = new Material(materials[i]);
                    if (white.HasProperty("_BaseColor")) white.SetColor("_BaseColor", Color.white);
                    if (white.HasProperty("_Color")) white.SetColor("_Color", Color.white);
                    if (white.HasProperty("_EmissionColor"))
                    {
                        white.EnableKeyword("_EMISSION");
                        white.SetColor("_EmissionColor", Color.white * 0.5f);
                    }
                    materials[i] = white;
                    placementPreviewMaterials.Add(white);
                }
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = sourceRenderer.enabled;
            }
            foreach (Transform child in source)
            {
                if (child.GetComponent<SellableObject>() != null) continue;
                var visual = new GameObject(child.name);
                visual.transform.SetParent(destination, false);
                visual.transform.localPosition = child.localPosition;
                visual.transform.localRotation = child.localRotation;
                visual.transform.localScale = child.localScale;
                visual.SetActive(child.gameObject.activeSelf);
                CopyPreviewVisuals(child, visual.transform);
            }
        }

        private static void DestroyPreviewObject(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void DestroyPlacementPreview()
        {
            if (placementPreview != null)
            {
                placementPreview.SetActive(false);
                DestroyPreviewObject(placementPreview);
                placementPreview = null;
            }
            foreach (Material material in placementPreviewMaterials)
                if (material != null) DestroyPreviewObject(material);
            placementPreviewMaterials.Clear();
        }

        public bool TryPlaceOnShelf()
        {
            PlacablePoint point = FindShelfPlacementPoint();
            if (point == null) return false;
            PhysicalEquipmentDetails owner = point.GetComponentInParent<PhysicalEquipmentDetails>();
            PhysicalEquipmentDetails self = GetComponent<PhysicalEquipmentDetails>();
            PlacedObjectData saved = SaveLoadManager.Instance.GetPlacedObject(self.Name, self.equipmentIndex);
            if (owner == null || saved == null) return false;
            DestroyPlacementPreview();
            isHolding = false;
            HeroPlayerScript.Instance.isHoldingBox = false;
            InventoryManager.Instance.CurrentOrderBox = null;
            if (AdvancedGameManager.Instance.controllerType == ControllerType.Mobile)
                GameCanvas.Instance.Button_GetProductBackToOrderBox.SetActive(false);
            Vector3 startPosition = transform.position;
            AttachToShelf(point);
            saved.boxShelfSceneId = InventoryManager.Instance.CurrentEquipmentList.Contains(owner.gameObject)
                ? null : owner.GetComponent<ArrayableArea>().SceneShelfId;
            saved.boxShelfName = owner.Name;
            saved.boxShelfIndex = owner.equipmentIndex;
            saved.boxShelfSlot = point.ID;
            saved.position = new Vector3Data(transform.position);
            saved.rotation = new Vector3Data(transform.eulerAngles);
            saved.scale = new Vector3Data(transform.localScale);
            SaveLoadManager.Instance.SaveGame();
            StartCoroutine(FlyToShelf(point, startPosition));
            AdvancedGameManager.Instance.CurrentMode = Mode.Free;
            AudioManager.Instance.Play_audioClip_Locate();
            return true;
        }

        private IEnumerator FlyToShelf(PlacablePoint point, Vector3 startPosition)
        {
            IsMovingToShelf = true;
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            boxCollider.enabled = false;
            transform.position = startPosition;
            try
            {
                float elapsed = 0f;
                while (point != null && elapsed < 0.25f)
                {
                    transform.position = Vector3.Lerp(startPosition, point.transform.position, elapsed / 0.25f);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (point != null) transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
            }
            finally
            {
                boxCollider.enabled = true;
                IsMovingToShelf = false;
            }
        }

        // Called while loading, before the first physics step or delayed slot restoration.
        public void PrepareShelfRestore(PlacedObjectData saved)
        {
            if (saved == null || (string.IsNullOrEmpty(saved.boxShelfName)
                && string.IsNullOrEmpty(saved.boxShelfSceneId))) return;
            rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        public void AttachToShelf(PlacablePoint point)
        {
            ShelfPoint = point;
            point.objectToGrab = gameObject;
            point.isAvailable = false;
            // Parent to the shelf so relocation also carries the box and its contents.
            transform.SetParent(point.GetComponentInParent<ArrayableArea>().transform, true);
            transform.localScale = initialSize;
            transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            transform.GetComponent<BoxCollider>().enabled = true;
            animation.Stop();
            animation["Open"].time = 0f;
            animation["Open"].speed = 0f;
            animation.Play();
        }

        private void DetachFromShelf()
        {
            if (ShelfPoint == null) return;
            ShelfPoint.objectToGrab = null;
            ShelfPoint.isAvailable = true;
            ShelfPoint = null;
            PhysicalEquipmentDetails self = GetComponent<PhysicalEquipmentDetails>();
            PlacedObjectData saved = SaveLoadManager.Instance.GetPlacedObject(self.Name, self.equipmentIndex);
            if (saved != null)
            {
                saved.boxShelfName = null;
                saved.boxShelfSceneId = null;
                SaveLoadManager.Instance.SaveGame();
            }
        }

        public SpriteRenderer spriteProduct;
        public TMPro.TMP_Text textCount;

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            initialSize = transform.localScale;
            if (arrayableArea == null) arrayableArea = GetComponent<ArrayableArea>();
            PhysicalEquipmentDetails detail = GetComponent<PhysicalEquipmentDetails>();
            PlacedObjectData saved = SaveLoadManager.Instance.GetPlacedObject(detail.Name, detail.equipmentIndex);
            PrepareShelfRestore(saved);
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
            DetachFromShelf();
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
            if (!ContentsReady || IsMovingToShelf) return;
            if (!isHolding && !HeroPlayerScript.Instance.isHoldingBox)
            {
                if (locationSave != null) StopCoroutine(locationSave);
                SetAimHighlight(false);
                DetachFromShelf();
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
                if (TryPlaceOnShelf()) return;
                // Box-only points must not prevent throwing when every box slot is full.
                if (!TriggerScript.Instance.placablePoints.Any(point => point != null
                    && !point.transform.IsChildOf(transform)
                    && point.GetComponentInParent<OrderBox>() == null
                    && point.GetComponentInParent<ArrayableArea>() != null
                    && !point.GetComponentInParent<ArrayableArea>().boxesOnly))
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
                    locationSave = StartCoroutine(SavetoSystemNewLocation());
                }
            }
        }

        public IEnumerator SavetoSystemNewLocation()
        {
            yield return new WaitForSeconds(2);
            if (isHolding || ShelfPoint != null) yield break;
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
