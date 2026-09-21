using UnityEngine;
using UnityEngine.InputSystem;

namespace Aim.Services
{
    public static class MobileDevice
    {
        public static bool IsHandheld()
        {
            if (Application.isMobilePlatform)
                return true;

            if (SystemInfo.deviceType == DeviceType.Handheld)
                return true;

            var os = SystemInfo.operatingSystem;
            if (!string.IsNullOrEmpty(os))
            {
                if (ContainsIgnoreCase(os, "Android"))
                    return true;
                if (ContainsIgnoreCase(os, "iPhone"))
                    return true;
                if (ContainsIgnoreCase(os, "iPad"))
                    return true;
                if (ContainsIgnoreCase(os, "iOS"))
                    return true;
            }

            return false;
        }

        public static bool HasTouchThisFrame()
        {
            var touch = Touchscreen.current;
            if (touch == null)
                return false;

            if (touch.primaryTouch.press.isPressed)
                return true;

            var touches = touch.touches;
            for (var i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.isPressed)
                    return true;
            }

            return false;
        }

        static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
