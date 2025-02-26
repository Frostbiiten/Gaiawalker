using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace frost
{
    [Serializable]
    public class PlayerAirborne : State
    {
        public float hitFly { get; private set; }
        public bool slideContact { get; private set; }
        [NonSerialized] public bool airHit;
        public bool bounceContact;
        private Vector3 hitflyVelo = Vector3.zero;
        [SerializeField] private ParticleSystem bounceVFX;
        [SerializeField] private TrailRenderer trailRenderer;
        
        public override StateID id
        {
            get 
            {
                return StateID.Airborne; 
            }
        }
        
        public override void Awake()
        {
            //throw new System.NotImplementedException();
        }

        public override void Start()
        {
            //throw new System.NotImplementedException();
        }
        
        // This will run every frame
        public override void Update()
        {
            hitFly -= Time.deltaTime;
        }

        public override void FixedUpdate()
        {
            if (hitFly > 0f || bounceContact || slideContact)
            {
                if (slideContact)
                {
                    Vector3 normal = Vector3.up;
                    if (playerCore.floorRayDetected)
                    {
                        normal = playerCore.rayResult.normal;
                    }
                    
                    hitflyVelo = Quaternion.FromToRotation(normal, Vector3.up) * hitflyVelo;
                    hitflyVelo.y = 0f;
                    hitflyVelo = Quaternion.FromToRotation(Vector3.up, normal) * hitflyVelo;
                    hitflyVelo *= 0.95f;
                    if (hitflyVelo.sqrMagnitude < 2f || !playerCore.floorDetected)
                    {
                        hitFly = 0f;
                        slideContact = false;
                        playerCore.animations.playerAnimator.CrossFade("Standup", 0.2f, 0, 0.05f);
                        trailRenderer.emitting = false;
                    }
                
                    playerCore.velocity = hitflyVelo;
                }
                else if (bounceContact)
                {
                    playerCore.velocity = Vector3.zero;
                }
                else
                {
                    playerCore.velocity = hitflyVelo;
                    hitflyVelo += playerCore.stats.gravity;
                    if (playerCore.rb.linearVelocity.magnitude < 4f)
                    {
                        hitFly = 0f;
                        slideContact = false;
                        trailRenderer.emitting = false;
                    }
                }
            }
            else
            {
                playerCore.velocity *= 1 - playerCore.stats.airDrag;
                playerCore.velocity += playerCore.stats.gravity;
                playerCore.velocity += playerCore.inputDirection * 0.7f;

                if (playerCore.floorDistance < playerCore.stats.landDistThreshold || playerCore.floorDetected)
                {
                    playerCore.ChangeState(playerCore.grounded);
                    
                    // * without if is messing animations up
                    if (playerCore.animations.playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Airborne"))
                    {
                        playerCore.animations.playerAnimator.Play("GroundedDefault", 0, 0f);
                    }
                    return;
                }
            }

            // Don't rotate normally if airhit
            if (!airHit)
            {
                if (playerCore.combat.currentAttackState == AttackState.None)
                {
                    if (Vector3.ProjectOnPlane(playerCore.velocity, Vector3.up).sqrMagnitude > 10f)
                    {
                        playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCore.velocity, Vector3.up).normalized), 0.1f));
                    }
                    else
                    {
                        playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCore.inputDirection, Vector3.up).normalized), 0.02f));
                    }
                }
                else
                {
                    playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(playerCore.combat.attackDir, playerCore.rayResult.normal), 0.3f));
                }
            }
            else
            {
                playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCore.transform.forward, Vector3.up).normalized), 0.02f));
            }
        }

        public void SetHitfly(Vector3 velocity, float time)
        {
            slideContact = false;
            hitFly = time;
            hitflyVelo = velocity;
            playerCore.animations.playAnimation("Hitfar");
        }
        private IEnumerator<float> Bounce(Vector3 normal)
        {
            if (Vector3.Dot(hitflyVelo, normal) < 0)
            {
                bounceContact = true;
                bounceVFX.transform.up = normal;
                bounceVFX.Play();
                yield return Timing.WaitForSeconds(0.125f);
                Vector3 vel = Vector3.Reflect(hitflyVelo, normal);
                vel *= 0.55f;
                vel = Quaternion.FromToRotation(normal, Vector3.up) * vel;
                vel.y *= 0.5f;
                vel = Quaternion.FromToRotation(Vector3.up, normal) * vel;
                hitflyVelo = vel;
                bounceContact = false;
            }
        }

        public override void OnCollisionEnter(Collision collision)
        {
            if (hitFly > 0f && !bounceContact && !slideContact)
            {
                Timing.RunCoroutine(Bounce(collision.contacts[0].normal));
                if (collision.contacts[0].otherCollider.gameObject.CompareTag("Floor") && hitflyVelo.y is < 0 and > -20)
                {
                    playerCore.animations.playerAnimator.SetFloat("HitSpinSpeed", 0.01f);
                    playerCore.animations.playerAnimator.Play("Hitfar", 0, 0f);
                    slideContact = true;
                }
            }
        }

        public override bool Enter(State from)
        {
            if (hitFly > 0)
            {
                playerCore.animations.playerAnimator.SetFloat("HitSpinSpeed", 1f);
                trailRenderer.emitting = true;
            }
            return true;
        }
        public override bool Exit(State to)
        { 
            trailRenderer.emitting = false;
            return true;
        }
    }
}
