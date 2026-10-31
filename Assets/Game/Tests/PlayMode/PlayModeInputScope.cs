using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoundThief.Tests
{
    // Synthetic input must not depend on which desktop window currently has focus.
    sealed class PlayModeInputScope : IDisposable
    {
        readonly InputSettings original;
        readonly InputSettings settings;
        readonly Keyboard keyboard;
        public PlayModeInputScope()
        {
            original = InputSystem.settings;
            settings = UnityEngine.Object.Instantiate(original);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = settings;
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
        }
        public void Dispose()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = original;
            UnityEngine.Object.Destroy(settings);
        }
    }
}
