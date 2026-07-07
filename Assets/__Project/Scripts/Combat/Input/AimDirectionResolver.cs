using Combat.Config;
using GameInput.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat.Input
{
    /// <summary>
    /// Device adapter behind <see cref="IAimDirectionResolver"/>. On keyboard+mouse (and touch, which
    /// is a pointer) it keeps the shipped gesture exactly: cursor ray → ground plane at the character's
    /// feet → planar direction. On gamepad the left stick IS the direction, mapped through the camera's
    /// planar basis so "up on the stick" means "away from the camera". Reads devices directly — this is
    /// the one adapter allowed to, because a screen POSITION and a direction VECTOR cannot share one
    /// action value without ambiguity; the declared bindings live on the asset's Player/Aim action.
    /// </summary>
    public sealed class AimDirectionResolver : IAimDirectionResolver
    {
        private readonly IActiveInputSource _activeSource;
        private readonly InputConfig _config;
        private UnityEngine.Camera _camera;

        public AimDirectionResolver(IActiveInputSource activeSource, InputConfig config)
        {
            _activeSource = activeSource;
            _config = config;
        }

        public Vector3? ResolveAimDirection(Transform characterTransform)
        {
            if (characterTransform == null)
            {
                return null;
            }

            return _activeSource.Current == InputSource.Gamepad
                ? FromStick(characterTransform)
                : FromPointer(characterTransform);
        }

        private Vector3? FromPointer(Transform characterTransform)
        {
            var camera = ResolveCamera();
            if (camera == null || Mouse.current == null)
            {
                return null;
            }

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var groundPlane = new Plane(Vector3.up, characterTransform.position);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 direction = ray.GetPoint(distance) - characterTransform.position;
                direction.y = 0f;

                if (direction.magnitude > _config.inputDeadzone)
                {
                    return direction.normalized;
                }
            }

            return null;
        }

        private Vector3? FromStick(Transform characterTransform)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return null;
            }

            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude < _config.inputDeadzone)
            {
                return null;
            }

            Vector3 forward;
            Vector3 right;
            var camera = ResolveCamera();
            if (camera != null)
            {
                forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            }
            else
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }

            return (right * stick.x + forward * stick.y).normalized;
        }

        private UnityEngine.Camera ResolveCamera()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }

            return _camera;
        }
    }
}
