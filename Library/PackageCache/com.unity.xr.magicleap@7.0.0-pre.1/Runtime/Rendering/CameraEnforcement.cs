using System;

namespace UnityEngine.XR.MagicLeap.Rendering
{
    /// <summary>
    /// Static class for enforcing camera restrictions.
    /// </summary>
    internal static class CameraEnforcement
    {
        internal static void EnforceCameraProperties()
        {
            if (Camera.main != null)
            {
                // If the near clipping distance from MLGraphicsAPI is greater,
                // Set the main camera near clipping plane to this distance and notify the user.
                if (RenderingSettings.nearClipDistance - Camera.main.nearClipPlane > 0.001f)
                {
                    Debug.LogWarning("The Camera\'s near clipping plane was set to a value that's beneath hardware limitations. Setting value to:" + RenderingSettings.nearClipDistance.ToString());
                    Camera.main.nearClipPlane = RenderingSettings.nearClipDistance;
                }
            }
            else
            {
                Debug.LogWarning("Main camera is null. Skipping CameraEnforcement restrictions.");
            }
        }
    }
}