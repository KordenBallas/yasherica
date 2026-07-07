using System;
using GameInput.Core;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Zenject;

namespace GameInput.View
{
    /// <summary>
    /// Watches the Input System event stream and keeps <see cref="IActiveInputSource"/> pointed at the
    /// device family the player last actually used (Input Foundation R5). Classification rules live in
    /// the pure <see cref="InputSourceClassifier"/>; this adapter only maps devices/events onto its
    /// inputs. Listens to raw events rather than actions so ANY actuation counts — including buttons
    /// no action binds.
    /// </summary>
    public sealed class ActiveInputSourceTracker : IActiveInputSource, IInitializable, IDisposable
    {
        private readonly InputSourceClassifier _classifier;
        private InputSource _current = InputSource.KeyboardMouse;

        public InputSource Current => _current;

        public event Action<InputSource> Changed;

        public ActiveInputSourceTracker(InputSourceClassifier classifier)
        {
            _classifier = classifier;
        }

        public void Initialize()
        {
            InputSystem.onEvent += HandleEvent;
        }

        public void Dispose()
        {
            InputSystem.onEvent -= HandleEvent;
        }

        private void HandleEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            {
                return;
            }

            float strongest = -1f;
            foreach (var control in eventPtr.EnumerateChangedControls(device))
            {
                float magnitude = control.EvaluateMagnitude();
                if (magnitude < 0f && device is Mouse)
                {
                    // Position/delta controls report no magnitude but only change on physical use;
                    // for a mouse that IS the player picking it back up, so count it as full actuation.
                    magnitude = 1f;
                }

                if (magnitude > strongest)
                {
                    strongest = magnitude;
                }
            }

            if (strongest < 0f)
            {
                return;
            }

            var source = _classifier.Classify(ToKind(device), device.native, strongest);
            if (source == null || source.Value == _current)
            {
                return;
            }

            _current = source.Value;
            Changed?.Invoke(_current);
        }

        private static InputDeviceKind ToKind(InputDevice device)
        {
            switch (device)
            {
                case Keyboard _: return InputDeviceKind.Keyboard;
                case Mouse _: return InputDeviceKind.Mouse;
                case Touchscreen _: return InputDeviceKind.Touchscreen;
                case Gamepad _: return InputDeviceKind.Gamepad;
                case Joystick _: return InputDeviceKind.Joystick;
                default: return InputDeviceKind.Other;
            }
        }
    }
}
