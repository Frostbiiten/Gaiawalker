using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace  frost
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private int numIslands;
        [SerializeField] private Vector3 boundsSize;
        [SerializeField] private Island baseIsland;
        [SerializeField] private Vector3 sizeMin, sizeMax;
        
        private Island[] islandInstances;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, boundsSize);
        }

        public void Generate()
        {
            Bounds[] positions = new Bounds[numIslands];
            Bounds b = new Bounds(transform.position, boundsSize);
            for (int i = 0; i < numIslands; i++)
            {
                Vector3 islandPos = new Vector3(
                    Random.Range(b.min.x, b.max.x),
                    Random.Range(b.min.y, b.max.y),
                    Random.Range(b.min.z, b.max.z)
                );

                Vector3 size = new Vector3(
                    Random.Range(sizeMin.x, sizeMax.x),
                    Random.Range(sizeMin.y, sizeMax.y),
                    Random.Range(sizeMin.z, sizeMax.z));
                
                Bounds newBounds = new Bounds(islandPos, size);
                
                for (int attempts = 0; attempts < 10; attempts++)
                {
                    bool br = false;
                    
                    islandPos = new Vector3(
                        Random.Range(b.min.x, b.max.x),
                        Random.Range(b.min.y, b.max.y),
                        Random.Range(b.min.z, b.max.z)
                    );
                    
                    size = new Vector3(
                        Random.Range(sizeMin.x, sizeMax.x),
                        Random.Range(sizeMin.y, sizeMax.y),
                        Random.Range(sizeMin.z, sizeMax.z));
                    
                    newBounds = new Bounds(islandPos, size);
                    
                    // inflate
                    Bounds inflatedBounds = new Bounds(islandPos, size);
                    inflatedBounds.Expand(20f);
                    
                    for (int j = 0; j < i; j++)
                    {
                        if (inflatedBounds.Intersects(positions[j]))
                        {
                            break;
                        }

                        if (j == i - 1) br = true;
                    }

                    if (br) break;
                }

                positions[i] = newBounds;
            }

            islandInstances = new Island[numIslands];
            for (int i = 0; i < numIslands; i++)
            {
                Island l = Instantiate(baseIsland.gameObject).GetComponent<Island>();
                l.Init(positions[i]);
                islandInstances[i] = l;
            }
        }

        public Island[] GetIslands()
        {
            return islandInstances;
        }
    }
}
