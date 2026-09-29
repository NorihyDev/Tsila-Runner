using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerWorld : MonoBehaviour
    {
        public GameObject roadPrefab;
        public RunnerItem[] itemPrefabs;
        public RunnerPlayer player;
        public RunnerGame game;
        [System.NonSerialized] Transform[] roads;
        [System.NonSerialized] RunnerRoadSection[] sections;
        [System.NonSerialized] RunnerItem[,] items;
        System.Random random;
        float nextRow;
        int safeLane;
        double travelled;
        public int ActiveItemCount { get; private set; }

        public void Initialize()
        {
            if (roads != null && roads.Length == RunnerRules.RoadCount && roads[0] != null && items != null && sections != null) return;
            // Unity's fast Enter Play Mode can retain managed fields with destroyed scene objects.
            // Recover once here; normal restarts simply reuse the existing valid pool.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            roads = new Transform[RunnerRules.RoadCount];
            sections = new RunnerRoadSection[roads.Length];
            for (int i = 0; i < roads.Length; i++)
            {
                roads[i] = Instantiate(roadPrefab, transform).transform;
                sections[i] = roads[i].GetComponent<RunnerRoadSection>();
            }
            items = new RunnerItem[RunnerRules.ItemKindCount, RunnerRules.PoolPerKind];
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                {
                    items[kind, i] = Instantiate(itemPrefabs[kind], transform);
                    items[kind, i].Release();
                }
        }

        public void ResetWorld(int seed)
        {
            Initialize();
            random = new System.Random(seed);
            safeLane = 1;
            nextRow = RunnerRules.FirstRow;
            ActiveItemCount = 0;
            travelled = 0d;
            for (int i = 0; i < roads.Length; i++)
            {
                roads[i].position = new Vector3(0f, 0f, (i - 1) * RunnerRules.RoadLength);
                if (sections[i] != null) sections[i].SetLocation(roads[i].position.z);
            }
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++) items[kind, i].Release();
            FillAhead();
        }

        public void Simulate(float travel, Bounds previousPlayer, Bounds currentPlayer)
        {
            travelled += travel;
            for (int i = 0; i < roads.Length; i++)
            {
                Vector3 p = roads[i].position - Vector3.forward * travel;
                if (p.z < -RunnerRules.RoadLength * 1.5f)
                {
                    p.z += roads.Length * RunnerRules.RoadLength;
                    if (sections[i] != null) sections[i].SetLocation(travelled + p.z);
                }
                roads[i].position = p;
            }
            // Obstacles before coins, so a fatal contact cannot also award a coin.
            for (int pass = 0; pass < RunnerRules.ItemKindCount; pass++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                {
                    int kind = pass == 3 ? 4 : pass == 4 ? 3 : pass; // All obstacles before coins.
                    RunnerItem item = items[kind, i];
                    if (!item.InUse) continue;
                    float itemTravel = item.TravelThisTick(travel, travel / Mathf.Max(RunnerRules.StartSpeed, game.Speed));
                    bool hit = RunnerRules.SweptOverlap(previousPlayer, currentPlayer, item.HitBounds, itemTravel);
                    item.transform.position -= Vector3.forward * itemTravel;
                    if (hit && game.State == RunnerGame.RunState.Running)
                    {
                        if (item.kind == RunnerItemKind.Coin)
                        {
                            item.Release();
                            ActiveItemCount--;
                            game.CollectCoin();
                        }
                        else game.EndRun();
                    }
                    if (item.InUse && item.transform.position.z < -12f)
                    {
                        item.Release();
                        ActiveItemCount--;
                    }
                }
            nextRow -= travel;
            FillAhead();
        }

        public bool IntersectsObstacle(Bounds bounds)
        {
            if (items == null) return false;
            for (int kind = 0; kind < RunnerRules.ItemKindCount; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                    if (kind != (int)RunnerItemKind.Coin && items[kind, i].InUse && items[kind, i].HitBounds.Intersects(bounds)) return true;
            return false;
        }

        void FillAhead()
        {
            while (nextRow <= RunnerRules.Horizon)
            {
                safeLane = RunnerRules.NextSafeLane(safeLane, random);
                for (int lane = 0; lane < 3; lane++)
                    if (lane != safeLane)
                    {
                        int kind = random.Next(0, 4);
                        Place(kind == 3 ? RunnerItemKind.RunningPerson : (RunnerItemKind)kind, lane, nextRow);
                    }
                // Coins only occupy the completely clear lane, before and after the row.
                for (int i = 0; i < 4; i++) Place(RunnerItemKind.Coin, safeLane, nextRow - 8f + i * 4f);
                nextRow += RunnerRules.RowSpacing;
            }
        }

        void Place(RunnerItemKind kind, int lane, float z)
        {
            for (int i = 0; i < RunnerRules.PoolPerKind; i++)
            {
                RunnerItem item = items[(int)kind, i];
                if (item.InUse) continue;
                item.Place((lane - 1) * RunnerRules.LaneWidth, z);
                ActiveItemCount++;
                return;
            }
            // Bounded pools: if tuning ever exhausts one, omit the item rather than allocate.
        }
    }
}
