using UnityEngine;
using System;
using System.Collections.Generic;
using MEC;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.VFX;

namespace frost
{
    [Serializable]
    public class PlayerCombat
    {
        private PlayerCore pc;
        public float hp { private get; set; }
        public float maxHp { private get; set; }
        [field: SerializeField] public float energy { private get; set; }
        public float invulnerable { private get; set; }
        
        [field: SerializeField] public CombatStats stats { private set; get; }
        private float dodgeCooldown = 0f;

        // Basic enemy
        public AttackState currentAttackState { private set; get; } = AttackState.None;
        [field: NonSerialized] public Attack currentAttack { private set; get; }
        [field: NonSerialized] public float hitStop { private set; get; }
        [field: NonSerialized] public float killStop { private set; get; }
        private float attackTimer;
        public Vector3 attackDir { private set; get; }
        private Vector3 hitVel;
        private bool hitThisFrame;
        
        // Enemy detection
        public bool enemyFocused { private set; get; }
        public Enemy currentEnemy { private set; get; }
        public Enemy lastKilled { private set; get; }
        private CoroutineHandle attackCoroutineHandle;

        [SerializeField] private BoxCollider[] hitBoxes = new BoxCollider[10];
        [NonSerialized] public Collider[] detectedEnemies = new Collider[10];
        [NonSerialized] public int detectedEnemiesCount;
        [SerializeField] private LayerMask enemyMask;
        
        // UI
        [SerializeField] private Image hpBar;
        [SerializeField] private Image hpBarDropIndicator;
        [SerializeField] private Image[] energyBars;
        private float dropTimer;
        
        [SerializeField] private Image hpBarEnemy;
        [SerializeField] private Image hpBarDropIndicatorEnemy;
        [SerializeField] private Image enemyReticle;
        [SerializeField] private TextMeshProUGUI enemyHpText;
        private float dropTimerEnemy, prevEnemyHp;
        
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] public TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Animator endscreenAnimator;
        [SerializeField] private int score;
        
        // VFX & hitstop
        [SerializeField] private VisualEffect slash;
        [SerializeField] private VisualEffect hitImpact;
        [SerializeField] private Transform slashParent;
        private bool hitStopEnd, hitFly;

        public bool dead = false;

        public void Init(PlayerCore pc)
        {
            this.pc = pc;
            hp = maxHp = stats.maxHP;
        }

        public void Hitstop(float duration, bool attacking, Enemy enemy = null)
        {
            hitStop = duration;
            pc.playerCam.cameraShake.ShakePresets.ShortShake3D(0.4f);
            hitVel = pc.velocity;
            pc.velocity = Vector3.zero;
            pc.animations.playerAnimator.speed = 0.01f;

            if (attacking)
            {
                slash.playRate = 0.004f;
                hitImpact.playRate = 0.004f;
                slash.SetFloat("spd", 0.05f);
                if (enemy)
                {
                    enemy.SetStun(duration);
                    
                    // spawn hitimpact
                    hitImpact.transform.position = enemy.hitVFXCollider.ClosestPoint(pc.transform.position + attackDir * 0.5f);
                    hitImpact.Play();
                }
            }
        }

        private IEnumerator<float> _HitPause(float time)
        {
            Timing.PauseCoroutines(attackCoroutineHandle);
            yield return Timing.WaitForSeconds(time);
            Timing.ResumeCoroutines(attackCoroutineHandle);
        }

        public void Attack(int type)
        {
            if (currentAttackState != AttackState.None) return;
            currentAttack = stats.attacks[type];
            attackCoroutineHandle = Timing.RunCoroutine(_Attack());
        }

        public void AddEnergy(int amt)
        {
            energy = Mathf.Clamp(energy + amt, 0, 5);
        }
        
        public void Dash()
        {
            if (energy <= 0) return;
            pc.ChangeState(pc.dash);
        }
        
        public void Dodge()
        {
            if (dodgeCooldown > 0f) return;
            if (pc.ChangeState(pc.dodge)) dodgeCooldown = pc.stats.dodgeCooldown;
        }

