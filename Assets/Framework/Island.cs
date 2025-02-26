using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using Random = UnityEngine.Random;

namespace frost
{
    public class Island : MonoBehaviour
    {
        [SerializeField] private Transform islandBase;
        public Transform gateRef;
        public Transform camPos;
        [NonSerialized] public bool spawnIsland;
        
        // Dimensions
        [SerializeField] private float sideColliderHeight, sideColliderWidth;
        private BoxCollider[] sideColliders;
        public Vector3 size { private set; get; }
        private Bounds bounds;
        
        // Query
        private Collider[] overlapColliders = new Collider[4];
        [SerializeField] private LayerMask playerMask;
        
        // Gameplay
        public enum IslandState
        {
            Ready,
            Engaged,
            Cleared
        }

        // Spawning
        [SerializeField] private Vector2Int enemyCountRange;
        [SerializeField] private Vector2Int roundRange;
        [SerializeField] public GameObject[] enemyPrefabs;
        public Enemy[] spawnedEnemies;

        // Island is "Ready" by default
        public IslandState currentState = IslandState.Ready;
        private bool startingRound;
        [NonSerialized] public int rounds;
        [NonSerialized] public int currentRound;

        // borderobject
        [SerializeField] private GameObject borderObject;
        private static int clearedIslands = 0;
        [SerializeField] private float difficultyMult = 0.3f;

        [SerializeField] private GameObject bottomBlock;
        public float offset;
        
        public void Init(Bounds bounds)
        {
            rounds = (int)(Random.Range(roundRange.x, roundRange.y) + (difficultyMult * clearedIslands));
            this.bounds = bounds;
            size = bounds.size;
            transform.position = bounds.center;
            islandBase.localScale = size;
           
            // move base downwards
            islandBase.transform.localPosition =  -transform.up * size.y / 2f;
            
            // generate blocks on the outside (outline)
            sideColliders = new BoxCollider[5];
            
            // front
            sideColliders[0] = gameObject.AddComponent<BoxCollider>();
            sideColliders[0].center = islandBase.forward * ((size.z + sideColliderWidth) / 2) + transform.up * sideColliderHeight / 2f;
            sideColliders[0].size = new Vector3(size.x, sideColliderHeight, sideColliderWidth);
           
            // back
            sideColliders[1] = gameObject.AddComponent<BoxCollider>();
            sideColliders[1].center = -islandBase.forward * ((size.z + sideColliderWidth) / 2) + transform.up * sideColliderHeight / 2f;
            sideColliders[1].size = new Vector3(size.x, sideColliderHeight, sideColliderWidth);
           
            // right
            sideColliders[2] = gameObject.AddComponent<BoxCollider>();
            sideColliders[2].center = islandBase.right * ((size.x + sideColliderWidth) / 2) + transform.up * sideColliderHeight / 2f;
            sideColliders[2].size = new Vector3(sideColliderWidth, sideColliderHeight, size.z);
           
            // left
            sideColliders[3] = gameObject.AddComponent<BoxCollider>();
            sideColliders[3].center = -islandBase.right * ((size.x + sideColliderWidth) / 2) + transform.up * sideColliderHeight / 2f;
            sideColliders[3].size = new Vector3(sideColliderWidth, sideColliderHeight, size.z);
           
            // top
            sideColliders[4] = gameObject.AddComponent<BoxCollider>();
            sideColliders[4].center = transform.up * (sideColliderHeight + sideColliderWidth / 2f);
            sideColliders[4].size = new Vector3(size.x, sideColliderWidth, size.z);

            for (int i = 0; i < 5; ++i)
            {
                sideColliders[i].enabled = false;
            }
            
            // Generate blocks on bottom
            for (int i = 0; i < 30; i++)
            {
                Vector3 point = new Vector3
                (
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y) - bounds.size.y - Mathf.Pow(i, 3) * offset,
                    Random.Range(bounds.min.z, bounds.max.z)
                );
                GameObject g = Instantiate(bottomBlock, point, Quaternion.identity, transform);
                Vector3 s = new Vector3
                (
                    Random.Range(0.2f, 1f),
                    Random.Range(0.2f, 1f),
                    Random.Range(0.2f, 1f)
                ) * 20f;
                g.transform.localScale = s;
            }
            
            //GenerateBorder(20);
        }
        
        public void GenerateBorder(int numItems)
        {
            // sine/cos to create circle -> large exponents to make it more like a square
            // ik this isnt very elegant...

            float step = 2 * Mathf.PI / numItems;
            float current = 0f;

            GameObject container = new GameObject("container");
            container.transform.parent = transform;
            container.transform.localPosition = Vector3.zero;

            float signA, signB;
        }

        public void FixedUpdate()
        {
        }

        public void Update()
        {
            if (currentState is IslandState.Engaged)
            {
                bool clear = true;
                for (int i = 0; i < spawnedEnemies.Length; i++)
                {
                    if (!spawnedEnemies[i].dead)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear && !startingRound)
                {
                    if (currentRound < rounds)
                    {
                        int num = (int)(Random.Range(enemyCountRange.x, enemyCountRange.y) + (difficultyMult * clearedIslands));
                        PlayerCore.mainPlayerCore.combat.UpdateRound();
                        Timing.RunCoroutine(SpawnDelay(num));
                        currentRound++;
                    }
                    else
                    {
                        PlayerCore.mainPlayerCore.combat.UpdateRound();
                        clearedIslands++;
                        Clear();
                        // complete
                    }
                }
            }
        }

        private IEnumerator<float> SpawnDelay(int num)
        {
            startingRound = true;
            yield return Timing.WaitForSeconds(2f);
            spawnedEnemies = new Enemy[num];
            
            for (int i = 0; i < num; i++)
            {
                GameObject currentEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length - 1)];;
                Vector3 pos = transform.position + transform.up +
                              new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 5f;
                Vector3 lookDir = (PlayerCore.mainPlayerCore.transform.position - pos);
                lookDir.y = 0f;
                lookDir.Normalize();
                GameObject instance = Instantiate(currentEnemy.gameObject, pos, Quaternion.LookRotation(lookDir), transform);
                Enemy component = instance.transform.Find("Body").GetComponent<Enemy>();
                spawnedEnemies[i] = component;
            }

            startingRound = false;
        }

        public void Engage()
        {
            currentState = IslandState.Engaged;
            
            // Enable side colliders
            for (int i = 0; i < sideColliders.Length; i++)
            {
                //sideColliders[i].enabled = true;
            }

            currentRound = 1;
            int num = (int)(Random.Range(enemyCountRange.x, enemyCountRange.y) + (difficultyMult * clearedIslands));
            Timing.RunCoroutine(SpawnDelay(num));
        }

        public void Clear()
        {
            currentState = IslandState.Cleared;
            PlayerCore.mainPlayerCore.combat.ClearEnemy();
            GameMan.gameMan.SpawnTravel(this);
        }
    }
}
