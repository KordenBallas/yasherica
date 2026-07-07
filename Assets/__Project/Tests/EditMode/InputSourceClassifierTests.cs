using GameInput.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class InputSourceClassifierTests
    {
        private readonly InputSourceClassifier _classifier = new InputSourceClassifier();

        [Test]
        public void KeyboardAndMouse_ClassifyAsKeyboardMouse()
        {
            Assert.AreEqual(InputSource.KeyboardMouse,
                _classifier.Classify(InputDeviceKind.Keyboard, isNativeDevice: true, actuationMagnitude: 1f));
            Assert.AreEqual(InputSource.KeyboardMouse,
                _classifier.Classify(InputDeviceKind.Mouse, isNativeDevice: true, actuationMagnitude: 1f));
        }

        [Test]
        public void NativeGamepad_ClassifiesAsGamepad()
        {
            Assert.AreEqual(InputSource.Gamepad,
                _classifier.Classify(InputDeviceKind.Gamepad, isNativeDevice: true, actuationMagnitude: 1f));
            Assert.AreEqual(InputSource.Gamepad,
                _classifier.Classify(InputDeviceKind.Joystick, isNativeDevice: true, actuationMagnitude: 1f));
        }

        [Test]
        public void VirtualGamepad_FromOnScreenControls_ClassifiesAsTouch()
        {
            // Unity's on-screen stick/button drive a virtual (non-native) Gamepad; touching the overlay
            // must keep the active source on Touch, not flip prompts to controller cues.
            Assert.AreEqual(InputSource.Touch,
                _classifier.Classify(InputDeviceKind.Gamepad, isNativeDevice: false, actuationMagnitude: 1f));
        }

        [Test]
        public void Touchscreen_ClassifiesAsTouch()
        {
            Assert.AreEqual(InputSource.Touch,
                _classifier.Classify(InputDeviceKind.Touchscreen, isNativeDevice: true, actuationMagnitude: 1f));
        }

        [Test]
        public void WeakActuation_LikeStickDrift_IsIgnored()
        {
            Assert.IsNull(_classifier.Classify(InputDeviceKind.Gamepad, isNativeDevice: true,
                actuationMagnitude: InputSourceClassifier.MinActuation - 0.01f));
        }

        [Test]
        public void UntrackedDeviceKind_IsIgnored()
        {
            Assert.IsNull(_classifier.Classify(InputDeviceKind.Other, isNativeDevice: true, actuationMagnitude: 1f));
        }
    }
}
