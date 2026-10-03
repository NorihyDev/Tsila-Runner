using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerRoadSection : MonoBehaviour
    {
        public const float ZoneLength = 288f;
        public static readonly string[] ZoneNames = { "METHOD ISLAND", "MOUNTAIN PASS", "UNDERGROUND" };
        public GameObject[] scenery;
        public int Zone { get; private set; }
        ScenerySpacing[] spacedProps;

        public static int ZoneAt(double distance) => (int)(System.Math.Floor(System.Math.Max(0d, distance) / ZoneLength) % 3d);

        public void SetLocation(double distance)
        {
            Zone = ZoneAt(distance);
            for (int i = 0; i < scenery.Length; i++) scenery[i].SetActive(i == Zone);
            if (spacedProps == null) spacedProps = GetComponentsInChildren<ScenerySpacing>(true);
            long section = (long)System.Math.Floor(distance / RunnerRules.RoadLength);
            foreach (var prop in spacedProps) prop.SetSection(section);
        }
    }
}
