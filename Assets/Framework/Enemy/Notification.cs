using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace frost
{
    public class Notification : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI text;
        [SerializeField] private float lifetime;
        
        void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
