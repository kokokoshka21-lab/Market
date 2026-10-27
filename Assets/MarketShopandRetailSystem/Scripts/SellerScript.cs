using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class SellerScript : MonoBehaviour
    {
        public string Name;
        public string[] MeetingConversations;
        private int MeetingConversationIndex = 0;


        private bool isRotating = false;
        private float rotationTime = 0.0f;
        private Quaternion startRotation;
        private Quaternion targetRotation;

        public void Talk()
        {
            StartRotation();
            if (!SaveLoadManager.Instance.GetHasMetSellerBefore())
            {
                string conversation = MeetingConversations[MeetingConversationIndex];
                SpeechManager.instance.Show_Speach(conversation, Name, gameObject);
                if (MeetingConversationIndex == MeetingConversations.Length - 1)
                {
                    SaveLoadManager.Instance.SetHasMetSellerBefore(true);
                }
                else
                {
                    MeetingConversationIndex++;
                }
            }
            else
            {
                GameCanvas.Instance.Show_Panel_SellerShop();
            }
        }

        private void Update()
        {
            if (isRotating)
            {
                RotateTowardsTarget();
            }
        }

        public void Leave()
        {
            GameCanvas.Instance.Hide_Panel_SellerShop(false);
        }


        void StartRotation()
        {
            startRotation = transform.rotation;
            Vector3 direction = HeroPlayerScript.Instance.transform.position - transform.position;
            targetRotation = Quaternion.LookRotation(direction);
            isRotating = true;
            rotationTime = 0.0f;
        }

        void RotateTowardsTarget()
        {
            rotationTime += Time.deltaTime;
            float fraction = rotationTime / 1;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, fraction);

            if (fraction >= 1.0f)
            {
                isRotating = false;
            }
        }
    }
}