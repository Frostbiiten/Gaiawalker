using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace frost
{
    public class Enemy : MonoBehaviour
    {
        private static Dictionary<GameObject, Enemy> _enemies = new Dictionary<GameObject, Enemy>();
        public static Enemy GetEnemy(GameObject g)
        {
            Enemy val;
            return _enemies.TryGetValue(g, out val) ? val : _enemies[g] = g.GetComponent<Enemy>();
        }
        
        public float maxHp { protected set; get; }
        public float hp { protected set; get; }
        public float invulnerable { protected set; get; } = 0;
        public bool dead { protected set; get; }
        public int deathState { protected set; get; }
        [field:SerializeField] public float resistance { protected set; get; }
        
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected Collider mainCollider;
        [SerializeField] protected int score;
        private float deathTime;

        protected Vector3 kb, stunPos;
        protected float stunTimer, attackTimer, hitStop;
        protected bool hitThisFrame;
        
        // Attack
        [field: SerializeField] public CombatStats stats { get; private set; }
        [SerializeField] private VisualEffect smoke;
        
        public enum EnemyAttack
        {
            Light = 0,
            Heavy = 1
        }
        protected AttackState currentAttackState = AttackState.None;
        protected Attack currentAttack;
        protected bool currentAttackHit;
        protected CoroutineHandle attackCoroutineHandle;
            
        // Attack Query
        protected Collider[] queryColliders = new Collider[4];
        [SerializeField] protected BoxCollider[] hitBoxes;
        [SerializeField] protected LayerMask playerMask;
        
        //visual
        [field: SerializeField] public Vector3 outlineBoundsSize { private set; get; }
        [field: SerializeField] public Collider hitVFXCollider { private set; get; }
        [field: SerializeField] public VisualEffect hitImpact { private set; get; }
        [field: SerializeField] public TrailRenderer trail { private set; get; }

        private void Awake()
        {
            _Awake();
        }

        protected virtual void _Awake()
        {
            
        }

        // Damage
        public bool Damage(Hit hit, Vector3 direction)
        {
            kb += Quaternion.LookRotation(direction) * hit.knockback;
            if (invulnerable > 0) return false;
            invulnerable = 0.1f;
            hp -= (1f - resistance) * hit.damage;
            if (hp <= 0)
            {
                if (!dead)
                {
                    PlayerCore pc = PlayerCore.mainPlayerCore;
                    pc.combat.AddEnergy(1);
                    pc.combat.AddScore(score);
                }
                Die(direction);
                hp = 0;
            }
            
            return _Damage(hit, direction);
        }
        protected virtual bool _Damage(Hit hit, Vector3 direction)
        {
            return true;
        }

        private void Die(Vector3 dir)
        {
            dead = true;
            deathState = 0;
            kb = (dir + Vector3.up * 0.5f).normalized * 50f;
            AudioManager.instance.PlaySound("Collect");
            SetStun(stats.deathHitstop);
            _Die();
        }
        protected virtual void _Die()
        {
            
        }

        protected void SendHits()
        {
            // Register continuously
            if (currentAttackState == AttackState.Active)
            {
                if (hitThisFrame || (currentAttackHit && !currentAttack.multiHit)) return;
                hitThisFrame = true;

                int currentAttackInt = (int)currentAttack.id;
                int overlaps = Physics.OverlapBoxNonAlloc(
                   hitBoxes[currentAttackInt].transform.position, 
                   hitBoxes[currentAttackInt].size,
                   queryColliders, 
                   hitBoxes[currentAttackInt].transform.rotation,
                   playerMask);

                Hit hit = new Hit(this, currentAttack.damage, currentAttack.knockback);
                
                for (int i = 0; i < overlaps && i < queryColliders.Length; i++)
                {
                    bool hitReg = PlayerCore.mainPlayerCore.combat.Damage(hit, transform.forward);
                    if (hitReg)
                    {
                        hitStop = currentAttack.hitstop;
                        PlayerCore.mainPlayerCore.combat.Hitstop(hitStop, false);
                        Timing.RunCoroutine(_Hitstun(hitStop));
                        hitImpact.transform.position = PlayerCore.mainPlayerCore.transform.position+ transform.forward * 0.5f;
                        hitImpact.Play();
                        currentAttackHit = true;
                    }
                }
            }
        }
        protected IEnumerator<float> _Hitstun(float time)
        {
            Timing.PauseCoroutines(attackCoroutineHandle);
            yield return Timing.WaitForSeconds(time);
            Timing.ResumeCoroutines(attackCoroutineHandle);
        }
        
        public void SetStun(float stun)
        {
            rb.isKinematic = stun > 0f;
            stunPos = transform.position;
            stunTimer = stun;
        }
        
        public bool Parry()
        {
            if (!currentAttack.parryable) return false;
            Timing.KillCoroutines(attackCoroutineHandle);
            CompleteAttack();
            SetStun(0.3f);
            return true;
        }
        
        private IEnumerator<float> _Attack()
        {
            Vector3 attackDir = transform.forward;
            
            // go through unique phases
            attackTimer = 0f;
            currentAttackState = AttackState.Windup;
            yield return Timing.WaitForSeconds(currentAttack.windup);
            currentAttackState = AttackState.Active;
            SendHits();
            yield return Timing.WaitForSeconds(currentAttack.hitTime);
            currentAttackState = AttackState.Cooldown;
            yield return Timing.WaitForSeconds(currentAttack.cooldown);
            currentAttackState = AttackState.None;
            CompleteAttack();
        }
        protected void Attack(int type)
        {
            if (currentAttackState != AttackState.None && currentAttackState != AttackState.Charging) return;
            currentAttack = stats.attacks[type];
            currentAttackHit = false;
            attackCoroutineHandle = Timing.RunCoroutine(_Attack());
        }
        
        // End attack
        protected virtual void CompleteAttack()
        {
            currentAttackState = AttackState.None;
        }
        
        // Start
        public void Start()
        {
            hp = maxHp = stats.maxHP;
            _Start();
        }
        protected virtual void _Start() {}
        
        // Update
        public void Update()
        {
            if (dead)
            {
                deathTime += Time.deltaTime;
            }
            
            invulnerable -= Time.deltaTime;
            if (currentAttackState != AttackState.None && currentAttackState != AttackState.Charging && hitStop <= 0f)
            {
                attackTimer += Time.deltaTime;
            }
            
            stunTimer -= Time.deltaTime;
            if (stunTimer < 0)
            {
                if (!dead)
                {
                    rb.isKinematic = false;
                }
                else if (deathState == 0)
                {
                    rb.isKinematic = false;
                    deathState = 1;
                    rb.linearVelocity = kb;
                    trail.emitting = true;
                }
            }
            
            if (hitStop > 0)
            {
                hitStop -= Time.deltaTime;
                if (hitStop <= 0)
                {
                    hitImpact.playRate = 1f;
                }
            }
            _Update();
        }
        protected virtual void _Update(){}
        
        // FixedUpdate
        public void FixedUpdate()
        {
            mainCollider.enabled = (!dead) || (dead && deathTime > 0.5f);
            if (deathState == 1)
            {
                rb.linearVelocity += Vector3.down * 0.5f;
                rb.MoveRotation(rb.rotation * Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, rb.linearVelocity.normalized, 0.1f)));
            }
            _FixedUpdate();

            hitThisFrame = false;
            SendHits();
        }
        protected virtual void _FixedUpdate(){}

        private void OnCollisionEnter(Collision collision)
        {
            CollisionCheck(collision);
        }
        
        private void OnCollisionStay(Collision collision)
        {
            CollisionCheck(collision);
        }

        private void CollisionCheck(Collision collision)
        {
            if (deathState == 1 && deathTime > 0.5f)
            {
                if (stunTimer < -0.3f && collision.gameObject.CompareTag("Floor"))
                {
                    rb.isKinematic = true;
                    rb.MovePosition(Vector3.Lerp(transform.position, collision.contacts[0].point, 0.75f));
                    deathState = 2;
                    smoke.transform.rotation = Quaternion.identity;
                    if (smoke != null) smoke.Play();
                    PlayerCore.mainPlayerCore.playerCam.cameraShake.ShakePresets.Explosion3D(0.9f, 0.75f);
                }
            }
        }
    }
}