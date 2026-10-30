using UnityEngine;

namespace SoundThief
{
    [CreateAssetMenu(menuName = "Sound Thief/Sound Audio Library")]
    public sealed class SoundAudioLibrary : ScriptableObject
    {
        public AudioClip doorOpen;
        public AudioClip doorClose;
        public AudioClip keyPickup;
        public AudioClip treasurePickup;
        public AudioClip collectiblePickup;
        public AudioClip resonator;
        public AudioClip playerFootstep;
        public AudioClip guardFootstep;
        [Range(0, 1)] public float doorVolume = 0.8f;
        [Range(0, 1)] public float pickupVolume = 0.75f;
        [Range(0, 1)] public float resonatorVolume = 1f;

        public AudioClip PickupClip(InteractionKind kind)
        {
            if (kind == InteractionKind.Key) return keyPickup;
            if (kind == InteractionKind.Treasure) return treasurePickup;
            return collectiblePickup;
        }

        public AudioClip RelationClip(ThiefInteractable item)
        {
            if (!item) return resonator;
            if (item.kind == InteractionKind.Door) return doorOpen;
            if (item.kind == InteractionKind.Key) return keyPickup;
            if (item.kind == InteractionKind.Treasure) return treasurePickup;
            if (item.kind == InteractionKind.Collectible) return collectiblePickup;
            return resonator;
        }
    }
}
