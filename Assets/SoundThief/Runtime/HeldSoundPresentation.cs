using UnityEngine;

namespace SoundThief
{
    // Visual feedback only: the charge's deadline and its gameplay effects remain authoritative.
    public sealed class HeldSoundPresentation : MonoBehaviour
    {
        public Transform hand;
        public GameObject chargePrefab, burstPrefab;
        ThiefPlayer player;
        SoundCharge shownCharge;
        GameObject effect;
        float initialDuration, acquiredAt;
        Vector3 restPosition;
        Quaternion restRotation;
        public float Strain { get; private set; }

        public void Initialize(ThiefPlayer owner)
        {
            player = owner;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            restPosition = hand.localPosition;
            restRotation = hand.localRotation;
            hand.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (!player || !hand) return;
            var current = player.Held;
            if (current != shownCharge)
            {
                if (effect) Destroy(effect);
                shownCharge = current;
                hand.gameObject.SetActive(current != null);
                if (current != null)
                {
                    initialDuration = Mathf.Max(.01f, current.Remaining);
                    acquiredAt = Time.time;
                    if (chargePrefab)
                    {
                        effect = Instantiate(chargePrefab, hand);
                        effect.name = "손에 담긴 소리";
                        effect.transform.localPosition = new Vector3(0, .09f, .15f);
                        effect.transform.localRotation = Quaternion.identity;
                        effect.transform.localScale = Vector3.one * .07f;
                        foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                        {
                            var main = particle.main;
                            main.loop = true;
                            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                            // Projectile trails and full-screen cones obscure the player's view at hand range.
                            if (particle.transform == effect.transform || particle.name == "LightCone" || particle.name == "Slash" || particle.name == "DarkBG" || particle.name == "Flare")
                            {
                                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                                var visual = particle.GetComponent<ParticleSystemRenderer>();
                                if (visual) visual.enabled = false;
                            }
                        }
                    }
                }
            }
            if (current == null) { Strain = 0; return; }
            Strain = 1f - Mathf.Clamp01(current.Remaining / initialDuration);
            float effort = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, 1f, Strain));
            float reach = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.time - acquiredAt) / .55f));
            float t = Time.time;
            hand.localPosition = restPosition + new Vector3(0, -.15f, -.1f) * (1f - reach)
                + new Vector3(Mathf.Sin(t * 17f) * .012f, Mathf.Sin(t * 13f) * .009f - effort * .035f, Mathf.Sin(t * 11f) * .005f) * effort;
            hand.localRotation = restRotation * Quaternion.Euler(-35f * (1f - reach) + Mathf.Sin(t * 12f) * 5f * effort, 0, Mathf.Sin(t * 16f) * 7f * effort);
            if (effect) effect.transform.localScale = Vector3.one * (.07f + .015f * effort * (1f + Mathf.Sin(t * 9f))) * reach;
        }

        public void Burst()
        {
            if (!burstPrefab || !hand) return;
            var burst = Instantiate(burstPrefab, hand.position, hand.rotation);
            burst.name = "손에서 터진 소리";
            burst.transform.localScale = Vector3.one * .15f;
            foreach (var particle in burst.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            Destroy(burst, 6f);
        }
    }
}
