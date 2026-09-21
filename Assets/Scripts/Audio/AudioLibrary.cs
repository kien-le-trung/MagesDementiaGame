using UnityEngine;

namespace MagesDementiaGame
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "MAGES/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Header("Background")]
        public AudioClip RoomAmbience;
        public AudioClip ReflectionTheme;

        [Header("UI")]
        public AudioClip UiClick;
        public AudioClip UiConfirm;
        public AudioClip DialogueBlip;

        [Header("Table task")]
        public AudioClip ItemPickup;
        public AudioClip ItemPlace;
        public AudioClip InvalidDrop;
        public AudioClip ChecklistTick;

        [Header("Environment")]
        public AudioClip TelevisionOn;
        public AudioClip TelevisionOff;
        public AudioClip TelevisionVolumeDown;
        public AudioClip TelevisionStaticLoop;
        public AudioClip DoorOpening;
    }
}
