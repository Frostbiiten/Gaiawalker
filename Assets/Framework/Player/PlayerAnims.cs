using System;
using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
using UnityEngine;

namespace frost
{
    [Serializable] public class PlayerAnims
    {
        [field: SerializeField] public Animator playerAnimator { private set; get; }
        private PlayerCore playerCore;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform playerSkin;
        [SerializeField] private JiggleSettings jiggle;
        
        public void setPlayerCore(PlayerCore pc)
        {
            playerCore = pc;
        }
        
        public void playAnimation(string name)
        {
            playerAnimator.Play(name);
        }

        // update animator variables
        public void Update()
        {
        }
        
        public void LateUpdate()
        {
            float blend = playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hitfar") ? 0f : 1f;
            jiggle.SetParameter(JiggleSettingsBase.JiggleSettingParameter.Blend, blend);
            playerAnimator.SetBool("Grounded", playerCore.currentState.id == StateID.Grounded);
            playerAnimator.SetBool("Aiming", playerCore.currentState.id == StateID.Grounded);
            playerAnimator.SetBool("GroundDetected", playerCore.floorDetected);
            
            if (playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Standup") || (playerCore.currentState.id == StateID.Airborne
                && playerCore.airborne.hitFly > 0f))
            {
                Vector3 vel = playerCore.normalizedVelocity;
                vel.y *= 2f;
                vel.Normalize();
                playerSkin.rotation = Quaternion.LookRotation(vel);
            }
            else
            {
                playerSkin.localRotation = Quaternion.Slerp(playerSkin.localRotation, Quaternion.identity, Time.deltaTime * 5f);
            }
            
            if (playerCore.combat.enemyFocused && !playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hurt"))
            {
                Vector3 dir = (headBone.position - playerCore.combat.currentEnemy.transform.position).normalized;
                dir.y = Mathf.Clamp(dir.y, -0.2f, 0.2f);
                Quaternion rot = Quaternion.LookRotation(dir, playerCore.transform.up);
                float delta = Quaternion.Angle(headBone.rotation, rot);
                float range = playerCore.currentState.id is StateID.Airborne ? 205f : 180f;
                if (delta < range)
                {
                    headBone.rotation = Quaternion.Slerp(headBone.rotation, rot, Mathf.SmoothStep(0f, 1.2f, 1 - delta / range));
                }
            }
        }
    }
}
