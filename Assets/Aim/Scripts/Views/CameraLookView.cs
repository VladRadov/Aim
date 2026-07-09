using UnityEngine;

namespace Aim.Views
{
    public sealed class CameraLookView : MonoBehaviour
    {
        [SerializeField] Transform yawPivot;
        [SerializeField] Transform pitchPivot;

        public Transform YawPivot => yawPivot;
        public Transform PitchPivot => pitchPivot;

        public void SetAimAngles(float yaw, float pitch)
        {
            yawPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
