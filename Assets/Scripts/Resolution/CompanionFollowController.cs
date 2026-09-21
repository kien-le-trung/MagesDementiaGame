using System.Collections.Generic;
using UnityEngine;

namespace MagesDementiaGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CompanionFollowController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float sampleDistance = 0.25f;
        [SerializeField] private float followDistance = 1.15f;

        private readonly Queue<Vector2> trail = new Queue<Vector2>();
        private Rigidbody2D body;
        private CharacterVisualController visual;
        private Transform leader;
        private Vector2 lastSample;
        private bool following;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            visual = GetComponentInChildren<CharacterVisualController>();
            enabled = false;
        }

        public void BeginFollowing(Transform target)
        {
            leader = target;
            trail.Clear();
            lastSample = target.position;
            trail.Enqueue(lastSample);
            following = true;
            enabled = true;
        }

        public void StopFollowing()
        {
            following = false;
            trail.Clear();
            visual?.SetMotion(Vector2.zero);
            enabled = false;
        }

        private void Update()
        {
            if (!following || leader == null) return;
            var leaderPosition = (Vector2)leader.position;
            if (Vector2.Distance(lastSample, leaderPosition) >= sampleDistance)
            {
                lastSample = leaderPosition;
                trail.Enqueue(leaderPosition);
            }
        }

        private void FixedUpdate()
        {
            if (!following || leader == null || trail.Count == 0) return;
            if (Vector2.Distance(body.position, leader.position) <= followDistance && trail.Count <= 1)
            {
                visual?.SetMotion(Vector2.zero);
                return;
            }

            var target = trail.Peek();
            var delta = target - body.position;
            if (delta.magnitude < 0.08f)
            {
                trail.Dequeue();
                visual?.SetMotion(Vector2.zero);
                return;
            }

            var movement = delta.normalized;
            body.MovePosition(body.position + movement * (moveSpeed * Time.fixedDeltaTime));
            visual?.SetMotion(movement);
        }
    }
}
