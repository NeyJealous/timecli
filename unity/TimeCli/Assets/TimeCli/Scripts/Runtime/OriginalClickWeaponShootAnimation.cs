using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Shoot/recoil curves decoded from the canonical 1.4.5 AnimationClips.
    ///
    /// Pistol: ClickerPistolShoot (120 Hz, top1 position).
    /// Cannon: ClickCannonFire (60 Hz, model root position + constant rotation).
    /// Launcher: RocketLauncher (60 Hz, model root position + quaternion).
    /// </summary>
    internal static class OriginalClickWeaponShootAnimation
    {
        internal const float PistolDuration = 0.08333333582f;
        internal const float CannonDuration = 0.08333333582f;
        internal const float LauncherDuration = 0.1666666716f;

        internal static readonly AnimationCurve PistolSlideY =
            new(
                new Keyframe(0.000000000f, 0.1410000026f, -0.004658460617f, -0.004658460617f),
                new Keyframe(0.08333333582f, 0.1406117976f, -0.004658460201f, 0.000000000f));

        internal static readonly AnimationCurve PistolSlideZ =
            new(
                new Keyframe(0.000000000f, -0.06100000069f, 0.7654053569f, 0.7654053569f),
                new Keyframe(0.08333333582f, 0.002783778356f, 0.7654052633f, 0.000000000f));

        internal static readonly AnimationCurve CannonZ =
            new(
                new Keyframe(0.000000000f, -0.05000000075f, 0.5999956727f, 0.5999956727f),
                new Keyframe(0.08333333582f, -3.576278687e-7f, 0.5999955744f, 0.000000000f));

        internal static readonly AnimationCurve LauncherY =
            new(
                new Keyframe(0.000000000f, 0.000000000f, -0.3540000021f, -0.3540000021f),
                new Keyframe(0.1666666716f, -0.05900000036f, -0.3539999705f, 0.000000000f));

        internal static readonly AnimationCurve LauncherZ =
            new(
                new Keyframe(0.000000000f, -0.05999999866f, 1.500000000f, 1.500000000f),
                new Keyframe(0.1666666716f, 0.1899999976f, 1.499999799f, 0.000000000f));

        internal static readonly AnimationCurve LauncherRotationX =
            new(
                new Keyframe(0.000000000f, -0.06734322011f, 0.4030108154f, 0.4030108154f),
                new Keyframe(0.01666666754f, -0.06062637269f, 0.4031763564f, 0.4031763375f),
                new Keyframe(0.03333333507f, -0.05390400812f, 0.4034890144f, 0.4034890234f),
                new Keyframe(0.05000000447f, -0.04717673734f, 0.4037648131f, 0.4037648439f),
                new Keyframe(0.06666667014f, -0.04044517875f, 0.4040039680f, 0.4040039480f),
                new Keyframe(0.08333333582f, -0.03370993957f, 0.4042064264f, 0.4042063951f),
                new Keyframe(0.1000000015f, -0.02697163261f, 0.4043720603f, 0.4043720365f),
                new Keyframe(0.1166666672f, -0.02023087256f, 0.4045007766f, 0.4045007825f),
                new Keyframe(0.1333333403f, -0.01348827034f, 0.4045928262f, 0.4045928419f),
                new Keyframe(0.1500000060f, -0.006744442042f, 0.4046480783f, 0.4046481252f),
                new Keyframe(0.1666666716f, 0.000000000f, 0.4046665930f, 0.000000000f));

        internal static readonly AnimationCurve LauncherRotationY =
            new(
                new Keyframe(0.000000000f, 0.7038927078f, 0.03661393747f, 0.03661393747f),
                new Keyframe(0.01666666754f, 0.7045029402f, 0.03469705769f, 0.03469705582f),
                new Keyframe(0.03333333507f, 0.7050492764f, 0.03085076281f, 0.03085076436f),
                new Keyframe(0.05000000447f, 0.7055312991f, 0.02699732247f, 0.02699732594f),
                new Keyframe(0.06666667014f, 0.7059491873f, 0.02314567692f, 0.02314567752f),
                new Keyframe(0.08333333582f, 0.7063028216f, 0.01929223545f, 0.01929223724f),
                new Keyframe(0.1000000015f, 0.7065922618f, 0.01543700808f, 0.01543700881f),
                new Keyframe(0.1166666672f, 0.7068173885f, 0.01157641368f, 0.01157641225f),
                new Keyframe(0.1333333403f, 0.7069781423f, 0.007717608258f, 0.007717607543f),
                new Keyframe(0.1500000060f, 0.7070746422f, 0.003860592397f, 0.003860593075f),
                new Keyframe(0.1666666716f, 0.7071068287f, 0.001931190389f, 0.000000000f));

        internal static readonly AnimationCurve LauncherRotationZ =
            new(
                new Keyframe(0.000000000f, -0.05590956286f, 0.3348581195f, 0.3348581195f),
                new Keyframe(0.01666666754f, -0.05032859370f, 0.3349527882f, 0.3349527717f),
                new Keyframe(0.03333333507f, -0.04474446923f, 0.3351314773f, 0.3351314664f),
                new Keyframe(0.05000000447f, -0.03915754333f, 0.3352890434f, 0.3352890611f),
                new Keyframe(0.06666667014f, -0.03356816620f, 0.3354256678f, 0.3354256749f),
                new Keyframe(0.08333333582f, -0.02797668800f, 0.3355414163f, 0.3355413973f),
                new Keyframe(0.1000000015f, -0.02238345332f, 0.3356361393f, 0.3356361091f),
                new Keyframe(0.1166666672f, -0.01678881794f, 0.3357097399f, 0.3357097208f),
                new Keyframe(0.1333333403f, -0.01119312737f, 0.3357623657f, 0.3357622921f),
                new Keyframe(0.1500000060f, -0.005596739706f, 0.3357938793f, 0.3357938528f),
                new Keyframe(0.1666666716f, 0.000000000f, 0.3358043791f, 0.000000000f));

        internal static readonly AnimationCurve LauncherRotationW =
            new(
                new Keyframe(0.000000000f, 0.7048928738f, 0.02522706799f, 0.02522706799f),
                new Keyframe(0.01666666754f, 0.7053133249f, 0.02390384657f, 0.02390384488f),
                new Keyframe(0.03333333507f, 0.7056896687f, 0.02124845821f, 0.02124845609f),
                new Keyframe(0.05000000447f, 0.7060216069f, 0.01859307375f, 0.01859307289f),
                new Keyframe(0.06666667014f, 0.7063094378f, 0.01594305352f, 0.01594305225f),
                new Keyframe(0.08333333582f, 0.7065530419f, 0.01328766587f, 0.01328766439f),
                new Keyframe(0.1000000015f, 0.7067523599f, 0.01063048872f, 0.01063048933f),
                new Keyframe(0.1166666672f, 0.7069073915f, 0.007971523300f, 0.007971524261f),
                new Keyframe(0.1333333403f, 0.7070180774f, 0.005314348638f, 0.005314349197f),
                new Keyframe(0.1500000060f, 0.7070845366f, 0.002658963217f, 0.002658963436f),
                new Keyframe(0.1666666716f, 0.7071067095f, 0.001330375534f, 0.000000000f));

        internal static readonly Quaternion CannonRotation =
            new(
                -0.4999999702f,
                -0.4999999702f,
                -0.5000000596f,
                0.4999999106f);

        internal static Vector3 EvaluatePistolSlide(float time) =>
            new(
                0f,
                PistolSlideY.Evaluate(time),
                PistolSlideZ.Evaluate(time));

        internal static Vector3 EvaluateCannon(float time) =>
            new(
                0f,
                OriginalClickWeaponPresentation.CannonModelPosition.y,
                CannonZ.Evaluate(time));

        internal static Vector3 EvaluateLauncherPosition(float time) =>
            new(
                0f,
                LauncherY.Evaluate(time),
                LauncherZ.Evaluate(time));

        internal static Quaternion EvaluateLauncherRotation(float time) =>
            new(
                LauncherRotationX.Evaluate(time),
                LauncherRotationY.Evaluate(time),
                LauncherRotationZ.Evaluate(time),
                LauncherRotationW.Evaluate(time));
    }
}
