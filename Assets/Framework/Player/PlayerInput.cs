using System;
using UnityEngine;
using UnityEngine.InputSystem;


namespace frost
{
    
    [Serializable]
    public class PlayerInput
    {
        private PlayerCore playerCore;
        
        [Header("Asset")]
        [SerializeField] InputActionAsset playerInputAsset;
        
        // Lock
        private float lockTime = 0f;

        [Header("Directional")]
        private InputAction directionalInputAction;
        public Vector2 directionalInput { get; private set; }
        public bool directionalInputTriggered { get; private set; }
        public float deadZone = 0.1f;
        
        [Header("Buttons")]
        private InputAction dashInputAction;
        public bool dash { get; private set; }
        
        private InputAction dodgeInputAction;
        public bool dodge { get; private set; }
        
        private InputAction jumpInputAction;
        public bool jump { get; private set; }
        
        private InputAction enemySwitchAction;
        
        // Combat
        private InputAction lightInputAction;
        public bool light { get; private set; }
        
        private InputAction heavyInputAction;
        public bool heavy{ get; private set; }
        
        private InputAction walkInputAction;
        public bool walk{ get; private set; }

        private bool CanAttack()
        {
            var state = playerCore.currentState;
            bool attackLock = true;
            if (state != null)
            {
                attackLock = playerCore.combat.dead || state is { id: StateID.Dash or StateID.Travel}
                             || (state.id == StateID.Airborne && (((PlayerAirborne)state).hitFly > 0f ||
                                                                  ((PlayerAirborne)state).slideContact))
                             || playerCore.combat.hitStop > 0f
                    ;
            }
            return !attackLock;
        }
        
        public void Init(PlayerCore pc)
        {
            playerCore = pc;
            
            // Movement
            directionalInputAction = playerInputAsset.FindAction("Directional");
            walkInputAction = playerInputAsset.FindAction("Walk");
            jumpInputAction = playerInputAsset.FindAction("Jump");
            jumpInputAction.performed += _ =>
            {
                if (playerCore.combat.dead) return;
                if (playerCore.currentState.id is StateID.Travel)
                {
                    playerCore.travel.Launch();
                }
                else
                {
                    playerCore.grounded.TriggerJump();
                }
            };

            // Combat
            lightInputAction = playerInputAsset.FindAction("Light");
            lightInputAction.performed += _ =>
            {
                if (CanAttack()) playerCore.combat.Attack((int)PlayerAttack.LightA);
            };
            
            heavyInputAction = playerInputAsset.FindAction("Heavy");
            heavyInputAction.performed += _ =>
            {
                if (CanAttack()) playerCore.combat.Attack((int)PlayerAttack.HeavyA);
            };
            
            dashInputAction = playerInputAsset.FindAction("Dash");
            dashInputAction.performed += _ =>
            {
                if (CanAttack()) playerCore.combat.Dash();
            };
            
            dodgeInputAction = playerInputAsset.FindAction("Dodge");
            dodgeInputAction.performed += _ =>
            {
                if (CanAttack()) playerCore.combat.Dodge();
            };
            
            enemySwitchAction = playerInputAsset.FindAction("EnemySwitch");
            enemySwitchAction.performed += _ =>
            {
                if (playerCore.combat.dead) return;
                    
                if (playerCore.currentState.id is StateID.Travel)
                {
                    playerCore.travel.SwitchIsland(enemySwitchAction.ReadValue<float>() > 0f);
                }
                else
                {
                    playerCore.combat.Switch(enemySwitchAction.ReadValue<float>() > 0f);
                }
            };
        }

        public void LockInput(float time)
        {
            
        }

        void UpdateInput()
        {
            if (lockTime <= 0f && !playerCore.combat.dead)
            {
                // Movement
                directionalInput = directionalInputAction.ReadValue<Vector2>();
                directionalInputTriggered = directionalInputAction.inProgress;
                
                walk = walkInputAction.inProgress;
                jump = jumpInputAction.inProgress;
                
                // Combat
                light = lightInputAction.inProgress;
                heavy = heavyInputAction.inProgress;
                dash = dashInputAction.inProgress;
            }
            else
            {
                walk = jump = light = heavy = dash = false;
                directionalInputTriggered = false;
                directionalInput = Vector2.zero;
            }
        }

        public void Update()
        {
            lockTime -= Time.deltaTime;
            UpdateInput();
        }

        public void lockInput(float time, bool additive = false)
        {
            if (!additive) lockTime = 0f;
            lockTime += time;
        }
    }
}
