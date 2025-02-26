using System;
using UnityEngine;
using Object = System.Object;

namespace frost
{
    public struct Hit
    {
        public Object source;
        public float damage;
        public Vector3 knockback;

        public Hit(Object src, float dmg, Vector3 kb)
        {
            source = src;
            damage = dmg;
            knockback = kb;
        }
    }

    public enum AttackState
    {
        Windup,
        Active,
        Cooldown,
        Charging,
        None
    }

    public enum PlayerAttack
    {
        LightA = 0,
        HeavyA = 1,
        DashHit = 2,
    }
    
    [Serializable]
    public class Attack
    {
        [field: SerializeField] public int id { get; private set; }
        [field: SerializeField] public float windup { get; private set; }
        [field: SerializeField] public float hitTime { get; private set; }
        [field: SerializeField] public float cooldown { get; private set; }
        [field: SerializeField] public float hitstop { get; private set; }
        [field: SerializeField] public float damage { get; private set; }
        [field: SerializeField] public Vector3 knockback { get; private set; }
        [field: SerializeField] public AnimationCurve velocity { get; private set; }
        [field: SerializeField] public bool parryable { get; private set; } = false;
        [field: SerializeField] public bool multiHit { get; private set; } = false;
    }
}
