using System.Collections;
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
        public Transform suctionTarget;
        [Min(0f)]
        public float suctionDuration = 0.5f;
        [Min(0f)]
        [Tooltip("Minimum suction rotation speed in degrees per second.")]
        public float minSuctionRotationSpeed = 180f;
        [Min(0f)]
        [Tooltip("Maximum suction rotation speed in degrees per second.")]
        public float maxSuctionRotationSpeed = 540f;
        private readonly HashSet<Collider> nearbyObjects = new HashSet<Collider>();
        private readonly HashSet<Transform> suckedBoxes = new HashSet<Transform>();
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
            suckedBoxes.RemoveWhere(box => box == null);
            if (nearbyObjects.Count > 0 || suckedBoxes.Count > 0)
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
            StopAllCoroutines();
            suckedBoxes.Clear();
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

            float duration = Mathf.Max(0f, suctionDuration);
            // Persist removal immediately; destruction remains scheduled even if the bin is disabled.
            if (body.TryGetComponent<OrderBox>(out var orderBox))
            {
                orderBox.DestroyTheOrderBox(duration);
            }
            else if (body.TryGetComponent<ObjectStatusTracker>(out var tracker))
            {
                tracker.DestroyObjectForever(duration);
            }
            else
            {
                Destroy(body.gameObject, duration);
            }

            if (duration > 0f)
            {
                suckedBoxes.Add(body.transform);
                StartCoroutine(SuckBox(body.transform, duration));
            }
        }

        private IEnumerator SuckBox(Transform box, float duration)
        {
            Vector3 startPosition = box.position;
            Vector3 startScale = box.localScale;
            Quaternion startRotation = box.rotation;
            Vector3 rotationAxis = Random.onUnitSphere;
            float minSpeed = Mathf.Max(0f, Mathf.Min(minSuctionRotationSpeed, maxSuctionRotationSpeed));
            float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(minSuctionRotationSpeed, maxSuctionRotationSpeed));
            float rotationSpeed = Random.Range(minSpeed, maxSpeed);
            Vector3 targetPosition = suctionTarget != null ? suctionTarget.position : transform.position;
            float elapsed = 0f;

            while (box != null && elapsed < duration)
            {
                elapsed = Mathf.Min(elapsed + Time.deltaTime, duration);
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (suctionTarget != null) targetPosition = suctionTarget.position;
                box.position = Vector3.Lerp(startPosition, targetPosition, progress);
                box.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                box.rotation = Quaternion.AngleAxis(rotationSpeed * elapsed, rotationAxis) * startRotation;
                yield return null;
            }

            if (box != null)
            {
                box.position = targetPosition;
                box.localScale = Vector3.zero;
            }
            suckedBoxes.Remove(box);
            closeAt = Time.time + Mathf.Max(0f, closeDelay);
        }
    }
}
