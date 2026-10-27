using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MarketShopandRetailSystem
{
    public class DoorScript : MonoBehaviour
    {
        public bool isOpened = false;
        private Animation animation;
        public NavMeshObstacle navmeshObstacle;
        public Collider collider;


        private void Start()
        {
            animation = GetComponent<Animation>();
        }

        IEnumerator OpenTheDoor()
        {
            LastTimeTry = LastTimeTry - 1;
            yield return new WaitForSeconds(0.5f);
            TryToOpen();
        }

        private float LastTimeTry = 0;
        public void TryToOpen()
        {
            if (Time.time > LastTimeTry + 1)
            {
                LastTimeTry = Time.time;
                if (isOpened == false)
                {
                    animation["DoorOpen"].time = 0;
                    animation["DoorOpen"].speed = 1;
                    animation.Play("DoorOpen");
                    isOpened = true;
                    navmeshObstacle.enabled = false;
                    collider.isTrigger = true;
                    AudioManager.Instance.Play_Door_Wooden_Open();
                }
                else
                {
                    isOpened = false;
                    AudioManager.Instance.Play_Door_Close();
                    animation["DoorOpen"].time = animation["DoorOpen"].length;
                    animation["DoorOpen"].speed = -1;
                    animation.Play("DoorOpen");
                    navmeshObstacle.enabled = true;
                    collider.isTrigger = false;
                }
            }
        }
    }
}