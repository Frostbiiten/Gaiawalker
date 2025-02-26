using System;
using UnityEngine;

namespace frost
{
    [Serializable]
    public class PlayerGrounded : State
    {
        public float currentSpeed { private set; get; }
        private Vector3 forwardDirection, kb;
        private float veloDot;
        
        [Header("Jump")]
        private bool jumped, fixedUpdateJump;
        
        public override StateID id
        {
            get 
            {
                return StateID.Grounded; 
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

        public void TriggerJump()
        {
            fixedUpdateJump = true;
        }

        public void SetSpeed(float s)
        {
            currentSpeed = s;
        }
        
        // This will run every frame
        public override void Update()
        {
            playerCore.animations.playerAnimator.SetFloat("GroundSpeed", playerCore.grounded.currentSpeed - kb.magnitude);
            
            // Normal step
            if(playerCore.directionalInput && playerCore.combat.currentAttackState == AttackState.None && playerCore.combat.hitStop <= 0)
            {
                //forwardDirection = Vector3.Slerp(forwardDirection, playerCore.inputDirection, 1.0f - Mathf.Pow(0.0f, Time.deltaTime));
                forwardDirection = Vector3.MoveTowards(forwardDirection,
                    Vector3.ProjectOnPlane(playerCore.inputDirection, playerCore.rayResult.normal),
                    playerCore.stats.groundTurnSpeed.Evaluate(currentSpeed)).normalized;
            }
        }
        
        void Jump()
        {
            //playerCore.rb.velocity += Vector3.up * jumpForce;
            playerCore.animations.playerAnimator.Play("Jump", 0, 0f);
            playerCore.rb.linearVelocity += playerCore.transform.up * playerCore.stats.jumpForce;
            playerCore.ChangeState(playerCore.airborne);
            //playerCore.airborne.jumpTimer = 0f;
            fixedUpdateJump = false;
            jumped = true;
        }
        
        void ProcessMovement()
        {
            veloDot = Vector3.Dot((playerCore.velocity - kb).normalized, playerCore.inputDirection.normalized);

            if (playerCore.velocityMagnitude < currentSpeed) currentSpeed = playerCore.velocityMagnitude;
            
            if (playerCore.directionalInput)
            {
                if (veloDot >= 0.0f)
                {
                    float target = playerCore.stats.groundTopSpeed;
                    if (playerCore.playerInput.walk) target = playerCore.stats.groundBaseSpeed;

                    if (veloDot < 0.5f) veloDot = 0.5f;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, target, playerCore.stats.groundAcceleration * veloDot);
                }
                else
                {
                    currentSpeed = Mathf.Max(currentSpeed - playerCore.stats.groundDeceleration, 0.0f);
                }
            }
            else
            {
                currentSpeed = Mathf.Max(currentSpeed - playerCore.stats.groundFriction, 0.0f);
            }

            Vector3 upDir = Vector3.up;
            if (playerCore.floorRayDetected)
            {
                upDir = playerCore.rayResult.normal;
            }
            
            playerCore.velocity = forwardDirection * currentSpeed + kb;
            
            // decrease kb
            kb *= 0.925f;
        }

        public void SetForward(Vector3 forward)
        {
            forwardDirection = forward;
        }
        
        public override void FixedUpdate()
        {
            ProcessMovement();

            if(!playerCore.floorDetected) playerCore.ChangeState(playerCore.airborne);
            if (fixedUpdateJump)
            {
                Jump();
                return;
            }

            if (playerCore.floorDetected)
            {
                if ((Quaternion.FromToRotation(Vector3.up, playerCore.rayResult.normal) * playerCore.velocity).y < 0.1f)
                {
                    jumped = false;
                }
            }
            
            if (jumped) return;

            if (playerCore.floorDistance < playerCore.stats.fallDistThreshold && !jumped)
            {
                //playerSM.playerCore.transform.position += Vector3.down * floorDist;
                playerCore.transform.position = Vector3.Lerp(playerCore.transform.position, playerCore.transform.position - playerCore.rayResult.normal * playerCore.floorDistance, 0.5f);
                // playerCore.rb.position = Vector3.Lerp(playerCore.rb.position, playerCore.rb.position - playerCore.transform.up * playerCore.floorDistance, 0.5f);
            }

            if (playerCore.combat.currentAttackState == AttackState.None)
            {
                if (forwardDirection.sqrMagnitude > 0.01f)
                {
                    playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(forwardDirection, playerCore.rayResult.normal), playerCore.rayResult.normal), 0.3f));
                }
                else
                {
                    if (kb.sqrMagnitude > 0.01f)
                    {
                        playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(-kb.normalized, playerCore.rayResult.normal), playerCore.rayResult.normal), 0.3f));
                    }
                    else
                    {
                        playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCore.transform.forward, playerCore.rayResult.normal), playerCore.rayResult.normal), 0.9f));
                    }
                }
            }
            else
            {
                playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.transform.rotation, Quaternion.LookRotation(playerCore.combat.attackDir, playerCore.rayResult.normal), 0.3f));
            }
        }

        public override bool Enter(State from)
        {
            playerCore.groundedCollider.enabled = true;
            playerCore.baseCollider.enabled = false;
            
            Vector3 upDir = Vector3.up;
            if (playerCore.floorRayDetected) upDir = playerCore.rayResult.normal;
            
            Vector3 projectedVelocity = Vector3.ProjectOnPlane(playerCore.velocity, upDir);

            if (!playerCore.airborne.airHit)
            {
                forwardDirection = projectedVelocity.normalized;
                currentSpeed = projectedVelocity.magnitude;
                kb = Vector3.zero;
            }
            else
            {
                // 
                playerCore.airborne.airHit = false;
                forwardDirection = -projectedVelocity.normalized;
                currentSpeed = 0f;
                kb = projectedVelocity;
                playerCore.animations.playerAnimator.Play("GroundedDefault", 0, 0f);
            }
            
            jumped = false;
            
            return true;
        }

        public override bool Exit(State to)
        {
            playerCore.groundedCollider.enabled = false;
            playerCore.baseCollider.enabled = true;
            return true;
        }
    }
}
