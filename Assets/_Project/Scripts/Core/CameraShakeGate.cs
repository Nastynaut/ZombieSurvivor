namespace KnightSurvivors.Core
{
    /// <summary>
    /// TEMPORARY debug switch (camera-jitter investigation). Remove together with CameraShakeDebugToggle
    /// once the camera is confirmed smooth.
    ///
    /// One global yes/no: "may the camera shake right now?". It lives in Core so any future shake code
    /// (the juice PR: hit shake, CinemachineImpulse, ...) can check it without extra references.
    ///
    /// HONEST NOTE: at the time of writing there is NO camera shake code in the project at all, so turning
    /// this off changes nothing. That is useful in itself: if the jitter looks the same with shake ON and
    /// OFF, the jitter is not shake.
    /// </summary>
    public static class CameraShakeGate
    {
        /// <summary>True = shake allowed. Set from FeedbackSettings.cameraShakeEnabled at run start; F1 flips it.</summary>
        public static bool Enabled = true;
    }
}
