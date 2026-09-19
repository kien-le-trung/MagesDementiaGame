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
    }
}
