using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class TriggerScript : MonoBehaviour
    {
        public List<PlacablePoint> placablePoints;
        public static TriggerScript Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Point"))
            {
                for (int i = 0; i < placablePoints.Count; i++)
                {
                    if(placablePoints[i] == null)
                    {
                        placablePoints.RemoveAt(i);
                        i--;
                    }
                }
                PlacablePoint triggeredPoint = other.GetComponent<PlacablePoint>();
                if (triggeredPoint != null && !placablePoints.Contains(triggeredPoint))
                {
                    placablePoints.Add(triggeredPoint);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Point"))
            {
                for (int i = 0; i < placablePoints.Count; i++)
                {
                    if (placablePoints[i] == null)
                    {
                        placablePoints.RemoveAt(i);
                        i--;
                    }
                }
                PlacablePoint triggeredPoint = other.GetComponent<PlacablePoint>();
                if (triggeredPoint != null && placablePoints.Contains(triggeredPoint))
                {
                    placablePoints.Remove(triggeredPoint);
                }
            }
        }
    }
}