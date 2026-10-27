using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class WeatherManager : MonoBehaviour
    {
        public ParticleSystem rainParticleSystem;

        private float weatherChangeTimer;
        public float weatherChangeInterval = 10f;

        void Start()
        {
            weatherChangeTimer = weatherChangeInterval;
            ChangeWeather(); 
        }

        void Update()
        {
            weatherChangeTimer -= Time.deltaTime;

            if (weatherChangeTimer <= 0)
            {
                ChangeWeather();
                weatherChangeTimer = weatherChangeInterval;
            }
        }

        void ChangeWeather()
        {
            // 0: Rain, 1: Clear
            int weatherType = Random.Range(0, 5);
            if(weatherType == 0)
            {
                rainParticleSystem.Play();
            }
            else
            {
                rainParticleSystem.Stop();
            }
        }
    }
}
