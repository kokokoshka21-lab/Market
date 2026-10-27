using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class BoxScript : MonoBehaviour
    {
        public bool isHolding = false;
        public Rigidbody rigidbody;
        private Vector3 initialSize;

        void Start()
        {
            rigidbody = GetComponent<Rigidbody>();
            initialSize = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }

        public void Interact()
        {
            if (!isHolding && !HeroPlayerScript.Instance.isHoldingBox)
            {
                isHolding = true;
                if(GetComponent<ItemScript>().Name == "Trash Bag")
                {
                    transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                HeroPlayerScript.Instance.isHoldingBox = true;
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                transform.parent = CameraScript.Instance.transform;
                transform.GetComponent<BoxCollider>().enabled = false;
                transform.localRotation = new Quaternion(0,0,0,0);
                transform.localPosition = new Vector3(0,-0.5f,0.4f);
                FPSHandRotator.Instance.Switch_Hand(Hand_Type.Free);
                AudioManager.Instance.Play_Item_Grab();
            }
            else if(isHolding && HeroPlayerScript.Instance.isHoldingBox)
            {
                isHolding = false;
                transform.localScale = new Vector3(initialSize.x, initialSize.y, initialSize.z);
                HeroPlayerScript.Instance.isHoldingBox = false;
                rigidbody.isKinematic = false;
                transform.GetComponent<BoxCollider>().enabled = true;
                rigidbody.useGravity = true;
                HeroPlayerScript.Instance.RemovingBox();
                rigidbody.AddForce((transform.forward + transform.up) * 3, ForceMode.Impulse);
                transform.parent = null;
                AudioManager.Instance.Play_Item_Grab();
            }
        }
    }
}
