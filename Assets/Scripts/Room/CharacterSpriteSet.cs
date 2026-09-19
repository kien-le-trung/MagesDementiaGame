using System;
using UnityEngine;

namespace MagesDementiaGame
{
    [Serializable]
    public sealed class CharacterSpriteSet
    {
        public Sprite Idle;
        public Sprite Portrait;
        public Sprite[] WalkUp = Array.Empty<Sprite>();
        public Sprite[] WalkDown = Array.Empty<Sprite>();
        public Sprite[] WalkLeft = Array.Empty<Sprite>();
        public Sprite[] WalkRight = Array.Empty<Sprite>();
        public float WorldHeight = 1.45f;

        public bool HasWorldSprites => Idle != null || (WalkDown != null && WalkDown.Length > 0);
    }
}
