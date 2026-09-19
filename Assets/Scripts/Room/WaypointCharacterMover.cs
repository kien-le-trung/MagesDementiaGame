using System;
using System.Collections;
using UnityEngine;

namespace MagesDementiaGame
{
    public sealed class WaypointCharacterMover : MonoBehaviour
    {
        private CharacterVisualController visual;
        private Coroutine movementRoutine;

        public bool IsMoving => movementRoutine != null;

        private void Awake()
        {
            visual = GetComponentInChildren<CharacterVisualController>();
        }

        public void MoveAlong(Vector3[] waypoints, float speed, Action completed)
        {
            StopMovement();
            movementRoutine = StartCoroutine(MoveRoutine(waypoints, speed, completed));
        }

        public void StopMovement()
        {
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
                movementRoutine = null;
            }
            visual?.SetMotion(Vector2.zero);
        }

        private IEnumerator MoveRoutine(Vector3[] waypoints, float speed, Action completed)
        {
            foreach (var waypoint in waypoints)
            {
                while ((transform.position - waypoint).sqrMagnitude > 0.0025f)
                {
                    var difference = waypoint - transform.position;
                    visual?.SetMotion(difference);
                    transform.position = Vector3.MoveTowards(transform.position, waypoint, speed * Time.deltaTime);
                    yield return null;
                }
                transform.position = waypoint;
            }

            visual?.SetMotion(Vector2.zero);
            movementRoutine = null;
            completed?.Invoke();
        }
    }
}
