using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace frost
{
    [Serializable]
    public class PlayerDodge : State
    {
        public override StateID id => StateID.Dodge;
        private float dodgeTime;
        private Vector3 dodgeDirection, prevVel;
        private State prevState;

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
            //
        }

        public override void FixedUpdate()
        {
            dodgeTime -= Time.fixedDeltaTime;
            if (dodgeTime > 0f)
            {
                playerCore.velocity = dodgeDirection * playerCore.stats.dodgeSpeed * playerCore.stats.dodgeCurve.Evaluate((playerCore.stats.dodgeTime - dodgeTime)/playerCore.stats.dodgeTime);
            }
            else
            {
                playerCore.velocity = prevVel;
                if (!playerCore.ChangeState(prevState)) playerCore.ChangeState(playerCore.airborne);
            }
        }

        public override bool Enter(State from)
        {
            // Requires directional input
            if (!playerCore.directionalInput) return false;

            if (playerCore.combat.enemyFocused && Vector3.Distance(playerCore.combat.currentEnemy.transform.position,
                    playerCore.transform.position) < 10f && playerCore.playerInput.directionalInput.x != 0)
            {
                Vector3 lookVec = Vector3.ProjectOnPlane(
                    (playerCore.combat.currentEnemy.transform.position - playerCore.transform.position),
                    playerCore.transform.up);
                dodgeDirection = Quaternion.LookRotation(lookVec) * (Vector3.right * playerCore.playerInput.directionalInput.x);
                
                //playerCore.rb.MoveRotation(Quaternion.LookRotation(lookVec, playerCore.transform.up));
            }
            else
            {
                dodgeDirection = playerCore.inputDirection;
            }
            
            
            prevVel = playerCore.velocity;
            prevState = from;
            
            // set timer
            dodgeTime = playerCore.stats.dodgeTime;
            
            // animations
            playerCore.animations.playerAnimator.Play("Dodge");

            return true;
        }
        public override bool Exit(State to)
        {
            return true;
        }
    }
}

