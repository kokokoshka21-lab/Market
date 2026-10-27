using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MarketShopandRetailSystem
{
    public class SpeechManager : MonoBehaviour
    {
        public GameObject Panel_Speech;
        public Text Text_Speaker;
        public Text Text_SpeechText;
        public static SpeechManager instance;
        private void Awake()
        {
            instance = this;
        }
        Coroutine lastRoutine = null;
        public void Show_Speach(string speech, string speaker, GameObject speakerObject)
        {
            LastSpeakerObject = speakerObject;
            Text_Speaker.text = speaker;
            Text_SpeechText.text = "";
            if (lastRoutine != null)
            {
                StopCoroutine(lastRoutine);
            }
            lastRoutine = StartCoroutine(ShowText(speech));
            Panel_Speech.SetActive(true);
            if (hideSpeech != null)
            {
                StopCoroutine(hideSpeech);
            }
            hideSpeech = HidetheSpeech();
            lastTimePressE = Time.time;
            StartCoroutine(hideSpeech);
        }

        private IEnumerator hideSpeech;

        IEnumerator ShowText(String speech)
        {
            for (int i = 0; i < speech.Length; i++)
            {
                yield return new WaitForSeconds(0.02f);
                Text_SpeechText.text += speech[i];
            }
        }

        IEnumerator HidetheSpeech()
        {
            yield return new WaitForSeconds(8);
            Panel_Speech.SetActive(false);
        }

        private float lastTimePressE = 0;
        private float lastTimeCheckSpeaker = 0;
        private GameObject LastSpeakerObject;

        private void Update()
        {
            if (AdvancedGameManager.Instance.interactAction.WasReleasedThisFrame() && !Panel_Speech.activeSelf)
            {
                lastTimePressE = Time.time;
            }
            if (Time.time > lastTimePressE + 8 && Panel_Speech.activeSelf)
            {
                Panel_Speech.SetActive(false);
            }
            if(Time.time > lastTimeCheckSpeaker+0.5f && Panel_Speech.activeSelf && LastSpeakerObject != null)
            {
                lastTimeCheckSpeaker = Time.time;
                float distance = Vector3.Distance(HeroPlayerScript.Instance.transform.position, LastSpeakerObject.transform.position);
                if(distance > 6)
                {
                    Panel_Speech.SetActive(false);
                    if(LastSpeakerObject.GetComponent<CivilianController>() != null)
                    {
                        LastSpeakerObject.GetComponent<CivilianController>().GoSomewhere();
                    }
                    LastSpeakerObject = null;
                }
            }
        }
    }
}