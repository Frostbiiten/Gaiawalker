using System;
using UnityEngine;

namespace frost
{
    [CreateAssetMenu(fileName = "PlayerCombatStats", menuName = "Frost/PlayerCombatStats", order = 1)]
    public class CombatStats : ScriptableObject
    {
        [field: SerializeField] public float maxHP { get; private set; }
        [field: SerializeField] public Vector2 parryRange { get; private set; }
        [field: SerializeField] public float deathHitstop { get; private set; }
        public Attack[] attacks = new Attack[2];
    }
}
