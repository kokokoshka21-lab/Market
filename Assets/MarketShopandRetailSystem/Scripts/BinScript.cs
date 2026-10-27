using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class BinScript : MonoBehaviour
    {
        public Animation animation;
        public bool isOpened = false;

        private void OnTriggerEnter(Collider other)
        {
            if(!isOpened && other.CompareTag("Player"))
            {
                isOpened = true;
                animation.Play("BinOpeningAnim");
                AudioManager.Instance.Play_Audio_BinOpen();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (isOpened && other.CompareTag("Player"))
            {
                isOpened = false;
                animation.Play("BinClosingAnim");
                AudioManager.Instance.Play_Audio_BinClose();
            }
        }
    }
}
