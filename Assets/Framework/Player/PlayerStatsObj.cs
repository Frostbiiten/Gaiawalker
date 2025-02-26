using UnityEngine;

namespace frost
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Frost/PlayerStats", order = 1)]
    public class PlayerStats : ScriptableObject
    {
        [field: SerializeField] public float groundBaseSpeed { private set; get; }
        [field: SerializeField] public float groundTopSpeed {private set; get; }
        [field: SerializeField] public float groundAcceleration { private set; get; }
        [field: SerializeField] public float groundDeceleration { private set; get; }
        [field: SerializeField] public float groundFriction { private set; get; }
        [field: SerializeField] public AnimationCurve groundTurnSpeed { private set; get; }
        [field: SerializeField] public float fallDistThreshold { private set; get; }
        [field: SerializeField] public float jumpForce { private set; get; }
        
        // air
        [field: SerializeField] public float airSpeed { private set; get; }
        [field: SerializeField] public Vector3 gravity { private set; get; }
        [field: SerializeField] public float airDrag { private set; get; }
        [field: SerializeField] public float landDistThreshold { private set; get; }
        
        // dash (maybe larger attack hitbox?)
        [field: SerializeField] public float dashSpeed { private set; get; }
        [field: SerializeField] public float dashStartDelay { private set; get; }
        [field: SerializeField] public float dashTurnSpeed { private set; get; }
        [field: SerializeField] public float maxDashTime { private set; get; }
        
        // combat
        [field: SerializeField] public float enemyDetectionDistance { get; private set; }
        [field: SerializeField] public float enemyEngageDistance { get; private set; }
        
        // Dodge
        [field: SerializeField] public float dodgeSpeed { get; private set; }
        [field: SerializeField] public float dodgeTime { get; private set; }
        [field: SerializeField] public float dodgeCooldown { get; private set; }
        [field: SerializeField] public AnimationCurve dodgeCurve { private set; get; }
    }
}
