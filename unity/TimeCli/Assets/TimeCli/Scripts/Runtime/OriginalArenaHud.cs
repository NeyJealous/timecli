using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Lightweight reconstruction of the original ArenaDisplay, UIWaveHP and
    /// WidgetGold presentation. It intentionally uses IMGUI for the first
    /// modern-port milestone so the recovered layout does not depend on an
    /// additional UI package or serialized prefab.
    /// </summary>
    public sealed class OriginalArenaHud : MonoBehaviour
    {
        private const float ReferenceWidth = 1000f;
        private const float ReferenceHeight = 600f;

        [SerializeField]
        private TimeCliBootstrap bootstrap;

        [SerializeField]
        private ArenaRuntimeController arena;

        private Texture2D _pixel;

        private void Start()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<TimeCliBootstrap>();

            if (arena == null)
                arena = FindFirstObjectByType<ArenaRuntimeController>();

            _pixel = new Texture2D(1, 1);
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }

        private void OnGUI()
        {
            if (bootstrap == null ||
                arena == null ||
                !bootstrap.IsReady)
            {
                return;
            }

            var snapshot = ArenaHudMath.Build(
                bootstrap.Game,
                bootstrap.Arena.CurrentEnemy?.Model,
                Time.timeAsDouble);

            float scale = Mathf.Min(
                Screen.width / ReferenceWidth,
                Screen.height / ReferenceHeight);

            float offsetX =
                (Screen.width - ReferenceWidth * scale) * 0.5f;

            float offsetY =
                (Screen.height - ReferenceHeight * scale) * 0.5f;

            DrawWaveHp(snapshot, scale, offsetX, offsetY);
            DrawArenaDisplay(snapshot, scale, offsetX, offsetY);
            DrawGold(snapshot, scale, offsetX, offsetY);
        }

        private void DrawWaveHp(
            ArenaHudSnapshot snapshot,
            float scale,
            float offsetX,
            float offsetY)
        {
            // CanvasWaveHP/Container is authored at top-center with
            // anchoredPosition (0,-95), size 200x60 on a 1000x600 reference.
            Rect hpTextRect = R(
                405f,
                108f,
                190f,
                34f,
                scale,
                offsetX,
                offsetY);

            GUI.Label(
                hpTextRect,
                snapshot.WaveHpText,
                TextStyle(
                    32,
                    new Color(1f, 0f, 0f, 1f),
                    TextAnchor.MiddleLeft,
                    scale));

            Rect hpTrack = R(
                405f,
                147f,
                190f,
                5f,
                scale,
                offsetX,
                offsetY);

            DrawRect(
                hpTrack,
                new Color(
                    0.16f,
                    0.03f,
                    0.03f,
                    0.75f));

            Rect hpFill = hpTrack;
            hpFill.width *=
                (float)snapshot.HpNormalized;

            DrawRect(
                hpFill,
                new Color(
                    1f,
                    0f,
                    0f,
                    1f));

            if (!snapshot.IsBossWave)
                return;

            Rect bossTrack = R(
                405f,
                157f,
                190f,
                5f,
                scale,
                offsetX,
                offsetY);

            DrawRect(
                bossTrack,
                new Color(
                    0.04f,
                    0.13f,
                    0.24f,
                    0.75f));

            Rect bossFill = bossTrack;
            bossFill.width *=
                (float)snapshot.BossTimeNormalized;

            DrawRect(
                bossFill,
                new Color(
                    0.498039f,
                    0.702179f,
                    1f,
                    1f));

            GUI.Label(
                R(
                    405f,
                    163f,
                    190f,
                    24f,
                    scale,
                    offsetX,
                    offsetY),
                snapshot.BossTimeText,
                TextStyle(
                    18,
                    new Color(
                        0.498039f,
                        0.702179f,
                        1f,
                        1f),
                    TextAnchor.MiddleLeft,
                    scale));
        }

        private void DrawArenaDisplay(
            ArenaHudSnapshot snapshot,
            float scale,
            float offsetX,
            float offsetY)
        {
            Color textColor =
                new(
                    0f,
                    0f,
                    0f,
                    0.870588f);

            if (snapshot.IsBossWave)
            {
                GUI.Label(
                    R(
                        450f,
                        280f,
                        100f,
                        42f,
                        scale,
                        offsetX,
                        offsetY),
                    "Boss",
                    TextStyle(
                        36,
                        textColor,
                        TextAnchor.MiddleCenter,
                        scale));

                GUI.Label(
                    R(
                        450f,
                        320f,
                        100f,
                        38f,
                        scale,
                        offsetX,
                        offsetY),
                    snapshot.BossTimeText + "s",
                    TextStyle(
                        30,
                        textColor,
                        TextAnchor.MiddleCenter,
                        scale));

                return;
            }

            GUI.Label(
                R(
                    450f,
                    282f,
                    100f,
                    34f,
                    scale,
                    offsetX,
                    offsetY),
                "Remaining",
                TextStyle(
                    24,
                    textColor,
                    TextAnchor.MiddleCenter,
                    scale));

            GUI.Label(
                R(
                    435f,
                    320f,
                    130f,
                    50f,
                    scale,
                    offsetX,
                    offsetY),
                snapshot.ShowInfinity
                    ? "∞"
                    : snapshot.ArenaRemainingText,
                TextStyle(
                    snapshot.ShowInfinity
                        ? 54
                        : 48,
                    textColor,
                    TextAnchor.MiddleCenter,
                    scale));
        }

        private void DrawGold(
            ArenaHudSnapshot snapshot,
            float scale,
            float offsetX,
            float offsetY)
        {
            // WidgetGold is world-space in 1.4.5, but visually anchors to the
            // bottom-right via Camera.ViewportToWorldPoint(0.9,0.1,23.2).
            // This screen-space projection preserves that location while the
            // remaining WidgetGold mesh/spinner presentation is rebuilt.
            GUI.Label(
                R(
                    795f,
                    520f,
                    180f,
                    42f,
                    scale,
                    offsetX,
                    offsetY),
                snapshot.GoldText,
                TextStyle(
                    32,
                    new Color(
                        1f,
                        0.902507f,
                        0f,
                        1f),
                    TextAnchor.MiddleCenter,
                    scale));

            string cubes =
                "TC " +
                snapshot.TimeCubes +
                (snapshot.PendingTimeCubes > 0
                    ? " (+" +
                      snapshot.PendingTimeCubes +
                      ")"
                    : string.Empty) +
                "   WC " +
                snapshot.WeaponCubes;

            GUI.Label(
                R(
                    760f,
                    562f,
                    220f,
                    24f,
                    scale,
                    offsetX,
                    offsetY),
                cubes,
                TextStyle(
                    14,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    scale));
        }

        private void DrawRect(
            Rect rect,
            Color color)
        {
            if (_pixel == null)
                return;

            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _pixel);
            GUI.color = previous;
        }

        private static GUIStyle TextStyle(
            int fontSize,
            Color color,
            TextAnchor alignment,
            float scale)
        {
            return new GUIStyle(
                GUI.skin.label)
            {
                fontSize =
                    Mathf.Max(
                        1,
                        (int)(fontSize * scale)),
                alignment = alignment,
                normal =
                {
                    textColor = color
                }
            };
        }

        private static Rect R(
            float x,
            float y,
            float width,
            float height,
            float scale,
            float offsetX,
            float offsetY)
        {
            return new Rect(
                offsetX + x * scale,
                offsetY + y * scale,
                width * scale,
                height * scale);
        }
    }
}
