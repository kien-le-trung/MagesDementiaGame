using UnityEngine;

namespace MagesDementiaGame
{
    [CreateAssetMenu(menuName = "MAGES/Room Art Set", fileName = "RoomArtSet")]
    public sealed class RoomArtSet : ScriptableObject
    {
        public Sprite Background;
        public Sprite Sofa;
        public Sprite Table;
        public Sprite Television;
        public CharacterSpriteSet Lan = new CharacterSpriteSet();
        public CharacterSpriteSet Minh = new CharacterSpriteSet();
    }
}
