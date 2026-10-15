using UnityEngine;

namespace Shatterline
{
    // Authors the existing illustration as saved sprite regions in the Editor. The source
    // texture stays intact; only the displayed leg pieces swing while walking.
    public sealed class CourierWalkRig : MonoBehaviour
    {
        private const float PixelsPerUnit = 590f;
        [SerializeField] private SpriteRenderer still, torso, nearLeg, farLeg;
        [SerializeField] private Transform torsoPivot, nearPivot, farPivot;
        [SerializeField] private bool ready;
        [SerializeField] private float torsoY;

        private void Awake()
        {
            ready = still != null && torso != null && nearLeg != null && farLeg != null;
            if (ready) SetWalking(false);
        }

#if UNITY_EDITOR
        public void Initialize(Transform core, SpriteRenderer original, Texture2D texture)
        {
            still = original;
            if (texture == null || original == null) return;
            float left = -texture.width * .5f / PixelsPerUnit;
            float bottom = -texture.height * .06f / PixelsPerUnit;
            // Use the dimensions Unity actually imported: a platform may resize
            // this texture, so hardcoded source-image pixel rectangles can fail.
            int splitX = Mathf.Clamp(Mathf.RoundToInt(texture.width * 640f / 1222f), 1, texture.width - 1);
            int splitY = Mathf.Clamp(Mathf.RoundToInt(texture.height * 630f / 1287f), 1, texture.height - 1);
            int legTop = Mathf.Clamp(Mathf.RoundToInt(texture.height * 650f / 1287f), splitY, texture.height);
            Vector2 torsoHip = new Vector2(texture.width * .5f, splitY);
            Vector2 farHip = new Vector2(texture.width * 748f / 1222f, texture.height * 588f / 1287f);
            Vector2 nearHip = new Vector2(texture.width * 486f / 1222f, texture.height * 588f / 1287f);
            try
            {
                // A small overlap at the hips avoids opening a gap during each stride.
                torso = Piece(core, texture, "Walking torso",
                    new Rect(0, splitY, texture.width, texture.height - splitY),
                    torsoHip, left, bottom, out torsoPivot);
                farLeg = Piece(core, texture, "Far walking leg",
                    new Rect(splitX, 0, texture.width - splitX, legTop),
                    farHip, left, bottom, out farPivot);
                nearLeg = Piece(core, texture, "Near walking leg",
                    new Rect(0, 0, splitX, legTop),
                    nearHip, left, bottom, out nearPivot);
                farLeg.sortingOrder = 7;
                nearLeg.sortingOrder = 8;
                torso.sortingOrder = 9;
                torsoY = torsoPivot.localPosition.y;
                ready = true;
                SetWalking(false);
            }
            catch (System.Exception error)
            {
                // Cosmetic animation must never prevent a level from loading.
                Debug.LogWarning("Courier walking rig unavailable; showing the full sprite: " + error.Message);
                if (torsoPivot != null) Destroy(torsoPivot.gameObject);
                if (farPivot != null) Destroy(farPivot.gameObject);
                if (nearPivot != null) Destroy(nearPivot.gameObject);
                still.enabled = true;
                ready = false;
            }
        }

        private static SpriteRenderer Piece(Transform parent, Texture2D texture, string name,
            Rect region, Vector2 pivotPixel, float left, float bottom, out Transform pivot)
        {
            var obj = new GameObject(name);
            pivot = obj.transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(left + pivotPixel.x / PixelsPerUnit,
                bottom + pivotPixel.y / PixelsPerUnit, -.85f);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(texture, region,
                new Vector2((pivotPixel.x - region.x) / region.width,
                    (pivotPixel.y - region.y) / region.height), PixelsPerUnit);
            return renderer;
        }

#endif
        public void SetWalking(bool value)
        {
            if (!ready) return;
            still.enabled = !value;
            torso.enabled = nearLeg.enabled = farLeg.enabled = value;
            if (!value)
            {
                nearPivot.localRotation = farPivot.localRotation = Quaternion.identity;
                torsoPivot.localPosition = new Vector3(torsoPivot.localPosition.x, torsoY, -.85f);
            }
        }

        // The Animator drives the saved leg curves. This component only selects
        // the full illustration or its rigged walking pieces.
    }
}
