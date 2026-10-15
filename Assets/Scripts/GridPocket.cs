using UnityEngine;

namespace Shatterline
{
    // Both authored variants exist in the prefab; precisely one is enabled per run.
    public sealed class GridPocket : MonoBehaviour
    {
        public MinefieldGrid Lower, Upper;
        public int Row;
        public float Center;
        public void Apply(bool firstLow, System.Random random)
        {
            transform.localPosition = new Vector3(Center - .3f + (float)random.NextDouble() * .6f, 0, 0);
            bool low = Row == 0 ? firstLow : !firstLow;
            Lower.gameObject.SetActive(low); Upper.gameObject.SetActive(!low);
        }
    }
}
