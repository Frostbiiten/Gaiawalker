using DitzelGames.FastIK;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace frost
{
    public class DefaultEnemy : Enemy
    {
        // Behaviour
        [SerializeField] private float playerRange, fov, attackInterval, strafeDistance, runSpeed, strafeSpeed, attackDistance;
        [SerializeField] private Vector3 gravity;
        private float strafeTimer, strafeDir = -1;
        private Vector3 movementVel, lerpVel;
        private bool playerDetected;
        
        // Visual
        [SerializeField] private float shakeFreq, shakeAmp;
        [SerializeField] private VisualEffect attackLightWarning;
        [SerializeField] private VisualEffect attackHeavyWarning;

        [SerializeField] private Transform[] ikPoints;
        [SerializeField] private FastIKFabric[] ikSolvers;
        private float[] ikInterpTimes;
        [SerializeField] private float stepTime;
        [SerializeField] private float stepHeight;
        [SerializeField] private float stepDist;
        private Vector3[] targetPoints;
        private Vector3[] oldTargetPoints;
        [SerializeField] private Vector3 ikOffset;
        [SerializeField] private Vector3 ikCenterOffset;
        [SerializeField] private float ikLookahead;
        [SerializeField] private LayerMask floorMask;
        [SerializeField] private GameObject skin;
        [SerializeField] private float tilt;

        protected override void _Awake()
        {
            ikInterpTimes = new float[ikPoints.Length];
            targetPoints = new Vector3[ikPoints.Length];
            oldTargetPoints = new Vector3[ikPoints.Length];
        }

        protected override bool _Damage(Hit hit, Vector3 direction)
        {
            if (!playerDetected)
            {
                playerDetected = true;
                ResetStrafeVars();
            }

            Vector3 u = Vector3.Slerp(Vector3.up, direction.normalized, 0.6f);
            skin.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(-direction.normalized, u), u);
            
            return true;
        }

        protected override void _Die()
        {
            for (int i = 0; i < ikSolvers.Length; i++)
            {
                ikSolvers[i].enabled = false;
            }
        }

        private void ResetStrafeVars()
        {
            strafeTimer = attackInterval + attackInterval * Random.Range(0, 0.75f);
            strafeDir = Mathf.Sign(Random.Range(-1, 1));
        }
        protected override void CompleteAttack()
        {
            currentAttackState = AttackState.None;
            ResetStrafeVars();
        }

        private Vector3 oldVelo;
        private Vector3 smoothVelo;
        private Vector3 smoothAcc;

        protected override void _Update()
        {
            if (dead) return;
            
            for (int i = 0; i < ikPoints.Length; i++)
            {
                targetPoints[i] = transform.position + transform.rotation * ikCenterOffset + ikLookahead * rb.linearVelocity.normalized;
            }

            var tr = transform.right;
            var tf = transform.forward;
            targetPoints[0] += -tr * ikOffset.x + tf * ikOffset.z;
            targetPoints[1] += tr * ikOffset.x + tf * ikOffset.z;
            targetPoints[2] += tr * ikOffset.x + -tf * ikOffset.z;
            targetPoints[3] += -tr * ikOffset.x + -tf * ikOffset.z;

            for (int i = 0; i < ikPoints.Length; i++)
            {
                RaycastHit r;
                bool hit = Physics.Raycast(targetPoints[i], Vector3.down, out r, ikOffset.y, floorMask);
                ikSolvers[i].enabled = hit;
                if (hit) targetPoints[i] = r.point;
            }

            for (int i = 0; i < ikPoints.Length; i++)
            {
                if (ikInterpTimes[i] <= 0f && Vector3.Distance(targetPoints[i], ikPoints[i].position) > stepDist)
                {
                    float t = stepTime * 0.5f;
                    if (i % 2 == 0)
                    {
                        if (ikInterpTimes[0] > t || ikInterpTimes[2] > t) continue;
                    }
                    else
                    {
                        if (ikInterpTimes[1] > t || ikInterpTimes[3] > t) continue;
                    }
                    ikInterpTimes[i] = stepTime;
                    oldTargetPoints[i] = ikPoints[i].position;
                }
            }

            for (int i = 0; i < ikPoints.Length; i++)
            {
                if (ikInterpTimes[i] > 0f)
                {
                    ikInterpTimes[i] -= Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, (stepTime - ikInterpTimes[i]) / stepTime);
                    ikPoints[i].position = Vector3.Lerp(oldTargetPoints[i], targetPoints[i], t);
                    ikPoints[i].position += Vector3.up * stepHeight * Mathf.Sin(Mathf.PI * t);
                }
            }

            smoothVelo = Vector3.Lerp(smoothVelo, rb.linearVelocity, 0.5f);
            smoothAcc = Vector3.Lerp(smoothAcc, (oldVelo - smoothVelo), Time.deltaTime * 8f);
            
            Vector3 u = (Vector3.up + smoothAcc * 0.7f * tilt).normalized;
            skin.transform.rotation = Quaternion.Slerp(skin.transform.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, u), u), Time.deltaTime * 10f);
            
            /*
            skin.transform.rotation = Quaternion.Slerp(skin.transform.rotation,
                transform.rotation * Quaternion.SlerpUnclamped(Quaternion.identity,
                    Quaternion.FromToRotation(Vector3.up, smoothAcc.normalized), tilt * smoothAcc.magnitude),
                Time.deltaTime * 5f);
            */
            
            oldVelo = smoothVelo;
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
                    Vector3 dir = PlayerCore.mainPlayerCore.transform.position - transform.position;
                    dir.y = 0f;
                    float distance = Vector3.Magnitude(dir);
                    dir /= distance;

                    if (playerDetected)
                    {
                        if (currentAttackState == AttackState.None)
                        {
                            float diff = strafeDistance - distance;
                            if (Mathf.Abs(diff) < 0.5f)
                            {
                                movementVel = Vector3.Cross(dir, Vector3.up) * (strafeSpeed * strafeDir);
                            }
                            else
                            {
                                movementVel = dir * (-Mathf.Sign(diff) * runSpeed);
                            }
                            
                            strafeTimer -= Time.fixedDeltaTime;
                            if (strafeTimer < 0)
                            {
                                // begin charge
                                currentAttackState = AttackState.Charging;
                            }
                                                           
                            lerpVel = Vector3.Lerp(lerpVel, movementVel, 0.15f);
                            rb.linearVelocity = lerpVel;
                                                           
                            kb.y = Mathf.Clamp(kb.y, 0f, 0.1f);
                            kb *= 0.5f;
                                                           
                            rb.linearVelocity += kb + gravity;
                            rb.linearVelocity *= 0.99f;
                                                           
                            rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f));
                        }
                        else
                        {
                            if (currentAttackState == AttackState.Charging)
                            {
                                // preparing to start attack (charging)
                                rb.linearVelocity = dir * runSpeed + gravity;
                                
                                if (distance < attackDistance)
                                {
                                    if (Random.Range(0f, 1f) > 0.75f)
                                    {
                                        Attack((int)EnemyAttack.Heavy);
                                        attackHeavyWarning.Play();
                                    }
                                    else
                                    {
                                        Attack((int)EnemyAttack.Light);
                                        attackLightWarning.Play();
                                    }
                                }
                                else
                                {
                                    rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 0.1f));
                                    if (distance > strafeDistance + 2f)
                                    {
                                        currentAttackState = AttackState.None;
                                        ResetStrafeVars();
                                    }
                                }
                            }
                            else
                            {
                                // in process of attack
                                rb.linearVelocity = transform.forward * currentAttack.velocity.Evaluate(attackTimer) + gravity;
                            }
                        }
                    }
                    else
                    {
                        rb.linearVelocity = gravity;
                        
                        // also check raycast
                        if (distance <= playerRange && Vector3.Dot(dir, transform.forward) > fov)
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
        }
    }
}