        private int currentLightAttack = 0;
        private IEnumerator<float> _Attack()
        {
            // Default
            attackDir = pc.directionalInput ? pc.inputDirection : pc.transform.forward;
                
            // go through unique phases
            attackTimer = 0f;
            currentAttackState = AttackState.Windup;
            if (currentAttack.id is (int)PlayerAttack.LightA or (int)PlayerAttack.HeavyA)
            {
                if (currentAttack.id is (int)PlayerAttack.LightA)
                {
                    currentLightAttack++;
                    if (currentLightAttack > 3) currentLightAttack = 1;
                    
                    //pc.animations.playAnimation("Attack");
                    pc.animations.playerAnimator.Play("Attack " + currentLightAttack, 0, 0f);
                }
                else
                {
                    pc.animations.playerAnimator.Play("HeavyAttack", 0, 0f);
                }
            }
            else if (currentAttack.id is (int)PlayerAttack.DashHit)
            {
                attackDir = pc.transform.forward;
            }
                
            yield return Timing.WaitForSeconds(currentAttack.windup);
            currentAttackState = AttackState.Active;

            SendHits();
            
            // windup w/ animations & vfx
            if (currentAttack.id is (int)PlayerAttack.LightA or (int)PlayerAttack.HeavyA)
            {
                AudioManager.instance.PlaySound("Woosh");
                switch (currentLightAttack)
                { 
                    case 1:
                        slashParent.rotation = Quaternion.LookRotation(attackDir) * Quaternion.Euler(-14f, -44f, 165f);
                        break;
                    case 2:
                        slashParent.rotation = Quaternion.LookRotation(attackDir) * Quaternion.Euler(0f, 0f, 90f);
                        break;
                    case 3:
                        slashParent.rotation = Quaternion.LookRotation(attackDir) * Quaternion.Euler(0f, 180f, 55f);
                        break;
                    default:
                        break;
                }
                slashParent.position = pc.transform.position + attackDir;
                slash.Play();
            }
            else if (currentAttack.id == (int)PlayerAttack.DashHit)
            {
                AudioManager.instance.PlaySound("HighImpact");
                slashParent.rotation = Quaternion.LookRotation(attackDir) * Quaternion.Euler(164f, -164f, -243f);
                slashParent.position = pc.transform.position + attackDir * 2f;
                slash.Play();
            }
            
            yield return Timing.WaitForSeconds(currentAttack.hitTime);
            currentAttackState = AttackState.Cooldown;
            yield return Timing.WaitForSeconds(currentAttack.cooldown);
            currentAttackState = AttackState.None;
        }
        
        public void Update()
        {
            if (dead)
            {
                Time.timeScale = Mathf.Lerp(Time.timeScale, 0f, 0.01f);
            }
            
            dodgeCooldown -= Time.deltaTime;
            invulnerable -= Time.deltaTime;
            if (currentAttackState != AttackState.None && hitStop <= 0f)
            {
                attackTimer += Time.deltaTime;
            }
            
            killStop -= Time.deltaTime;
            if (hitStop > 0)
            {
                hitStop -= Time.deltaTime;
                if (hitStop <= 0)
                {
                    pc.velocity = hitVel;
                    pc.animations.playerAnimator.speed = 1f;
                    slash.playRate = 1f;
                    hitImpact.playRate = 1f;
                    slash.SetFloat("spd", 1f);
                    hitStopEnd = true;
                }
            }
            
            // Update HUD
            
            // Player HP
            dropTimer -= Time.deltaTime;
            float hPercent = hp / maxHp;
            hpBar.rectTransform.pivot = new Vector2(Mathf.Lerp(hpBar.rectTransform.pivot.x, 1f - hPercent, 20f * Time.deltaTime), 1f);
            if (dropTimer < 0)
            {
                hpBarDropIndicator.rectTransform.pivot = new Vector2(Mathf.MoveTowards(hpBarDropIndicator.rectTransform.pivot.x, 1f - hPercent, Time.deltaTime * 0.7f), 1f);
            }
            
            // Enemy HP
            if (enemyFocused && currentEnemy)
            {
                hPercent = currentEnemy.hp / currentEnemy.maxHp;
                hpBarEnemy.rectTransform.anchorMax = new Vector2(Mathf.Lerp(hpBarEnemy.rectTransform.anchorMax.x, hPercent, 20f * Time.deltaTime), 1f);

                dropTimerEnemy -= Time.deltaTime;
                float curHp = currentEnemy.hp;
                if (curHp != prevEnemyHp)
                {
                    dropTimerEnemy = 0.5f;
                    enemyHpText.text = Mathf.CeilToInt(curHp) + "/" + Mathf.CeilToInt(currentEnemy.maxHp);
                }
                prevEnemyHp = curHp;

                if (dropTimerEnemy < 0)
                {
                    hpBarDropIndicatorEnemy.rectTransform.anchorMax = new Vector2(Mathf.MoveTowards(hpBarDropIndicatorEnemy.rectTransform.anchorMax.x, hPercent, Time.deltaTime * 0.7f), 1f);
                }
                
                UpdateReticleBounds();
                enemyReticle.pixelsPerUnitMultiplier = Mathf.Lerp(enemyReticle.pixelsPerUnitMultiplier, 12f, Time.deltaTime * 15f);
                enemyReticle.color = Color.Lerp(enemyReticle.color, Color.white, Time.deltaTime * 15f);
            }
            else
            {
                Vector2 min = new Vector2(-10, -10);
                Vector2 max = new Vector2(Screen.width + 10, Screen.height + 10) / pc.canvas.scaleFactor; 

                enemyReticle.rectTransform.offsetMin = Vector2.Lerp(enemyReticle.rectTransform.offsetMin, min, Time.deltaTime * 10f);
                enemyReticle.rectTransform.offsetMax = Vector2.Lerp(enemyReticle.rectTransform.offsetMax, max - pc.UIContainer.rect.size, Time.deltaTime * 10f);
                
                hpBarEnemy.rectTransform.pivot = new Vector2(1f, 1f);
                hpBarDropIndicatorEnemy.rectTransform.pivot = new Vector2(1f, 1f);
                enemyReticle.pixelsPerUnitMultiplier = Mathf.Lerp(enemyReticle.pixelsPerUnitMultiplier, 7f, Time.deltaTime * 15f);
                enemyReticle.color = Color.Lerp(enemyReticle.color, new Color(1, 1, 1, 0f), Time.deltaTime * 15f);
            }
            
            // energy bars
            for (int i = 0; i < energyBars.Length; i++)
            {
                float opacity = energy > i ? 1f : 0f;
                energyBars[i].color = new Color(energyBars[i].color.r, energyBars[i].color.g, energyBars[i].color.b, Mathf.Lerp(energyBars[i].color.a, opacity, Time.deltaTime * 10f));
            }
        }

