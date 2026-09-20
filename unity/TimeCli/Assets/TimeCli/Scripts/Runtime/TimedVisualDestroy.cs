using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public sealed class TimedVisualDestroy : MonoBehaviour
    {
        public float Lifetime { get; set; } = 0.5f;

        private void Update()
        {
            Lifetime -= Time.deltaTime;

            if (Lifetime <= 0f)
                Destroy(gameObject);
        }
    }
}
