using UnityEngine;

namespace Shatterline
{
    // A bounded offset of an already-authored object; never generates terrain at runtime.
    public sealed class RouteVariation : MonoBehaviour
    {
        public Vector3 Anchor;
        public Vector2 HorizontalRange, VerticalRange;
        public void Apply(System.Random random)
        {
            transform.localPosition = Anchor + new Vector3(
                HorizontalRange.x + (float)random.NextDouble() * (HorizontalRange.y - HorizontalRange.x),
                VerticalRange.x + (float)random.NextDouble() * (VerticalRange.y - VerticalRange.x), 0);
        }
    }
}
