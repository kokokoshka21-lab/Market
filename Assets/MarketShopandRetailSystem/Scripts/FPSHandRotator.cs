using System.Collections;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class FPSHandRotator : MonoBehaviour
    {
        public float speed = 2.5f;
        public static FPSHandRotator Instance;

        public GameObject Hands_Parent;
        public GameObject Hammer;
        public GameObject Broom;

        public Hand_Type Current_HandType = Hand_Type.Free;

        private void Awake()
        {
            Instance = this;
        }

        public void Hide_HandParent()
        {
            Switch_Hand(Hand_Type.Free);
            Hammer.SetActive(false);
            Broom.SetActive(false);
            Hands_Parent.SetActive(false);
        }

        public void Show_HandParent()
        {
            Hands_Parent.SetActive(true);
        }

        public IEnumerator Switch_Hand_InTime(Hand_Type hand_Type, float second)
        {
            yield return new WaitForSeconds(second);
            Switch_Hand(hand_Type);
        }

        public void Switch_Hand(Hand_Type hand_Type)
        {
            if (Current_HandType != hand_Type)
            {
                Hammer.SetActive(false);
                Broom.SetActive(false);
                Current_HandType = hand_Type;
                switch (hand_Type)
                {
                    case Hand_Type.Build:
                        Hammer.SetActive(true);
                        break;
                    case Hand_Type.Clean:
                        Broom.SetActive(true);
                        break;
                    case Hand_Type.Free:
                        break;
                }
            }
        }

        public void AnimateHand(InteractionType type)
        {
            switch (Current_HandType)
            {
                case Hand_Type.Build:
                    if (type == InteractionType.None || type == InteractionType.Build)
                    {
                        if (!Hammer.GetComponent<Animation>().isPlaying)
                        {
                            Hammer.GetComponent<Animation>().Play();
                        }
                        AudioManager.Instance.Play_Audio_Building();
                    }
                    break;
                case Hand_Type.Clean:
                    if (type == InteractionType.Clean)
                    {
                        if (!Broom.GetComponent<Animation>().isPlaying)
                        {
                            Broom.GetComponent<Animation>().Play();
                        }
                        AudioManager.Instance.Play_Audio_Cleaning();
                    }
                    break;
            }
        }

    }

    public enum Hand_Type
    {
        Free,
        Build,
        Clean,
        Any
    }

}
