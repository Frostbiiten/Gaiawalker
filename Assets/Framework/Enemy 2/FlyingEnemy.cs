using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace frost
{
    public class FlyingEnemy : Enemy
    {
        // Visual
        [SerializeField] private float playerRange, fov;
        [SerializeField] private float shakeFreq, shakeAmp;
        [SerializeField] private VisualEffect attackWarning;
        [SerializeField] private Transform skin;
        private bool playerDetected;
        
        // Movement
        [SerializeField] private float hoverForce, hoverHeight;
        [SerializeField] private float movementForce;
        [SerializeField] private float aimDistance;
        [SerializeField] private float lookForce;
        [SerializeField] private float hitForce;
        [SerializeField] private LayerMask floorMask;
        private Vector3 hitTorque;
        
        // Attack
        [SerializeField] private Transform rotator;
        [SerializeField] private float reloadTime;
        private float currentTimer = 0f;
        private float rotatorSpeed = 0f;
            
        protected override bool _Damage(Hit hit, Vector3 direction)
        {
            if (!playerDetected)
            {
                playerDetected = true;
                
                Vector3 dir = PlayerCore.mainPlayerCore.transform.position - transform.position;
                dir.y = 0f;
                float distance = Vector3.Magnitude(dir);
                dir /= distance;
            
                // also check raycast
                if (distance <= playerRange && Vector3.Dot(dir, transform.forward) > fov)
                {
                    playerDetected = true;
                }
            }
            
            Vector3 u = Vector3.Slerp(Vector3.up, direction.normalized, 0.6f);
            Vector3 delta = Quaternion.LookRotation(Vector3.ProjectOnPlane(-direction.normalized, u), u).eulerAngles;
            delta.x = Mathf.Repeat(delta.x + 180f, 360f) - 180f;
            delta.y = Mathf.Repeat(delta.y + 180f, 360f) - 180f;
            delta.z = Mathf.Repeat(delta.z + 180f, 360f) - 180f;
            hitTorque = delta * hitForce;
            
            return true;
        }

        protected override void _Update()
        {
            if (currentTimer > 0f)
            {
                currentTimer -= Time.deltaTime;
                if (currentTimer < 0f)
                {
                    // shoot event, reset timer
                    currentTimer = reloadTime;
                }
            }
        }
        protected override void _FixedUpdate()
        {
            if (dead) return;
            
            if (!rb.isKinematic)
            {
                if (hitStop > 0)
                {
                    rb.linearVelocity = Vector3.zero;
                }
                else
                {
                    if (hitTorque != Vector3.zero)
                    {
                        rb.AddTorque(hitTorque, ForceMode.Impulse);
                        hitTorque = Vector3.zero;
                    }
                    
                    // start spinning when its gonna shoot
                    if (playerDetected)
                    {
                        Vector3 playerPos = PlayerCore.mainPlayerCore.transform.position;
                        
                        // get direction to player position
                        Vector3 dir =  playerPos - transform.position;
                        float playerDist = dir.magnitude;
                        dir /= playerDist;
                        
                        // ofset has no y
                        Vector3 offset = -dir;
                        offset.y = 0;
                        offset.Normalize();
                        
                        Vector3 targetPos = playerPos + offset * aimDistance;
                        targetPos.y = transform.position.y;
                        Vector3 movement = (targetPos - transform.position).normalized * movementForce;
                        
                        float verticalForce = (playerPos.y + hoverHeight - transform.position.y) * hoverForce;
                        
                        // Raycast down from current pos
                        if (Physics.Raycast(transform.position, Vector3.down, out var hit, hoverHeight, floorMask.value))
                        {
                            verticalForce = (hit.point.y + hoverHeight - transform.position.y) * hoverForce;
                        }
                        
                        rb.AddForce(movement.x, verticalForce, movement.z);
                        
                        // Face player
                        Vector3 delta = Quaternion.FromToRotation(transform.forward, dir).eulerAngles;
                        delta.x = Mathf.Repeat(delta.x + 180f, 360f) - 180f;
                        delta.y = Mathf.Repeat(delta.y + 180f, 360f) - 180f;
                        delta.z = Mathf.Repeat(delta.z + 180f, 360f) - 180f;
                        rb.AddTorque(delta * lookForce);
                    }
                    else
                    {
                        float verticalForce = -rb.linearVelocity.y;
                        // Raycast down from current pos
                        if (Physics.Raycast(transform.position, Vector3.down, out var hit, hoverHeight, floorMask.value))
                        {
                            verticalForce = (hit.point.y + hoverHeight - transform.position.y) * hoverForce;
                        }
                        
                        rb.AddForce(rb.linearVelocity.x * -movementForce, verticalForce, rb.linearVelocity.z * -movementForce);
                        
                        // check for player
                        Vector3 dir = PlayerCore.mainPlayerCore.transform.position - transform.position;
                        float dist = dir.magnitude;
                        dir /= dist;
                        if (dist <= playerRange && Vector3.Dot(dir, transform.forward) > fov)
                        {
                            playerDetected = true;
                        }
                    }
                }
            }
            else
            {
                rb.position = stunPos + kb.normalized * (Mathf.Sin(stunTimer * shakeFreq) * shakeAmp * stunTimer);
            }

            rotatorSpeed += 0.01f;
            rotator.localRotation *= Quaternion.AngleAxis(rotatorSpeed, Vector3.up);
        }
    }
}
