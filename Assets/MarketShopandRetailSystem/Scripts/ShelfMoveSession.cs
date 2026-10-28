using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace MarketShopandRetailSystem
{
    // Moves the existing objects; inventory counts, slot assignments and product IDs stay intact.
    public sealed class ShelfMoveSession
    {
        public PhysicalEquipmentDetails Shelf { get; }
        public bool CanPlace { get; private set; }
        private readonly Vector3 originalPosition;
        private readonly Quaternion originalRotation;
        private readonly Collider[] colliders;
        private readonly bool[] colliderStates;
        private readonly BoxCollider[] bounds;
        private readonly NavMeshObstacle[] obstacles;
        private readonly bool[] obstacleStates;
        private readonly List<ProductState> products = new List<ProductState>();
        private readonly float bottomOffset;
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

        public ShelfMoveSession(PhysicalEquipmentDetails shelf)
        {
            Shelf = shelf;
            originalPosition = shelf.transform.position;
            originalRotation = shelf.transform.rotation;
            bounds = shelf.GetComponents<BoxCollider>().Where(box => box.enabled && !box.isTrigger).ToArray();
            float bottom = float.PositiveInfinity;
            foreach (BoxCollider box in bounds)
                bottom = Mathf.Min(bottom, box.bounds.min.y);
            bottomOffset = bottom - shelf.transform.position.y;

            foreach (PlacablePoint point in shelf.GetComponent<ArrayableArea>().PlacablePoints)
            {
                if (point.objectToGrab == null) continue;
                SellableObject product = point.objectToGrab.GetComponent<SellableObject>();
                if (product == null) continue;
                products.Add(new ProductState { product = product, parent = product.transform.parent,
                    available = product.isAvailable, point = product.putPoint });
                product.transform.SetParent(shelf.transform, true);
                product.isAvailable = false;
                product.putPoint = null;
            }

            colliders = shelf.GetComponentsInChildren<Collider>(true);
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                colliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }
            obstacles = shelf.GetComponentsInChildren<NavMeshObstacle>(true);
            obstacleStates = new bool[obstacles.Length];
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacleStates[i] = obstacles[i].enabled;
                obstacles[i].enabled = false;
            }
        }

        public void Aim(Ray ray, LayerMask mask)
        {
            CanPlace = false;
            RaycastHit[] hits = Physics.RaycastAll(ray, 6f, mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(Shelf.transform)) continue;
                // The nearest solid surface must be a floor, not a wall or the back of an obstacle.
                if (hit.normal.y < 0.7f) return;
                Shelf.transform.position = hit.point - Vector3.up * bottomOffset;
                CanPlace = Validate(mask);
                return;
            }
        }

        public void Rotate(float degrees)
        {
            Shelf.transform.Rotate(Vector3.up, degrees, Space.World);
            CanPlace = false;
        }

        public bool Validate(LayerMask mask)
        {
            for (int i = 0; i < bounds.Length; i++)
            {
                bool insideShop = false;
                BoxCollider box = bounds[i];
                if (box.isTrigger) continue;
                Vector3 scale = box.transform.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Vector3 halfSize = Vector3.Scale(box.size, scale) * 0.5f;
                halfSize = new Vector3(Mathf.Max(0.001f, halfSize.x - 0.015f),
                    Mathf.Max(0.001f, halfSize.y - 0.015f), Mathf.Max(0.001f, halfSize.z - 0.015f));
                Vector3 center = box.transform.TransformPoint(box.center);
                foreach (Collider other in Physics.OverlapBox(center, halfSize, box.transform.rotation,
                    mask, QueryTriggerInteraction.Collide))
                {
                    if (other.transform.IsChildOf(Shelf.transform)) continue;
                    if (other.CompareTag("Shop"))
                    {
                        if (other.bounds.Contains(center) && (!(other is BoxCollider shopArea)
                            || ContainsFootprint(shopArea, center, halfSize, box.transform.rotation))) insideShop = true;
                    }
                    else if (!other.isTrigger) return false;
                }
                if (!insideShop) return false;
            }
            return bounds.Length > 0;
        }

        private static bool ContainsFootprint(BoxCollider area, Vector3 center, Vector3 halfSize, Quaternion rotation)
        {
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + rotation * new Vector3(x * halfSize.x, 0f, z * halfSize.z);
                Vector3 local = area.transform.InverseTransformPoint(corner) - area.center;
                if (Mathf.Abs(local.x) > area.size.x * 0.5f || Mathf.Abs(local.z) > area.size.z * 0.5f) return false;
            }
            return true;
        }

        public void Finish(bool cancel)
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
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            for (int i = 0; i < obstacles.Length; i++)
                if (obstacles[i] != null) obstacles[i].enabled = obstacleStates[i];
        }
    }
}
