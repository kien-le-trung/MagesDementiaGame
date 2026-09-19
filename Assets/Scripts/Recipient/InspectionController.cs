using System;
using System.Collections;
using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class InspectionController : MonoBehaviour
    {
        private const float TweenDuration = 0.35f;
        private const float InspectionSize = 2.2f;

        private Camera roomCamera;
        private Coroutine routine;
        private Vector3 originalPosition;
        private float originalSize;
        private Action completed;

        public bool IsInspecting { get; private set; }
        public bool PhotographVisible { get; private set; }

        public void Initialize(Camera camera)
        {
            roomCamera = camera;
        }

        public void BeginPhotographInspection(bool photographVisible, Vector3 tablePosition, Action onCompleted)
        {
            if (IsInspecting || roomCamera == null)
            {
                return;
            }

            IsInspecting = true;
            PhotographVisible = photographVisible;
            completed = onCompleted;
            originalPosition = roomCamera.transform.position;
            originalSize = roomCamera.orthographicSize;
            var target = new Vector3(tablePosition.x, tablePosition.y, originalPosition.z);
            routine = StartCoroutine(TweenCamera(originalPosition, target, originalSize, InspectionSize, null));
        }

        public void FinishInspection()
        {
            if (!IsInspecting)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            var startPosition = roomCamera.transform.position;
            var startSize = roomCamera.orthographicSize;
            routine = StartCoroutine(TweenCamera(startPosition, originalPosition, startSize, originalSize, Complete));
        }

        private IEnumerator TweenCamera(Vector3 from, Vector3 to, float fromSize, float toSize, Action onFinished)
        {
            var elapsed = 0f;
            while (elapsed < TweenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / TweenDuration));
                roomCamera.transform.position = Vector3.Lerp(from, to, amount);
                roomCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, amount);
                yield return null;
            }

            roomCamera.transform.position = to;
            roomCamera.orthographicSize = toSize;
            routine = null;
            onFinished?.Invoke();
        }

        private void Complete()
        {
            IsInspecting = false;
            var callback = completed;
            completed = null;
            callback?.Invoke();
        }
    }
}
