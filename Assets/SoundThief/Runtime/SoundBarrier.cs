using UnityEngine;

namespace SoundThief
{
    // Put this on structural walls, not every piece of solid furniture.
    public sealed class SoundBarrier : MonoBehaviour
    {
        [Range(.05f, 1f)] public float rangeMultiplier = .65f;
    }
}
