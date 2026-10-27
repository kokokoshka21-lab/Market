using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class HangingSignScript : MonoBehaviour
    {
        public Animation animation;
        public AudioSource audioSource;

        private void Start()
        {
            gameObject.SetActive(AdvancedGameManager.Instance.isHangingSignActive);
        }

        public void Flip()
        {
            if (AdvancedGameManager.Instance.isShopOpen)
            {
                GameCanvas.Instance.Show_Warning_Not("Store is Closed!", false);
                animation["HangingSign_Flip"].time = animation["HangingSign_Flip"].length;
                animation["HangingSign_Flip"].speed = -1;
                animation.Play("HangingSign_Flip");
                AdvancedGameManager.Instance.isShopOpen = false;
            }
            else
            {
                GameCanvas.Instance.Show_Warning_Not("Store is Open!", true);
                animation["HangingSign_Flip"].time = 0;
                animation["HangingSign_Flip"].speed = 1;
                animation.Play("HangingSign_Flip");
                AdvancedGameManager.Instance.isShopOpen = true;
            }
            audioSource.Play();
        }
    }
}
