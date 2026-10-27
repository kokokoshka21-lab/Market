using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;

namespace MarketShopandRetailSystem
{
    public class DayNightManager : MonoBehaviour
    {
        [HideInInspector]
        public float time;
        public TimeSpan currenttime;
        public Transform SunTransform;
        public Light Sun;
        public Text timetext;
        public Text daytext;

        [HideInInspector]
        public int day;
        private float intensity;
        public Color ambienceColorNight;
        private int speed = 128;
        public float sunRiseHour = 8;
        public float sunSetHour = 20;
        public static DayNightManager Instance;
        public Material skyboxMaterial;

        [HideInInspector]
        public bool isDark = false;

        public List<Light> lights;

        public UnityEvent EventToInvokeWhenSunRise;
        public UnityEvent EventToInvokeWhenSunSet;


        private void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            ChangeTime();
        }

        private void Start()
        {
            skyboxMaterial.SetFloat("_Exposure", 1);
            RenderSettings.ambientIntensity = 1;
            time = 86400f * (sunRiseHour / 24f);
            day = SaveLoadManager.Instance.GetCurrentDay();
            daytext.text = "DAY " + day.ToString();
        }

        public void Sleep()
        {
            if(time > 0 && time < 3600 * sunRiseHour)
            {
                // day is already counted.
            }
            else
            {
                day += 1;
            }
            SaveLoadManager.Instance.SetCurrentDay(day);
            time = 3600 * sunRiseHour;
            daytext.text = "DAY " + day.ToString();
            HeroPlayerScript.Instance.InteractableObject = null;
        }

        private int hour = 0;
        private int minutes = 0;

        public void ChangeTime()
        {
            time += Time.deltaTime * speed;
            if (time > 86400)
            {
                day += 1;
                time = 0;
                daytext.text = "DAY " + day.ToString();
                SaveLoadManager.Instance.SetCurrentDay(day);
            }

            currenttime = TimeSpan.FromSeconds(time);
            string[] temptime = currenttime.ToString().Split(":"[0]);
            minutes = Convert.ToInt32(temptime[1]);
            hour = Convert.ToInt32(temptime[0]);
            timetext.text = temptime[0] + ":" + temptime[1];

            if (time > 43200)
                intensity = 1 - (43200 - time) / 43200;
            else
                intensity = 1 - ((43200 - time) / 43200 * -1);

            Sun.intensity = 1.8f - intensity;
            float currentRotation = RenderSettings.skybox.GetFloat("_Rotation");
            RenderSettings.skybox.SetFloat("_Rotation", currentRotation + 0.5f * Time.deltaTime);

            if (time < sunRiseHour * 3600 || time > sunSetHour * 3600)
            {
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(skyboxMaterial.GetFloat("_Exposure"), Sun.intensity, 0.02f));
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, ambienceColorNight, 0.02f);
                if (!isDark)
                {

                    for (int i = 0; i < lights.Count; i++)
                    {
                        lights[i].enabled = true;
                    }
                    isDark = true;
                    if(EventToInvokeWhenSunSet != null)
                    {
                        EventToInvokeWhenSunSet.Invoke();
                    }
                }
            }
            else
            {
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(skyboxMaterial.GetFloat("_Exposure"), 1, 0.02f));
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, Color.white, 0.02f);
                if (isDark)
                {
                    for (int i = 0; i < lights.Count; i++)
                    {
                        lights[i].enabled = false;
                    }
                    isDark = false;
                    if (EventToInvokeWhenSunRise != null)
                    {
                        EventToInvokeWhenSunRise.Invoke();
                    }
                    HeroPlayerScript.Instance.InteractableObject = null;
                }
            }
        }
    }
}