using UnityEngine;

namespace MagesDementiaGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterVisualController : MonoBehaviour
    {
        private const float FramesPerSecond = 7f;
        private const int SortingBase = 500;

        private SpriteRenderer spriteRenderer;
        private CharacterSpriteSet sprites;
        private Vector2 facing = Vector2.down;
        private float animationTime;
        private bool moving;

        public Vector2 FacingDirection => facing;

        public void Initialize(CharacterSpriteSet spriteSet)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            sprites = spriteSet;
            if (sprites != null && sprites.Idle != null)
            {
                spriteRenderer.sprite = sprites.Idle;
            }

            ApplyWorldHeight();
            SetMotion(Vector2.zero);
        }

        public void SetMotion(Vector2 motion)
        {
            moving = motion.sqrMagnitude > 0.001f;
            if (moving)
            {
                facing = motion.normalized;
            }
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                facing = direction.normalized;
            }
            SetMotion(Vector2.zero);
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingOrder = SortingBase - Mathf.RoundToInt(transform.position.y * 100f);
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (sprites == null || !sprites.HasWorldSprites)
            {
                return;
            }

            if (!moving)
            {
                animationTime = 0f;
                if (sprites.Idle != null)
                {
                    SetSprite(sprites.Idle);
                }
                return;
            }

            var frames = SelectFrames();
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            animationTime += Time.deltaTime;
            SetSprite(frames[Mathf.FloorToInt(animationTime * FramesPerSecond) % frames.Length]);
        }

        private Sprite[] SelectFrames()
        {
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
            {
                return facing.x < 0f ? sprites.WalkLeft : sprites.WalkRight;
            }

            return facing.y > 0f ? sprites.WalkUp : sprites.WalkDown;
        }

        private void ApplyWorldHeight()
        {
            if (spriteRenderer.sprite == null || sprites == null)
            {
                return;
            }

            var nativeHeight = spriteRenderer.sprite.bounds.size.y;
            if (nativeHeight <= 0f)
            {
                return;
            }

            var scale = sprites.WorldHeight / nativeHeight;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetSprite(Sprite sprite)
        {
            if (sprite == null || spriteRenderer.sprite == sprite)
            {
                return;
            }

            spriteRenderer.sprite = sprite;
            ApplyWorldHeight();
        }
    }
}