        private void UpdateReticleBounds()
        {
            Vector3 bSize = currentEnemy.outlineBoundsSize;
            Vector3 enemyPos = currentEnemy.transform.position;
            Quaternion camRot = pc.playerCam.cam.transform.rotation;
            Vector2 max = pc.playerCam.cam.WorldToScreenPoint(enemyPos + camRot * bSize) / pc.canvas.scaleFactor;
            Vector2 min = pc.playerCam.cam.WorldToScreenPoint(enemyPos - camRot * bSize) / pc.canvas.scaleFactor;

            enemyReticle.rectTransform.offsetMin = min;
            enemyReticle.rectTransform.offsetMax = max - pc.UIContainer.rect.size;
        }
        
        public void FixedUpdate()
        {
            // Scout cast
            detectedEnemiesCount = Physics.OverlapSphereNonAlloc(
                pc.transform.position, pc.stats.enemyDetectionDistance,
                detectedEnemies, enemyMask);

            if (detectedEnemiesCount == 0)
            {
                currentEnemy = null;
                enemyFocused = false;
            }
            else if (!enemyFocused)
            {
                if (detectedEnemiesCount > 0)
                {
                    float sqrDist = float.MaxValue;
                    Collider current = null;
                    
                    for (int i = 0; i < detectedEnemiesCount; i++)
                    {
                        if (Enemy.GetEnemy(detectedEnemies[i].gameObject).dead) continue;
                        float curDist = Vector3.SqrMagnitude(detectedEnemies[i].transform.position - pc.transform.position);
                        if (curDist < sqrDist)
                        {
                            current = detectedEnemies[i];
                            sqrDist = curDist;
                        }
                    }

                    // only focus if within distance
                    if (sqrDist < pc.stats.enemyEngageDistance * pc.stats.enemyEngageDistance)
                    {
                        currentEnemy = Enemy.GetEnemy(current.gameObject);
                        enemyFocused = true;
                    }
                }
            }
            
            if (currentAttackState != AttackState.None)
            {
                pc.velocity = attackDir * currentAttack.velocity.Evaluate(attackTimer);
            }

            if (hitStopEnd)
            {
                hitStopEnd = false;

                if (hitFly)
                {
                    pc.ChangeState(pc.airborne);
                    hitFly = false;
                    return;
                }
                else
                {
                    pc.velocity = hitVel;
                }
            }
            else if (hitStop > 0)
            {
                pc.velocity = Vector3.zero;
            }
            

            //hitboxParent.forward = attackDir;
            //hitboxParent.position = pc.transform.position;
            hitThisFrame = false;
            SendHits();

            // set enemy focused based on detection
            // enemyFocused = currentEnemy != null;
        }

        public void SendHits(bool parrying = false)
        {
            // Register continuously
            if (currentAttackState == AttackState.Active || parrying)
            {
                if (hitThisFrame) return;
                hitThisFrame = true;

                int currentAttackInt = currentAttack.id;
                int overlaps = Physics.OverlapBoxNonAlloc(
                   hitBoxes[currentAttackInt].transform.position, 
                   hitBoxes[currentAttackInt].size,
                   detectedEnemies, 
                   hitBoxes[currentAttackInt].transform.rotation,
                   enemyMask);

                Hit hit = new Hit(pc, currentAttack.damage, currentAttack.knockback);
                
                for (int i = 0; i < overlaps && i < detectedEnemies.Length; i++)
                {
                    Enemy current = Enemy.GetEnemy(detectedEnemies[i].gameObject);
                    if (!currentEnemy)
                    {
                        currentEnemy = current;
                        enemyFocused = true;
                    }
                    
                    bool hitReg = current.Damage(hit, attackDir);
                    if (hitReg)
                    {
                        float time = currentAttack.hitstop;
                        if (current.dead)
                        {
                            time = current.stats.deathHitstop;
                            lastKilled = current;
                            enemyFocused = false;
                            killStop = time * 2.5f;
                        }
                        Hitstop(time, true, current);
                    }
                }
            }
        }
        
