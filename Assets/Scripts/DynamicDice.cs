using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonRpg
{
    public class DynamicDice : MonoBehaviour
    {
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask clickMask = ~0;
        [SerializeField] private float rollDuration = 1.45f;
        [SerializeField] private float spinChangeInterval = 0.08f;
        [SerializeField] private float settleDuration = 0.28f;
        [SerializeField] private float liftHeight = 0.22f;
        [SerializeField] private float minimumSpinSpeed = 540f;
        [SerializeField] private float maximumSpinSpeed = 1260f;

        public int CurrentValue { get; private set; } = 1;
        public bool IsRolling { get; private set; }

        private Vector3 restingLocalPosition;

        private void Awake()
        {
            restingLocalPosition = transform.localPosition;
            ResolveReferences();
            transform.localRotation = GetRotationFacingCamera(CurrentValue);
        }

        private void Update()
        {
            if (IsRolling || turnManager == null || !turnManager.CanRollMovement)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, clickMask, QueryTriggerInteraction.Ignore) && hit.transform == transform)
            {
                turnManager.RequestPlayerRoll();
            }
        }

        public void SetTurnManager(TurnManager manager)
        {
            turnManager = manager;
        }

        public IEnumerator Roll(Action<int> onCompleted)
        {
            if (IsRolling)
            {
                yield break;
            }

            IsRolling = true;
            float elapsed = 0f;
            float nextSpinChange = 0f;
            Vector3 spinVelocity = CreateRandomSpinVelocity();
            while (elapsed < rollDuration)
            {
                float deltaTime = Time.deltaTime;
                elapsed += deltaTime;

                if (elapsed >= nextSpinChange)
                {
                    spinVelocity = CreateRandomSpinVelocity();
                    nextSpinChange = elapsed + spinChangeInterval;
                }

                float progress = Mathf.Clamp01(elapsed / rollDuration);
                float wobble = Mathf.Sin(progress * Mathf.PI * 8f) * 0.03f;
                transform.localPosition = restingLocalPosition + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * liftHeight + wobble);
                transform.Rotate(spinVelocity * deltaTime, Space.Self);
                yield return null;
            }

            CurrentValue = UnityEngine.Random.Range(1, 7);
            Quaternion startRotation = transform.localRotation;
            Quaternion resultRotation = GetRotationFacingCamera(CurrentValue);
            Vector3 startPosition = transform.localPosition;
            float settleElapsed = 0f;
            while (settleElapsed < settleDuration)
            {
                settleElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(settleElapsed / settleDuration));
                transform.localRotation = Quaternion.Slerp(startRotation, resultRotation, t);
                transform.localPosition = Vector3.Lerp(startPosition, restingLocalPosition, t);
                yield return null;
            }

            transform.localRotation = resultRotation;
            transform.localPosition = restingLocalPosition;
            IsRolling = false;
            onCompleted?.Invoke(CurrentValue);
        }

        public void ResetVisual()
        {
            IsRolling = false;
            transform.localPosition = restingLocalPosition;
            transform.localRotation = GetRotationFacingCamera(CurrentValue);
        }

        private void ResolveReferences()
        {
            if (turnManager == null)
            {
                turnManager = FindFirstObjectByType<TurnManager>();
            }

            ResolveCamera();
        }

        private Camera ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera;
        }

        private Vector3 CreateRandomSpinVelocity()
        {
            return new Vector3(
                RandomSignedSpeed(),
                RandomSignedSpeed(),
                RandomSignedSpeed());
        }

        private float RandomSignedSpeed()
        {
            float speed = UnityEngine.Random.Range(minimumSpinSpeed, maximumSpinSpeed);
            return UnityEngine.Random.value > 0.5f ? speed : -speed;
        }

        private Quaternion GetRotationFacingCamera(int value)
        {
            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return GetFallbackRotationForValue(value);
            }

            Vector3 directionToCamera = camera.orthographic
                ? -camera.transform.forward
                : (camera.transform.position - transform.position).normalized;

            if (directionToCamera.sqrMagnitude <= Mathf.Epsilon)
            {
                directionToCamera = Vector3.forward;
            }

            Quaternion faceBasis = Quaternion.LookRotation(GetFaceNormal(value), GetFaceUp(value));
            Quaternion cameraBasis = Quaternion.LookRotation(directionToCamera, camera.transform.up);
            Quaternion worldRotation = cameraBasis * Quaternion.Inverse(faceBasis);
            return transform.parent != null ? Quaternion.Inverse(transform.parent.rotation) * worldRotation : worldRotation;
        }

        private Vector3 GetFaceNormal(int value)
        {
            switch (value)
            {
                case 2:
                    return Vector3.forward;
                case 3:
                    return Vector3.left;
                case 4:
                    return Vector3.right;
                case 5:
                    return Vector3.back;
                case 6:
                    return Vector3.down;
                default:
                    return Vector3.up;
            }
        }

        private Vector3 GetFaceUp(int value)
        {
            switch (value)
            {
                case 1:
                    return Vector3.forward;
                case 6:
                    return Vector3.back;
                default:
                    return Vector3.up;
            }
        }

        private Quaternion GetFallbackRotationForValue(int value)
        {
            switch (value)
            {
                case 2:
                    return Quaternion.Euler(-90f, 0f, 0f);
                case 3:
                    return Quaternion.Euler(0f, 0f, -90f);
                case 4:
                    return Quaternion.Euler(0f, 0f, 90f);
                case 5:
                    return Quaternion.Euler(90f, 0f, 0f);
                case 6:
                    return Quaternion.Euler(180f, 0f, 0f);
                default:
                    return Quaternion.identity;
            }
        }
    }
}
