using System;
using TMPro;
using UnityEngine;

namespace frost
{
    [Serializable]
    public class PlayerTravel : State
    {
        public override StateID id => StateID.Travel;
        
        // either aiming or in motion...
        public bool aiming;
        
        // aim
        public Island aimedIsland;
        
        [SerializeField] private float travelTime = 3f;
        [SerializeField] private Vector3 gravity;
        [SerializeField] private TrailRenderer trail;
        private Vector3 destination;
        private Vector3 velocity;
        private float elapsedTime;

        [SerializeField] private TextMeshProUGUI islandText;

        private int islandIndex;

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
            if (aiming)
            {
                playerCore.velocity = Vector3.zero;
            }
            else
            {
                velocity += gravity * Time.fixedDeltaTime;
                playerCore.rb.linearVelocity = velocity;
                elapsedTime += Time.deltaTime;
                
                playerCore.rb.MoveRotation(Quaternion.LookRotation(velocity.normalized));

                if (elapsedTime >= travelTime)
                {
                    trail.emitting = false;
                    playerCore.baseCollider.enabled = true;
                    playerCore.groundedCollider.enabled = false;
                    playerCore.currentIsland = aimedIsland;
                    playerCore.airborne.airHit = true;
                    playerCore.playerCam.cam.transform.position = aimedIsland.camPos.position;
                    aimedIsland.Engage();
                    PlayerCore.mainPlayerCore.playerCam.cameraShake.ShakePresets.Explosion3D(1f, 1f);
                    //playerCore.animations.playerAnimator.Play("LaunchLand");
                    
                    playerCore.ChangeState(playerCore.airborne);
                }
            }
        }

        public void SetDestination(Vector3 position)
        {
            destination = position;
            
            // velocity x/z are constant
            velocity = destination - playerCore.transform.position;
            float deltaY = velocity.y;
            velocity.y = deltaY - (gravity.y / 2f * travelTime * travelTime);
            velocity /= travelTime;
            elapsedTime = 0f;
        }

        public void Launch()
        {
            if (!aiming) return;
            if (aimedIsland.currentState is Island.IslandState.Cleared) return;
            trail.emitting = true;
            SetDestination(aimedIsland.transform.position);
            playerCore.animations.playerAnimator.Play("Launch");

            playerCore.baseCollider.enabled = false;
            playerCore.groundedCollider.enabled = false;
            
            elapsedTime = 0f;
            aiming = false;
        }

        public void SwitchIsland(bool right)
        {
            Island[] islands = GameMan.gameMan.GetIslands();
            
            // scroll next/prev
            if (right)
            {
                islandIndex++;
                if (islandIndex >= islands.Length) islandIndex = 0;
            }
            else
            {
                islandIndex--;
                if (islandIndex < 0) islandIndex = islands.Length - 1;
            }

            aimedIsland = islands[islandIndex];
            
            // *
            if (islands[islandIndex].spawnIsland) SwitchIsland(right);
            islandText.text = "Island\n#" + islandIndex;

            playerCore.combat.roundText.text = islands[islandIndex].currentRound + "/" + islands[islandIndex].rounds;
        }

        public override bool Enter(State from)
        {
            aiming = true;
            aimedIsland = playerCore.currentIsland;
            Island[] islands = GameMan.gameMan.GetIslands();
            islandIndex = Array.IndexOf(islands, aimedIsland);
            if (aimedIsland.spawnIsland) SwitchIsland(true);
            
            return true;
        }
        
        public override bool Exit(State to)
        {
            return true;
        }
    }
}

