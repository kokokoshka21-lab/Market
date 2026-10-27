using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MarketShopandRetailSystem
{
    public class ItemScript : MonoBehaviour
    {
        public ItemType itemType;
        public string Name = "";
        public InteractionType interactionType;
        public UnityEvent eventToInvokeWhenInteract;
        public Hand_Type NeededHandType = Hand_Type.Any;
        [HideInInspector]
        public bool isInteracted = false;
        public SpriteRenderer sprite;

        public void Interact()
        {
            if (Time.time < HeroPlayerScript.Instance.LastInteractedTime + 0.5f) return;

            HeroPlayerScript.Instance.LastInteractedTime = Time.time;

            if (itemType == ItemType.Door)
            {
                GetComponent<DoorScript>().TryToOpen();
            }
            else if (itemType == ItemType.Seller)
            {
                GetComponent<SellerScript>().Talk();
            }
            else if (itemType == ItemType.Shop)
            {
                GetComponent<ShopScript>().TryToRentTheShop();
            }
            else if (itemType == ItemType.PriceTag)
            {
                GetComponent<Button>().onClick.Invoke();
            }
            else if (itemType == ItemType.Banknote)
            {
                GetComponent<BanknoteScript>().Select_Banknote();
            }
            else if (itemType == ItemType.HangingSign)
            {
                GetComponent<HangingSignScript>().Flip();
            }

            else if (itemType == ItemType.MoneyBag)
            {
                AdvancedGameManager.Instance.Get(CollactableType.Money, transform.GetComponent<GainScript>().amount);
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
                if (GetComponent<ObjectStatusTracker>() != null)
                {
                    GetComponent<ObjectStatusTracker>().DestroyObjectForever();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else if (itemType == ItemType.NPC)
            {
                GetComponent<CivilianController>().SpeakwithHero();
            }
            else if (itemType == ItemType.ItemToMaintain)
            {
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
                AudioManager.Instance.Play_Audio_PressAndHoldMaintainDone();
                if (GetComponent<ObjectStatusTracker>() != null)
                {
                    GetComponent<ObjectStatusTracker>().DestroyObjectForever();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else if (itemType == ItemType.OldPaper)
            {
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
                AudioManager.Instance.Play_audioClip_PaperCrease();
                isInteracted = true;
                StartCoroutine(OldPaperAnimate());
            }
            else if (itemType == ItemType.Box)
            {
                GetComponent<BoxScript>().Interact();
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
            }
            else if (itemType == ItemType.MailBox)
            {
                GetComponent<MailBoxController>().Interact();
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
            }
            else if (itemType == ItemType.OrderBox)
            {
                GetComponent<OrderBox>().Interact();
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
            }
            else if (itemType == ItemType.Bed)
            {
                if (DayNightManager.Instance.time < 3600 * DayNightManager.Instance.sunSetHour && DayNightManager.Instance.time > 3600 * DayNightManager.Instance.sunRiseHour)
                {
                    // Player can't sleep before sun set
                    GameCanvas.Instance.Show_Warning_Not("You can't go to bed before sunset!", false);
                    return;
                }
                StartCoroutine(SleepNow());
                if (eventToInvokeWhenInteract != null)
                {
                    eventToInvokeWhenInteract.Invoke();
                }
            }
            GameCanvas.Instance.Hide_Warning();
        }

        private void OnDestroy()
        {
            if (HeroPlayerScript.Instance != null && HeroPlayerScript.Instance.InteractableObject != null && HeroPlayerScript.Instance.InteractableObject == this)
            {
                HeroPlayerScript.Instance.InteractableObject = null;
            }
        }

        public void UpdateSprite()
        {
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = imageToFill.fillAmount;
                sprite.color = color;
            }
        }

        IEnumerator OldPaperAnimate()
        {
            Animation[] animations = GetComponentsInChildren<Animation>();
            for (int i = 0; i < animations.Length; i++)
            {
                animations[i].Play();
            }
            yield return new WaitForSeconds(1.1f);
            GetComponent<Rigidbody>().isKinematic = false;
            transform.GetChild(1).gameObject.SetActive(false);
            GetComponent<Rigidbody>().useGravity = true;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            yield return new WaitForSeconds(2);
            if (GetComponent<ObjectStatusTracker>() != null)
            {
                GetComponent<ObjectStatusTracker>().DestroyObjectForever();
            }
        }

        IEnumerator SleepNow()
        {
            HeroPlayerScript.Instance.DeactivatePlayer();
            CameraScript.Instance.enabled = false;
            GameCanvas.Instance.image_Blinking.GetComponent<Animation>().Play("GotoSleep");
            yield return new WaitForSeconds(2f);
            HeroPlayerScript.Instance.Heal();
            DayNightManager.Instance.Sleep();
            HeroPlayerScript.Instance.Rest();
            GameCanvas.Instance.image_Blinking.GetComponent<Animation>().Play("Blink");
            yield return new WaitForSeconds(2.5f);
            HeroPlayerScript.Instance.ActivatePlayer();
            CameraScript.Instance.enabled = true;
        }

        public void DeactivateCollidersAndRigidbody()
        {
            Collider[] allColliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                allColliders[i].enabled = false;
                transform.tag = "Untagged";
            }
            if (GetComponent<Rigidbody>() != null)
            {
                GetComponent<Rigidbody>().isKinematic = true;
                GetComponent<Rigidbody>().useGravity = false;
            }
        }


        public void ActivateCollidersAndRigidbody()
        {
            transform.localScale = new Vector3(1, 1, 1);
            Collider[] allColliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                allColliders[i].enabled = true;
                transform.tag = "Item";
            }
            if (GetComponent<Rigidbody>() != null)
            {
                GetComponent<Rigidbody>().isKinematic = false;
                GetComponent<Rigidbody>().useGravity = true;
            }
        }

        public Image imageToFill;
        public float lastTime = 0;

        private void OnTriggerEnter(Collider other)
        {
            if (!this.enabled) return;

            if (other.CompareTag("Item") && other.GetComponent<ItemScript>() != null  && itemType == ItemType.Bin)
            {
                // Let's send it to Bin
                if (GetComponent<BinScript>().isOpened)
                {
                    if (other.GetComponent<BoxScript>() != null && other.GetComponent<BoxScript>().rigidbody.isKinematic == false)
                    {
                        if (other.GetComponent<ObjectStatusTracker>() != null)
                        {
                            other.GetComponent<ObjectStatusTracker>().DestroyObjectForever();
                        }
                        else
                        {
                            Destroy(other.gameObject);
                        }
                    }
                    else if(other.GetComponent<ItemScript>() != null && other.GetComponent<OrderBox>().rigidbody.isKinematic == false && other.GetComponent<OrderBox>().isEmpty())
                    {
                        other.GetComponent<OrderBox>().DestroyTheOrderBox();
                    }
                }
            }
        }
    }

    public enum ItemType
    {
        Door,
        None,
        Box,
        Bed,
        ItemToMaintain,
        NPC,
        Shop,
        MoneyBag,
        Seller,
        Bin,
        Equipment,
        OldPaper,
        OrderBox,
        CashRegister,
        PriceTag,
        Banknote,
        HangingSign,
        MailBox
    }

    public enum InteractionType
    {
        Grab,
        Sleep,
        Carry,
        Open,
        Clean,
        Talk,
        Design,
        Sit,
        None,
        Remove,
        Interact,
        Build,
        Rent,
        Flip,
        Check
    }
}