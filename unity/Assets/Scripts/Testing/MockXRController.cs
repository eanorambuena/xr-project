using UnityEngine;

namespace VRCar.Tests.Mocks
{
    /// <summary>
    /// Mock de XRController para testing
    /// Simula inputs de Meta Quest 3 sin necesidad del hardware real
    /// </summary>
    public class MockXRController
    {
        public float triggerValue = 0f;
        public Vector2 joystickValue = Vector2.zero;
        public bool primaryButtonPressed = false;
        public bool secondaryButtonPressed = false;

        public void SetTriggerValue(float value)
        {
            triggerValue = Mathf.Clamp01(value);
        }

        public void SetJoystickValue(Vector2 value)
        {
            joystickValue = new Vector2(
                Mathf.Clamp(value.x, -1f, 1f),
                Mathf.Clamp(value.y, -1f, 1f)
            );
        }

        public void SetPrimaryButton(bool pressed)
        {
            primaryButtonPressed = pressed;
        }

        public void SetSecondaryButton(bool pressed)
        {
            secondaryButtonPressed = pressed;
        }

        public float GetTriggerValue() => triggerValue;
        public Vector2 GetJoystickValue() => joystickValue;
        public bool GetPrimaryButton() => primaryButtonPressed;
        public bool GetSecondaryButton() => secondaryButtonPressed;
    }
}