        public bool Damage(Hit hit, Vector3 direction)
        {
            if (invulnerable > 0) return false;

            if (pc.currentState.id is StateID.Airborne)
            {
                pc.airborne.airHit = true;
            }
            
            if (currentAttackState != AttackState.None && currentAttack.id == (int)PlayerAttack.LightA &&
                attackTimer > stats.parryRange.x && attackTimer < stats.parryRange.y && Vector3.Dot(attackDir, direction) < 0)
            {
                Enemy other = hit.source as Enemy;
                if (other.Parry())
                {
                    // Parry
                    Hitstop(0.3f, true, other);
                    pc.playerInput.LockInput(0.15f);
                    invulnerable = 0.3f;
                    hitThisFrame = false;
                    
                    SendHits(true);
                    return false;
                }
            }
            
            // add knockback
            if (hit.knockback.y > 2)
            {
                pc.airborne.SetHitfly(Quaternion.LookRotation(direction) * hit.knockback, 10f);
                invulnerable = 1f;
                hitFly = true;
            }
            else
            {
                pc.velocity = Quaternion.LookRotation(direction) * hit.knockback;
                pc.animations.playAnimation("Hurt");
                pc.grounded.SetForward(-direction);
                invulnerable = 0.1f;
            }
            
            hp -= hit.damage;
            dropTimer = 0.5f;

            if (hp <= 0f)
            {
                Die();
            }
            
            return true;
        }

        public void Die()
        {
            Hitstop(stats.deathHitstop, false);
            dead = true;

            int savedScore = score;
            int highest = PlayerPrefs.GetInt("Highscore");
            if (savedScore > highest)
            {
                // high score
                highScoreText.gameObject.SetActive(true);
                PlayerPrefs.SetInt("Highscore", highest);
                PlayerPrefs.Save();
            }
            
            finalScoreText.text = $"{savedScore:D5}";
            endscreenAnimator.Play("End");
            Timing.RunCoroutine(DieTimer());
        }
        
        private IEnumerator<float> DieTimer()
        {
            yield return Timing.WaitForSeconds(10f);
            SceneManager.LoadScene(0);
        }

        public void OnCollisionEnter(Collision collision)
        {
            
        }
        
        public void AddScore(int s, bool notify = true)
        {
            score += s;
            scoreText.text = $"{score:D6}";
            if (notify) pc.Notify("+" + s);
        }
        
        public void UpdateRound()
        {
            int current = pc.currentIsland.currentRound;
            int total = pc.currentIsland.rounds;
            roundText.text = "Round " + current + "/" + total;
            if (current == total)
            {
                pc.Notify("Cleared Island! + 1000");
                AddScore(1000, false);
            }
            else
            {
                pc.Notify("Cleared Round!");
            }
        }
        
        public void Switch(bool right)
        {
            if (currentEnemy == null) return;
            
            Vector3 bestPos;
            Collider bestEnemy = null;
            
            Vector3 currentEnemyPos = pc.playerCam.cam.WorldToScreenPoint(currentEnemy.GetComponent<Collider>().bounds.center);

            if (right)
            {
                bestPos = new Vector3(float.MaxValue, 0f, 0f);
                
                for (int i = 0; i < detectedEnemiesCount; i++)
                {
                    Vector3 pos = pc.playerCam.cam.WorldToScreenPoint(detectedEnemies[i].bounds.center);
                    if (pos.x > currentEnemyPos.x && pos.x < bestPos.x)
                    {
                        bestEnemy = detectedEnemies[i];
                        bestPos = pos;
                    }
                }
            }
            else
            {
                bestPos = new Vector3(float.MinValue, 0f, 0f);
                
                for (int i = 0; i < detectedEnemiesCount; i++)
                {
                    Vector3 pos = pc.playerCam.cam.WorldToScreenPoint(detectedEnemies[i].bounds.center);
                    if (pos.x < currentEnemyPos.x && pos.x > bestPos.x)
                    {
                        bestEnemy = detectedEnemies[i];
                        bestPos = pos;
                    }
                }
            }
            
            if (bestEnemy != null) currentEnemy = Enemy.GetEnemy(bestEnemy.gameObject);
        }

        public void ClearEnemy()
        {
            enemyFocused = false;
        }
    }
}
