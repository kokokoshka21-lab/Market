using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace MarketShopandRetailSystem
{
    // The real shelf stays at its saved location until a disposable construction preview is confirmed.
    public sealed class ShelfRelocation
    {
        public PhysicalEquipmentDetails Shelf { get; }
        public GameObject Preview { get; private set; }
        private readonly Vector3 originalPosition;
        private readonly Quaternion originalRotation;
        private readonly bool originalActive;
        private readonly List<ProductState> products = new List<ProductState>();
        private bool finished;

        private sealed class ProductState
        {
            public SellableObject product;
            public Transform parent;
            public bool available;
            public PlacablePoint point;
        }

        public static bool Supports(PhysicalEquipmentDetails shelf)
        {
            return shelf != null && shelf.equipmentType == EquipmentType.Decoration
                && shelf.GetComponent<ArrayableArea>() != null && shelf.GetComponent<OrderBox>() == null
                && shelf.GetComponent<ObjectPlacing>() == null
                && shelf.GetComponents<BoxCollider>().Any(box => box.enabled && !box.isTrigger);
        }

        public ShelfRelocation(PhysicalEquipmentDetails shelf)
        {
            Shelf = shelf;
            originalPosition = shelf.transform.position;
            originalRotation = shelf.transform.rotation;
            originalActive = shelf.gameObject.activeSelf;
            foreach (PlacablePoint point in shelf.GetComponent<ArrayableArea>().PlacablePoints)
            {
                if (TriggerScript.Instance != null) TriggerScript.Instance.placablePoints.Remove(point);
                if (point.objectToGrab == null) continue;
                SellableObject product = point.objectToGrab.GetComponent<SellableObject>();
                if (product == null) continue;
                products.Add(new ProductState { product = product, parent = product.transform.parent,
                    available = product.isAvailable, point = product.putPoint });
                product.transform.SetParent(shelf.transform, true);
                product.isAvailable = false;
                product.putPoint = null;
            }
            shelf.gameObject.SetActive(false);
        }

        public ObjectPlacing CreatePreview()
        {
            // Clone while inactive so gameplay Start/OnEnable methods cannot run on the ghost.
            Preview = Object.Instantiate(Shelf.gameObject, Shelf.transform.position,
                Shelf.transform.rotation, Shelf.transform.parent);
            Preview.name = Shelf.name + " (Move Preview)";
            Preview.transform.localScale = Shelf.transform.localScale;
            foreach (MonoBehaviour script in Preview.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(script is UnityEngine.UI.Graphic) && !(script is TMPro.TMP_Text)) script.enabled = false;
            foreach (NavMeshObstacle obstacle in Preview.GetComponentsInChildren<NavMeshObstacle>(true)) obstacle.enabled = false;
            foreach (Animation clip in Preview.GetComponentsInChildren<Animation>(true)) clip.enabled = false;
            foreach (Animator animator in Preview.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (Collider collider in Preview.GetComponentsInChildren<Collider>(true))
            {
                if (collider.transform == Preview.transform && collider is BoxCollider && collider.enabled)
                    collider.isTrigger = true;
                else collider.enabled = false;
            }
            foreach (Rigidbody body in Preview.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
            Rigidbody previewBody = Preview.GetComponent<Rigidbody>();
            if (previewBody == null) previewBody = Preview.AddComponent<Rigidbody>();
            previewBody.isKinematic = true;
            previewBody.useGravity = false;
            ObjectPlacing placing = Preview.AddComponent<ObjectPlacing>();
            placing.ConfigureRelocationPreview();
            return placing;
        }

        public bool TryCommit(SaveLoadManager save)
        {
            if (finished || Shelf == null || Preview == null || !Preview.activeInHierarchy) return false;
            Shelf.transform.SetPositionAndRotation(Preview.transform.position, Preview.transform.rotation);
            if (!save.UpdateShelfPlacement(Shelf))
            {
                Shelf.transform.SetPositionAndRotation(originalPosition, originalRotation);
                return false;
            }
            Finish(false);
            return true;
        }

        public void Cancel()
        {
            Finish(true);
        }

        private void Finish(bool cancel)
        {
            if (finished) return;
            finished = true;
            if (Shelf != null && cancel) Shelf.transform.SetPositionAndRotation(originalPosition, originalRotation);
            foreach (ProductState state in products)
            {
                if (state.product == null) continue;
                state.product.transform.SetParent(state.parent, true);
                state.product.isAvailable = state.available;
                state.product.putPoint = state.point;
            }
            if (Shelf != null) Shelf.gameObject.SetActive(originalActive);
            if (Preview != null)
            {
                Preview.SetActive(false);
                Object.Destroy(Preview);
                Preview = null;
            }
        }
    }
}
