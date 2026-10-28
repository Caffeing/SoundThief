using UnityEngine;
namespace SoundThief
{
    public sealed class SoundProjectile : MonoBehaviour
    {
        SoundCharge charge;
        SoundWorld world;
        public void Init(SoundCharge value, SoundWorld soundWorld) { charge = value; world = soundWorld; }
        void Update() { if (charge != null && charge.Remaining <= 0) Detonate(); }
        void OnCollisionEnter(Collision collision)
        {
            foreach (var contact in collision.contacts)
                if (contact.normal.y > 0.5f) { Detonate(); break; }
        }
        public void Detonate()
        {
            if (charge == null || !charge.Finish()) return;
            world.Blast(transform.position, charge.Grade, false);
            Destroy(gameObject);
        }
        void OnDestroy() { charge?.Finish(); }
    }

}

