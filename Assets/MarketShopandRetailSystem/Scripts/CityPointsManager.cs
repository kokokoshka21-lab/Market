using System.Collections.Generic;
using UnityEngine;

namespace MarketShopandRetailSystem
{
    public class CityPointsManager : MonoBehaviour
    {
        public List<Transform> SpawnPoints;
        public List<Transform> TargetPoints;
        public List<Transform> Doors;
        public static CityPointsManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        public Vector3 GetRandomSpawnPoint()
        {
            int i = Random.Range(0, SpawnPoints.Count);
            Vector3 v = new Vector3(SpawnPoints[i].position.x + Random.Range(-1.5f, 1.5f), SpawnPoints[i].position.y, SpawnPoints[i].position.z + Random.Range(-1.5f, 1.5f));
            return v;
        }

        public Transform GetRandomTargetPoint()
        {
            return TargetPoints[Random.Range(0, TargetPoints.Count)];
        }
        public Transform GetRandomDoor()
        {
            return Doors[Random.Range(0, Doors.Count)];
        }
    }
}