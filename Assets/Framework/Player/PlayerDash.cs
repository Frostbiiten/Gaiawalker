using System;
using UnityEngine;
using UnityEngine.VFX;

namespace frost
{
    [Serializable]
    public class PlayerDash : State
    {
        private Vector3 normalizedVelocity;
        [SerializeField] private BoxCollider hitBounds;
        [SerializeField] private LayerMask defaultHitMask;
        [SerializeField] private LayerMask enemyLayerMask;
        private Collider[] detectedColliders = new Collider[10];
        public float dashTimer { private set; get; }
        private float oldDashTimer;

        [SerializeField] private ParticleSystem dashStartVFX;
        [SerializeField] private TrailRenderer dashTrailVFX;
        [SerializeField] private VisualEffect dashingVFX;
        private Enemy target;
        private bool fromGrounded;

        public override StateID id
        {
            get 
            {
                return StateID.Dash; 
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
            oldDashTimer = dashTimer;
            dashTimer += Time.deltaTime;

            if (dashTimer < 0f)
            {
                if (!playerCore.playerInput.dash)
                {
                    playerCore.ChangeState(playerCore.airborne);
                    playerCore.animations.playerAnimator.CrossFade("DashHit", 0.3f, 0, 0.2f);
                }
            }
            
            if (dashTimer > playerCore.stats.maxDashTime)
            {
                //End
                playerCore.animations.playAnimation("Airborne");
                playerCore.airborne.SetHitfly(playerCore.velocity, 0.5f);
                EndDash();
            }
            else if (dashTimer > 0 && oldDashTimer <= 0)
            {
                // Start
                playerCore.animations.playAnimation("Dashing");
                AudioManager.instance.PlaySound("Dash");
                playerCore.combat.AddEnergy(-1);
                dashingVFX.Play();
            }
        }

        public override void FixedUpdate()
        {
            // Rotate forward direction over time
            normalizedVelocity = Vector3.Slerp(normalizedVelocity, (target.transform.position - playerCore.transform.position).normalized, playerCore.stats.dashTurnSpeed).normalized;

            bool inDash = dashTimer > 0;

            if (inDash)
            {
                playerCore.velocity = normalizedVelocity * playerCore.stats.dashSpeed;
                playerCore.transform.rotation = Quaternion.LookRotation(normalizedVelocity);
                dashingVFX.transform.rotation = Quaternion.identity;
                
                // Check for collisions to end state
                int overlaps = Physics.OverlapBoxNonAlloc(
                   hitBounds.transform.position, 
                   hitBounds.size,
                   detectedColliders, 
                   hitBounds.transform.rotation, defaultHitMask);

                for (int i = 0; i < overlaps; i++)
                {
                    Collider current = detectedColliders[i];

                    // Enemy detected
                    if ((enemyLayerMask & (1 << current.gameObject.layer)) != 0)
                    {
                        playerCore.animations.playerAnimator.Play("DashHit", 0, 0f);
                        playerCore.combat.Attack((int)PlayerAttack.DashHit);
                    }
                    else
                    {
                        playerCore.animations.playerAnimator.Play("Hurt");
                    }

                    // Change state after collision
                    playerCore.rb.linearVelocity = Vector3.zero;
                    playerCore.playerCam.cameraShake.ShakePresets.Explosion3D(12f, 0.5f);
                    playerCore.grounded.SetSpeed(0f);
                    playerCore.rb.MoveRotation(GetRot());
                    playerCore.combat.Hitstop(0.1f, true, Enemy.GetEnemy(current.gameObject));
                    EndDash();
                }
            }
            else
            {
                playerCore.velocity = Vector3.Lerp(playerCore.velocity, Vector3.zero, Time.deltaTime * 4f);
                playerCore.rb.MoveRotation(Quaternion.Slerp(playerCore.rb.rotation, GetRot(), Time.deltaTime * 4f));
            }
            
            // TODO: check if dot product between current normalized velocity and desired input direction is less than a value below 0 (opposite input direction, start skidding)?:w
        }

        private void EndDash()
        {
            dashingVFX.Stop();
            dashTrailVFX.emitting = false;
            playerCore.ChangeState(playerCore.airborne);
            playerCore.animations.playerAnimator.CrossFade("Airborne", 0.3f);
        }

        private Quaternion GetRot()
        {
            Quaternion newRot = Quaternion.identity;
            Vector3 dir = (target.transform.position - playerCore.transform.position).normalized;
            if (fromGrounded)
            {
                newRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, playerCore.transform.up), playerCore.transform.up);
            }
            else
            {
                newRot = Quaternion.LookRotation(dir) *
                                    Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, dir, 0.25f));
            }
            
            return newRot;
        }

        public override bool Enter(State from)
        {
            
            if (!playerCore.combat.currentEnemy) return false;
            
            // target
            target = playerCore.combat.currentEnemy;
            if (target == null) return false;

            // Check grounded
            fromGrounded = from.id == StateID.Grounded;

            // Raycast check
            Physics.Raycast(playerCore.transform.position, target.transform.position - playerCore.transform.position, out var h);
            if (h.collider.gameObject != target.gameObject) return false;
                    
            // Dash vars
            normalizedVelocity = (target.transform.position - playerCore.transform.position).normalized;
            dashTimer = -playerCore.stats.dashStartDelay;

            // VFX & animation
            playerCore.animations.playerAnimator.Play("DashBegin", 0, 0f);
            dashStartVFX.transform.rotation = GetRot();
            dashTrailVFX.emitting = true;
            dashStartVFX.Play();
            
            // end...
            return true;
        }

        public override bool Exit(State to)
        {
            dashingVFX.Stop();
            dashTrailVFX.emitting = false;
            return true;
        }
    }
}