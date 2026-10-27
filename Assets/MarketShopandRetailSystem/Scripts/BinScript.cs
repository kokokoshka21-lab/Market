using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class BinScript : MonoBehaviour
    {
        public Animation animation;
        public bool isOpened = false;
        [Min(0f)]
        public float closeDelay = 0.75f;
        private readonly HashSet<Collider> nearbyObjects = new HashSet<Collider>();
        private float closeAt;

        private void OnTriggerEnter(Collider other)
        {
            TrackNearbyObject(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // Also detect a box released while it is already inside the outer trigger.
            TrackNearbyObject(other);
        }

        private void OnTriggerExit(Collider other)
        {
            nearbyObjects.Remove(other);
        }

        private void Update()
        {
            nearbyObjects.RemoveWhere(other => other == null || !other.enabled
                || !other.gameObject.activeInHierarchy || !IsNearbyObject(other));
            if (nearbyObjects.Count > 0)
            {
                closeAt = Time.time + Mathf.Max(0f, closeDelay);
            }
            else if (isOpened && Time.time >= closeAt)
            {
                isOpened = false;
                animation.Play("BinClosingAnim");
                AudioManager.Instance.Play_Audio_BinClose();
            }
        }

        private void OnDisable()
        {
            nearbyObjects.Clear();
        }

        private void TrackNearbyObject(Collider other)
        {
            if (!IsNearbyObject(other)) return;

            nearbyObjects.Add(other);
            OpenLid();
        }

        private bool IsNearbyObject(Collider other)
        {
            return other.CompareTag("Player") || GetReleasedBoxBody(other) != null;
        }

        private static Rigidbody GetReleasedBoxBody(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null || body.isKinematic || !body.CompareTag("Item")) return null;

            if (body.TryGetComponent<OrderBox>(out var orderBox))
            {
                return orderBox.isHolding ? null : body;
            }
            if (body.TryGetComponent<BoxScript>(out var box))
            {
                return box.isHolding ? null : body;
            }
            return null;
        }

        private void OpenLid()
        {
            closeAt = Time.time + Mathf.Max(0f, closeDelay);
            if (isOpened) return;

            isOpened = true;
            animation.Play("BinOpeningAnim");
            AudioManager.Instance.Play_Audio_BinOpen();
        }

        public void TryDisposeBox(Collider other)
        {
            if (!isActiveAndEnabled) return;

            Rigidbody body = GetReleasedBoxBody(other);
            if (body == null) return;

            // Open here too: physics callbacks from the two zones may arrive in either order.
            OpenLid();
            // Prevent repeated disposal during the existing delayed destruction.
            body.isKinematic = true;
            body.useGravity = false;
            if (body.TryGetComponent<ItemScript>(out var item)) item.enabled = false;
            foreach (Collider boxCollider in body.GetComponentsInChildren<Collider>())
            {
                boxCollider.enabled = false;
            }

            if (body.TryGetComponent<OrderBox>(out var orderBox))
            {
                orderBox.DestroyTheOrderBox();
            }
            else if (body.TryGetComponent<ObjectStatusTracker>(out var tracker))
            {
                tracker.DestroyObjectForever();
            }
            else
            {
                Destroy(body.gameObject);
            }
        }
    }
}
