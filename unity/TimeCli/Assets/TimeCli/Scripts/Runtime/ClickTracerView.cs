using UnityEngine;

namespace TimeCli.UnityRuntime
{
    /// <summary>
    /// Clean reconstruction of the original ClickerPistol Tracer component.
    ///
    /// Recovered 1.4.5 behavior:
    /// - 4 line positions: start, Lerp(0.1), Lerp(0.9), end;
    /// - normal lifetime 0.1 s and width 0.15;
    /// - critical lifetime 0.2 s and width 0.5;
    /// - normal and critical color pairs from the serialized Tracer prefab;
    /// - end alpha fades linearly, start alpha fades as endAlpha^2.
    /// </summary>
    public sealed class ClickTracerView : MonoBehaviour
    {
        public const float NormalLifetime = 0.1f;
        public const float CriticalLifetime = 0.2f;
        public const float NormalWidth = 0.15f;
        public const float CriticalWidth = 0.5f;

        private LineRenderer _line;
        private bool _critical;
        private float _lifetime;
        private float _remaining;

        public static ClickTracerView Spawn(
            Vector3 start,
            Vector3 end,
            bool critical)
        {
            var go = new GameObject(
                critical ? "CriticalTracer" : "Tracer");

            var view = go.AddComponent<ClickTracerView>();
            view.Initialize(start, end, critical);
            return view;
        }

        public void Initialize(
            Vector3 start,
            Vector3 end,
            bool critical)
        {
            _critical = critical;
            _lifetime = critical
                ? CriticalLifetime
                : NormalLifetime;
            _remaining = _lifetime;

            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 4;
            _line.useWorldSpace = true;
            _line.startWidth = critical
                ? CriticalWidth
                : NormalWidth;
            _line.endWidth = _line.startWidth;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("TimeCli/BlockVertexColor");

            _line.sharedMaterial = new Material(shader)
            {
                name = "TimeCli_Reconstructed_Tracer"
            };

            _line.SetPosition(0, start);
            _line.SetPosition(1, Vector3.Lerp(start, end, 0.1f));
            _line.SetPosition(2, Vector3.Lerp(start, end, 0.9f));
            _line.SetPosition(3, end);

            ApplyAlpha(1f);
        }

        private void Update()
        {
            _remaining -= Time.deltaTime;

            if (_remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Original FadeOut:
            // endAlpha = Lerp(0, 1, delay / tracerLifetime)
            // startAlpha = endAlpha * endAlpha
            float endAlpha = Mathf.Lerp(
                0f,
                1f,
                _remaining / _lifetime);

            ApplyAlpha(endAlpha);
        }

        private void ApplyAlpha(float endAlpha)
        {
            float startAlpha = endAlpha * endAlpha;

            _line.startColor = GetStartColor(
                _critical,
                startAlpha);
            _line.endColor = GetEndColor(
                _critical,
                endAlpha);
        }

        private static Color GetStartColor(
            bool critical,
            float alpha)
        {
            return critical
                ? new Color(1f, 0f, 0f, alpha)
                : new Color(
                    0f,
                    0.4066853523f,
                    1f,
                    alpha);
        }

        private static Color GetEndColor(
            bool critical,
            float alpha)
        {
            return critical
                ? new Color(
                    1f,
                    0.3931033909f,
                    0f,
                    alpha)
                : new Color(
                    1f,
                    0.9333333969f,
                    0.4980392456f,
                    alpha);
        }
    }
}
