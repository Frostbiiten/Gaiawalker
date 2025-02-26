using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace frost
{
    public enum StateID
    {
        Grounded,
        Airborne,
        Dash,
        Dodge,
        Travel,
        None
    }
    
    [Serializable]
    public abstract class State
    {
        public virtual StateID id
        {
            get 
            {
                return StateID.None; 
            }
        }

        protected PlayerCore playerCore;

        public void setPlayerCore(PlayerCore pc)
        {
            playerCore = pc;
        }
        
        public abstract void Awake();
        public abstract void Start();
        public abstract void Update();
        public abstract void FixedUpdate();
        public abstract bool Enter(State from);
        public abstract bool Exit(State to);
        public virtual void OnCollisionEnter(Collision collision) { }
        public virtual void OnCollisionStay(Collision collision) { }
    }
    
    public class PlayerCore : MonoBehaviour
    {
        public static PlayerCore mainPlayerCore;
        [field:SerializeField] public PlayerStats stats { private set; get; }
        
        // Physics
        [Header("Physics")]
        public Rigidbody rb;
        public float playerHeight { private set; get; } = 2.0f;
        [SerializeField] private float playerRadius = 1.0f;
        public CapsuleCollider baseCollider, groundedCollider;
        
        public Vector3 velocity
        {
            set
            {
                rb.linearVelocity = value;
                velocitySqrMagnitude = value.sqrMagnitude;
                velocityMagnitude = Mathf.Sqrt(value.sqrMagnitude);
                normalizedVelocity = value / velocityMagnitude;
            }
            get => rb.linearVelocity;
        }
        [field: NonSerialized] public Vector3 normalizedVelocity { private set; get; }
        [field: NonSerialized]public float velocitySqrMagnitude { private set; get; }
        [field: NonSerialized] public float velocityMagnitude { private set; get; }
        [field: NonSerialized] public Vector3 realVelocity { private set; get; }
        private Vector3 oldPos, newPos;
        
        // Input
        [Header("Input")]
        public PlayerInput playerInput;
        [field: NonSerialized] public Vector3 inputDirection { private set; get; }
        [field: NonSerialized] public bool directionalInput { private set; get; }
        
        // Floor detection
        [Header("Floor Detection")]
        public LayerMask floorLayerMask;
        public LayerMask floorLayerMaskNoEnemy;
        public LayerMask floorLayerTravel;
        public float floorDetectionRadius = 0.3f;
        public float floorDetectionOffset;
        public bool floorDetected { private set; get; }
        private int floorSurfacesDetected { set; get; }
        private Collider[] floorCastSurfaces { set; get; } = new Collider[6];

        // Floor detection ray
        [NonSerialized] public RaycastHit rayResult;
        public float floorDistance { private set; get; }
        [field: NonSerialized] public bool floorRayDetected { private set; get; }
        
        // Platform tracker
        private Transform platformTracker;
        private Vector3 trackerOldPos;
        private Vector3 trackerCurrentPos;
        
        // States
        [field: NonSerialized] public State currentState { private set; get; }
        public PlayerGrounded grounded;
        public PlayerAirborne airborne;
        public PlayerTravel travel;
        public PlayerDodge dodge;
        public PlayerDash dash;
        
        // Camera
        [field: SerializeField] public PlayerCamera playerCam { private set; get; }

        // Combat
        [field: Header("Combat")]
        [field: SerializeField] public PlayerCombat combat { private set; get; }
        
        // Animation
        [field: SerializeField] public PlayerAnims animations { private set; get; }
        
        // UI
        [field: SerializeField] public Canvas canvas { private set; get; }
        [field: SerializeField] public RectTransform UIContainer { private set; get; }
        
        // Islands
        public Island currentIsland;
        
        // Instantiate
        [SerializeField] private GameObject notification;
        [SerializeField] private Transform container;

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position - transform.up * (playerHeight / 2f + floorDetectionOffset), floorDetectionRadius);
        }

        public void Awake()
        {
            Application.targetFrameRate = 200;
            mainPlayerCore = this;
            platformTracker = new GameObject("tracker").transform;
            
            animations.setPlayerCore(this);
            grounded.setPlayerCore(this);
            airborne.setPlayerCore(this);
            travel.setPlayerCore(this);
            dodge.setPlayerCore(this);
            dash.setPlayerCore(this);
            playerInput.Init(this);
            combat.Init(this);
            
            // start sm
            currentState = airborne;
            currentState.Awake();
        }
        
        public void Start()
        {
            currentState.Start();
        }

        void Update()
        {
            //QuickDebugDrawer.instance.UpdateVar("State", currentState.id.ToString());
            
            // update input
            playerInput.Update();
            
            inputDirection = Vector3.ProjectOnPlane(playerCam.cam.transform.rotation * new Vector3(playerInput.directionalInput.x, 0.0f, playerInput.directionalInput.y), transform.up).normalized;
            directionalInput = inputDirection.sqrMagnitude >= playerInput.deadZone;
            
            currentState.Update();
            combat.Update();
            
            // update camera, animations
            animations.Update();
        }

        private void LateUpdate()
        {
            playerCam.LateUpdate();
            animations.LateUpdate();
        }

        Collider[] travelSurfaces = new Collider[2];
        
        // Updates floor detection spherecast
        void UpdateFloorCast()
        {
            //Update floor surfaces detected from overlap sphere
            //floorSurfacesDetected = Physics.OverlapSphereNonAlloc(transform.position - transform.up * (playerHeight / 2f + floorDetectionOffset), floorDetectionRadius, floorCastSurfaces, floorLayerMaskRaw, QueryTriggerInteraction.Ignore);
            floorSurfacesDetected = Physics.OverlapSphereNonAlloc(transform.position - transform.up * (playerHeight / 2f + floorDetectionOffset), floorDetectionRadius, floorCastSurfaces, floorLayerMask, QueryTriggerInteraction.Ignore);
            floorDetected = floorSurfacesDetected > 0;

            if (floorDetected && floorCastSurfaces[0].transform != platformTracker.parent)
            {
                platformTracker.parent = floorCastSurfaces[0].transform;
                platformTracker.position = transform.position - Vector3.up;
                trackerOldPos = platformTracker.position;
                trackerCurrentPos = platformTracker.position;
            }

            if (currentState.id is not StateID.Travel && Physics.OverlapSphereNonAlloc(
                    transform.position - transform.up * (playerHeight / 2f + floorDetectionOffset),
                    floorDetectionRadius, travelSurfaces, floorLayerTravel, QueryTriggerInteraction.Ignore) > 0)
            {
                ChangeState(travel);
            }
        }

        // Updates floor detection raycasts
        void UpdateFloorRays()
        {
            floorRayDetected = Physics.Raycast(transform.position, -transform.up, out rayResult, playerHeight, floorLayerMaskNoEnemy, QueryTriggerInteraction.Ignore);
            if (floorRayDetected) floorDistance = rayResult.distance - playerHeight / 2.0f;
            else floorDistance = playerHeight;
        }

        // Updates the position of the floor tracker, allowing the player to stand on moving platforms
        void UpdateFloorTracker()
        {
            //Track platform
            trackerOldPos = trackerCurrentPos;
            if (platformTracker) trackerCurrentPos = platformTracker.position;

            //Move player if they are standing on the platform
            if (currentState == grounded)
            {
                transform.position += trackerCurrentPos - trackerOldPos;
            }
        }
        
        void FixedUpdate()
        {
            UpdateFloorTracker();
            UpdateFloorCast();
            UpdateFloorRays();
            
            // update 'real' velocity
            oldPos = newPos;
            newPos = transform.position;
            realVelocity = newPos - oldPos;
            
            currentState.FixedUpdate();
            combat.FixedUpdate();
        }
        
        public void Notify(String text)
        {
            Notification notif = Instantiate(notification, container).GetComponent<Notification>();
            notif.text.text = text;
        }

        private void OnCollisionEnter(Collision collision)
        {
            combat.OnCollisionEnter(collision);
            currentState.OnCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collisionInfo)
        {
            currentState.OnCollisionStay(collisionInfo);
        }

        public bool ChangeState(State newState)
        {
            bool success = (currentState.Exit(newState) && newState.Enter(currentState));
            if (success) currentState = newState;
            return success;
        }
    }
}
