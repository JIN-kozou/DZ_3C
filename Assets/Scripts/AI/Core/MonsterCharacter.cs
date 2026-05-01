using DZ_3C.AI.Config;
using UnityEngine;

namespace DZ_3C.AI.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public class MonsterCharacter : CharacterBase
    {
        [SerializeField] private MonsterStatConfigSO statConfig;

        public MonsterStatConfigSO StatConfig => statConfig;

        private float yawVelocity;

        protected override void Awake()
        {
            base.Awake();
            disEnableRootMotion = true;
            ignoreRotationRootMotion = true;
            applyFullRootMotion = false;
            if (statConfig != null) disEnableGravity = statConfig.aerialMode;

            if (statConfig != null && statConfig.playerNumericConfig != null)
            {
                ApplyNumericConfig(statConfig.playerNumericConfig);
            }
        }

        public void SetStatConfig(MonsterStatConfigSO config)
        {
            statConfig = config;
            if (statConfig != null)
            {
                disEnableGravity = statConfig.aerialMode;
            }
            if (statConfig != null && statConfig.playerNumericConfig != null)
            {
                ApplyNumericConfig(statConfig.playerNumericConfig);
            }
        }

        public void MoveBy(Vector3 worldDelta)
        {
            if (worldDelta.sqrMagnitude <= 0.000001f) return;
            UpdateCharacterMove(worldDelta, Quaternion.identity);
        }

        public void RotateTowards(Vector3 worldDirection, float maxDegreesPerSecond)
        {
            Vector3 planar = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (planar.sqrMagnitude <= 0.000001f) return;

            float targetYaw = Quaternion.LookRotation(planar.normalized, Vector3.up).eulerAngles.y;
            float currentYaw = transform.eulerAngles.y;

            if (statConfig == null || statConfig.turnSmoothTime <= 0.001f)
            {
                float nextYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, maxDegreesPerSecond * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, nextYaw, 0f);
                return;
            }

            float smoothTime = Mathf.Max(0.02f, statConfig.turnSmoothTime);
            float smoothedYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime, maxDegreesPerSecond, Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        }
    }
}
