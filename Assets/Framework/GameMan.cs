using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace frost
{
    public class GameMan : MonoBehaviour
    {
        [NonSerialized] public static GameMan gameMan;
        [SerializeField] private PlayerCore mainPlayer;
        [SerializeField] private MapGenerator mainGenerator;
        [SerializeField] private TextMeshProUGUI highscoreText;
        [SerializeField] private GameObject gatePrefab;
        private Island[] islands;
        
        void Awake()
        {
            gameMan = this;
            Init();
        }

        public Island[] GetIslands()
        {
            return islands;
        }
        
        private void Init()
        {
            mainGenerator.Generate();
            
            // choose random island
            islands = mainGenerator.GetIslands();
            int selected = Random.Range(0, islands.Length - 1);
            Island spawnIsland = islands[selected];
            spawnIsland.spawnIsland = true;

            // round to nearest 90
            spawnIsland.transform.rotation = Quaternion.LookRotation((transform.position - spawnIsland.transform.position).normalized);
            spawnIsland.transform.eulerAngles = new Vector3(0, Mathf.Round(spawnIsland.transform.eulerAngles.y / 90f) * 90f, 0);
            
            // move player to center of island
            mainPlayer.rb.MovePosition(spawnIsland.transform.position + spawnIsland.transform.up * mainPlayer.playerHeight * 0.5f);
            mainPlayer.currentIsland = spawnIsland;
            
            // Spawn start prefab
            Transform gateRef = spawnIsland.gateRef;
            GameObject start = Instantiate(gatePrefab, gateRef.position, gateRef.rotation, spawnIsland.transform);
        }

        public void SpawnTravel(Island island)
        {
            Transform gateRef = island.gateRef;
            GameObject go = Instantiate(gatePrefab, gateRef.position, gateRef.rotation, island.transform);
        }
    }
}
