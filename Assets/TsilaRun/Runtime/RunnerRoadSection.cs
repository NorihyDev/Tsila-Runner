using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerRoadSection : MonoBehaviour
    {
        public const float ZoneLength = 288f;
        public static readonly string[] ZoneNames = { "ÎLE METHOD", "COL DES MONTAGNES", "SOUTERRAIN" };
        public GameObject[] scenery;
        public int Zone { get; private set; }

        public static int ZoneAt(double distance) => (int)(System.Math.Floor(System.Math.Max(0d, distance) / ZoneLength) % 3d);

        public void SetLocation(double distance)
        {
            Zone = ZoneAt(distance);
            for (int i = 0; i < scenery.Length; i++) scenery[i].SetActive(i == Zone);
        }
    }
}
