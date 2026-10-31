using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SoundThief.SoundScan
{
    // The test scene temporarily swaps only the active quality's pipeline, then restores it.
    public sealed class SoundScanDemo : MonoBehaviour
    {
        public RenderPipelineAsset demoPipeline;
        public SoundScanController scanner;
        public Transform soundSource;
        public bool repeatAutomatically = true;
        RenderPipelineAsset previousPipeline;
        float nextScan;
        void OnEnable()
        {
            previousPipeline = QualitySettings.renderPipeline;
            if (demoPipeline) QualitySettings.renderPipeline = demoPipeline;
            nextScan = Time.time + .5f;
        }
        void OnDisable()
        {
            if (demoPipeline && QualitySettings.renderPipeline == demoPipeline) QualitySettings.renderPipeline = previousPipeline;
        }
        void Update()
        {
            if (!scanner || !soundSource) return;
            var key = Keyboard.current;
            if ((key != null && key.vKey.wasPressedThisFrame) || (repeatAutomatically && Time.time >= nextScan))
            {
                scanner.TriggerSoundScan(soundSource.position, 1);
                nextScan = Time.time + 5f;
            }
            if (key == null) return;
            Vector3 move = new Vector3((key.dKey.isPressed ? 1 : 0) - (key.aKey.isPressed ? 1 : 0),
                (key.eKey.isPressed ? 1 : 0) - (key.qKey.isPressed ? 1 : 0),
                (key.wKey.isPressed ? 1 : 0) - (key.sKey.isPressed ? 1 : 0));
            scanner.transform.position += scanner.transform.TransformDirection(move) * (Time.deltaTime * 4);
            float turn = (key.rightArrowKey.isPressed ? 1 : 0) - (key.leftArrowKey.isPressed ? 1 : 0);
            scanner.transform.Rotate(Vector3.up, turn * 55 * Time.deltaTime, Space.World);
        }
    }
}
