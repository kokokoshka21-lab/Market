using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;
        public AudioClip Door_Wooden_Open;
        public AudioClip Door_Close;
        public AudioClip Item_Grab;
        public AudioClip Audio_Breathing;
        public AudioClip Audio_Jump;
        public AudioClip Audio_Cabinet_Open;
        public AudioClip Audio_Drawer_Open;
        public AudioSource audioSource;
        public AudioSource audioSourceWalk;
        public AudioClip audioClip_ObjectiveCompleted;
        public AudioClip audioClip_ObjectiveAssigned;
        public AudioClip audioClip_Cleaning;
        public AudioClip audioClip_Beep;
        public AudioClip audioClip_CashRegisterDone;
        public AudioClip audioClip_CashRegisterError;
        public AudioClip audioClip_KeyboardPress;
        public AudioClip audioClip_Building;
        public AudioClip[] audioClip_PaperCrease;
        public AudioClip[] audioClip_Banknotes;

        public AudioClip audioClip_Coin;
        public AudioClip audioClip_Locate;
        public AudioClip audioClip_Experience;
        public AudioClip audioClip_BinOpen;
        public AudioClip audioClip_BinClose;
        public AudioSource AudioSource_Ambience;

        [Header("Press and Hold Sound Effects")]
        public AudioClip Audio_PressAndHoldMaintainDone;

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Play_Audio_Cleaning()
        {
            if(!audioSource.isPlaying)
            {
                audioSource.clip = audioClip_Cleaning;
                audioSource.Play();
            }
        }

        public void Stop_Audio_Cleaning()
        {
            audioSource.Stop();
        }

        public void Play_Audio_BinOpen()
        {
            audioSource.PlayOneShot(audioClip_BinOpen, 1);
        }

        public void Play_Audio_BinClose()
        {
            audioSource.PlayOneShot(audioClip_BinClose, 1);
        }

        public void Play_audioClip_PaperCrease()
        {
            audioSource.PlayOneShot(audioClip_PaperCrease[Random.Range(0, audioClip_PaperCrease.Length)]);
        }

        public void Play_Banknotes()
        {
            audioSource.PlayOneShot(audioClip_Banknotes[Random.Range(0, audioClip_Banknotes.Length)]);
        }
        

        public void Play_audioClip_Beep()
        {
            audioSource.PlayOneShot(audioClip_Beep, 1);
        }

        public void Play_audioClip_KeyboardPress()
        {
            audioSource.PlayOneShot(audioClip_KeyboardPress, 1);
        }

        public void Play_audioClip_CashRegisterResult(bool result)
        {
            if(result)
            {
                audioSource.PlayOneShot(audioClip_CashRegisterDone, 1);
            }
            else
            {
                audioSource.PlayOneShot(audioClip_CashRegisterError, 1);
            }
        }

        public void Play_audioClip_Experience()
        {
            audioSource.PlayOneShot(audioClip_Experience);
        }

        public void Play_audioClip_Coin()
        {
            audioSource.PlayOneShot(audioClip_Coin, 0.5f);
        }

        public void Play_audioClip_Locate()
        {
            audioSource.PlayOneShot(audioClip_Locate, 1);
        }
        

        public void Stop_Audio_Reparing()
        {
            audioSource.Stop();
        }

        public void Play_Audio_Building()
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = audioClip_Building;
                audioSource.Play();
            }
        }

        public void Stop_Audio_Building()
        {
            audioSource.Stop();
        }

        public void Play_ObjectiveCompleted()
        {
            audioSource.PlayOneShot(audioClip_ObjectiveCompleted);
        }

        public void Play_ObjectiveAssigned()
        {
            audioSource.PlayOneShot(audioClip_ObjectiveAssigned);
        }

        public void Play_Audio_PressAndHoldMaintainDone()
        {
            audioSource.PlayOneShot(Audio_PressAndHoldMaintainDone);
        }

        public void Play_Jump()
        {
            audioSource.pitch = 1;
            audioSource.PlayOneShot(Audio_Jump);
        }

        public void Play_Audio_Cabinet_Open()
        {
            audioSource.PlayOneShot(Audio_Cabinet_Open);
        }

        public void Play_Audio_Drawer_Open()
        {
            audioSource.PlayOneShot(Audio_Drawer_Open);
        }

        public void Play_Door_Wooden_Open()
        {
            audioSource.PlayOneShot(Door_Wooden_Open);
        }

        public void Play_Audio_Breathing()
        {
            audioSource.PlayOneShot(Audio_Breathing);
        }

        public void Play_Door_Close()
        {
            audioSource.PlayOneShot(Door_Close);
        }

        public void Play_Item_Grab()
        {
            audioSource.PlayOneShot(Item_Grab);
        }
    }
}