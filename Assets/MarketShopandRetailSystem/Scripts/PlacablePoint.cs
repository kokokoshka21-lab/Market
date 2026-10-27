using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class PlacablePoint : MonoBehaviour
    {
        [HideInInspector]
        public bool isAvailable = true;
        public GameObject objectToGrab;
        [HideInInspector]
        public int ID;
        public GameObject MainContainer;
        public FloorScript Floor;

        private void Start()
        {
            if(Floor == null)
            {
                Floor = transform.parent.GetComponent<FloorScript>();
            }
            if(MainContainer == null)
            {
                if(transform.GetComponentInParent<PhysicalEquipmentDetails>() != null)
                {
                    MainContainer = transform.GetComponentInParent<PhysicalEquipmentDetails>().gameObject;
                }
            }
        }
    }
}
