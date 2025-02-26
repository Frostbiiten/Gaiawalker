using System;
using CameraShake;
using UnityEngine;

namespace frost
{
    [Serializable]
    public class PlayerCamera
    {
        public Camera cam;

        [SerializeField] private Vector2 distanceRange;
        [SerializeField] private Vector2 enemyDistanceRange;
        [SerializeField] private float offsetClamp = 10.0f;
        
        [SerializeField] private PlayerCore pc;
        [SerializeField] private Transform target;
        [SerializeField] private float posLerpT = 0.5f;
        [SerializeField] private float rotLerpT = 0.5f;
        [SerializeField] private float lookahead;

        private Vector3 delta = new Vector3();
        private Vector3 up = Vector3.up;
        [SerializeField] private float minOffset = 0.1f;
        [SerializeField] private Vector2 yClampRange;
        public float lerpTest = 0.5f;
        private float currentFocusShift;

        public float defaultFOV = 60f, enemyFOV = 45f, spawnFOV = 45f;
        [field: SerializeField] public CameraShaker cameraShake {private set; get; }

        private Vector3[] rayDirections =
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            //new Vector3(0f, 1f, 0f),
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, -1f)
        };

        [SerializeField] private Vector3 rotationVector;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float lookDistance;
        private float currentRotation;

        public void LateUpdate()
        {
            Vector3 camPos = cam.transform.position, targetPos = target.transform.position + pc.velocity * lookahead;
            Quaternion targetRot;

            if (pc.currentState.id == StateID.Grounded)
            {
                up = target.transform.up;
            }
            
            Enemy enemy = pc.combat.killStop <= 0f ? pc.combat.currentEnemy : pc.combat.lastKilled;

            bool aiming = pc.travel.aiming;
            if (aiming)
            {
                // different camera
                if (pc.travel.aimedIsland == null || !pc.travel.aiming)
                {
                    targetPos = pc.currentIsland.camPos.position;
                    targetRot = Quaternion.LookRotation((target.transform.position - cam.transform.position).normalized);
                }
                else
                {
                    Vector3 dir = (Quaternion.AngleAxis(currentRotation, Vector3.up) * rotationVector).normalized;
                    targetPos = Vector3.Lerp(cam.transform.position, pc.travel.aimedIsland.transform.position + dir * lookDistance, 1f);
                    targetRot = Quaternion.LookRotation((pc.travel.aimedIsland.transform.position - targetPos).normalized);
                    currentRotation += rotationSpeed * Time.deltaTime;
                }
            }
            else if (pc.combat.enemyFocused || enemy != null)
            {
                //Vector3 enemyPos = enemy.transform.position;
                Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 screenMax = new Vector2(-1, -1);
                
                Bounds viewBounds = new Bounds(enemy.transform.position, Vector3.zero);
                viewBounds.Encapsulate(enemy.hitVFXCollider.bounds);
                viewBounds.Encapsulate(pc.baseCollider.bounds);
                
                const float margin = 1.12f;
                float maxExtent = viewBounds.extents.magnitude;
                float minDistance = (maxExtent * margin) / Mathf.Sin(Mathf.Deg2Rad * cam.fieldOfView / 2.0f);
                Vector3 enemyPos = viewBounds.center * 0.6f + enemy.transform.position * 0.4f;
                
                // Get vector from target enemy to player
                Vector3 vec0 = target.transform.position - enemyPos;
                float playerDistance = vec0.magnitude; // get current distance
                vec0 /= playerDistance; // normalize
                //vec0 += target.transform.right * 0.1f;
                //vec0.Normalize();
                
                // TEMP (?) - make sure above
                //vec0.y = Math.Max(delta.y, 0.05f); 
                //vec0.Normalize();
                
                // Get vector from target enemy to camera
                Vector3 vec1 = (cam.transform.position - enemyPos);
                float currentDist = vec1.magnitude; // get current distance
                vec1 /= currentDist; // normalize
                
                // Get 'offset' between vectors - should i convert this to 'local' space relative to camera????
                Vector3 offset = (vec1 - vec0);

                // clamp magnitude of vector
                float offsetMagnitude = offset.magnitude;
                offset = offset.normalized * Mathf.Min(offsetMagnitude, offsetClamp);
                Vector3 vec3 = (vec0 + offset).normalized;
                vec3.y = Mathf.Clamp(vec3.y, yClampRange.x, yClampRange.y);

                float minOffset2 = pc.combat.hitStop > 0f ? 1f : 0;
                //float distance = Mathf.Pow(a, 1.02f) * separationMultiplier;
                //float distance = Mathf.Pow(a, 1.6f) * separationMultiplier;
                vec3 *= Mathf.Clamp(minDistance + offsetMagnitude - minOffset2, playerDistance + enemyDistanceRange.x + minOffset - minOffset2, playerDistance + enemyDistanceRange.y);
                
                Vector3 resultant = enemyPos + vec3;
                
                // set target pos/rot
                targetPos = (pc.currentState.id == StateID.Dash && pc.dash.dashTimer < 0.15f) ? Vector3.Lerp(cam.transform.position, resultant, 0.01f) : resultant;
                
                currentFocusShift = Mathf.Lerp(currentFocusShift, pc.airborne.hitFly > 0 ? 1f : lerpTest, Time.deltaTime);
                Vector3 lookVec = (Vector3.Lerp(enemyPos, target.transform.position, currentFocusShift) - cam.transform.position).normalized;
                targetRot = Quaternion.LookRotation(lookVec, up);
            }
            else
            {
                delta = targetPos - camPos;
                float currentDistance = Vector3.Magnitude(delta);
                delta.Normalize();
                delta = Quaternion.FromToRotation(target.transform.up, Vector3.up) * delta;
                delta.y = Math.Min(delta.y, -0.1f);
                delta = Quaternion.FromToRotation(Vector3.up, target.transform.up) * delta;
                delta.Normalize();

                
                // adjust target position rotation
                targetPos -= delta * Mathf.Clamp(currentDistance, distanceRange.x, distanceRange.y);
                targetRot = Quaternion.LookRotation(delta, up);
            }

            float targetFov = aiming ? spawnFOV : (pc.combat.enemyFocused ? enemyFOV : defaultFOV);
            float currentFOV = Mathf.Lerp(cam.fieldOfView, targetFov, 0.5f * Time.deltaTime);
            cam.fieldOfView = currentFOV;
            
            // Interpolate
            targetPos = RayCheck(targetPos);
            Vector3 newPos = Vector3.Lerp(camPos, targetPos, 1.0f - Mathf.Pow(posLerpT, Time.deltaTime));
            Quaternion newRot = Quaternion.Slerp(cam.transform.rotation, targetRot, 1.0f - Mathf.Pow(rotLerpT, Time.deltaTime));
            
            //Quaternion newRot = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(t.transform.position-cam.transform.position, up), 1.0f - Mathf.Pow(rotLerpT, Time.deltaTime));
            cam.transform.SetPositionAndRotation(newPos, newRot);
        }

        private Vector3 RayCheck(Vector3 pos)
        {
            const float dist = 2f;
            RaycastHit hit;
            for (int i = 0; i < rayDirections.Length; i++)
            {
                Vector3 current = rayDirections[i];
                Debug.DrawRay(pos, current * dist);
                bool h = Physics.Raycast(pos, current, out hit, dist);
                if (h)
                {
                    pos -= current * (dist - Mathf.Min(hit.distance, dist));
                }
            }

            return pos;
        }

        public void Impact()
        {
            cameraShake.ShakePresets.ShortShake3D();
        }
    }
}
