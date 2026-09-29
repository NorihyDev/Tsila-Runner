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
        [System.NonSerialized] RunnerItem[,] items;
        System.Random random;
        float nextRow;
        int safeLane;
        public int ActiveItemCount { get; private set; }

        public void Initialize()
        {
            if (roads != null && roads.Length == RunnerRules.RoadCount && roads[0] != null && items != null) return;
            // Unity's fast Enter Play Mode can retain managed fields with destroyed scene objects.
            // Recover once here; normal restarts simply reuse the existing valid pool.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            roads = new Transform[RunnerRules.RoadCount];
            for (int i = 0; i < roads.Length; i++)
                roads[i] = Instantiate(roadPrefab, transform).transform;
            items = new RunnerItem[4, RunnerRules.PoolPerKind];
            for (int kind = 0; kind < 4; kind++)
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
            for (int i = 0; i < roads.Length; i++)
                roads[i].position = new Vector3(0f, 0f, (i - 1) * RunnerRules.RoadLength);
            for (int kind = 0; kind < 4; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++) items[kind, i].Release();
            FillAhead();
        }

        public void Simulate(float travel, Bounds previousPlayer, Bounds currentPlayer)
        {
            for (int i = 0; i < roads.Length; i++)
            {
                Vector3 p = roads[i].position - Vector3.forward * travel;
                if (p.z < -RunnerRules.RoadLength * 1.5f) p.z += roads.Length * RunnerRules.RoadLength;
                roads[i].position = p;
            }
            // Obstacles before coins, so a fatal contact cannot also award a coin.
            for (int kind = 0; kind < 4; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                {
                    RunnerItem item = items[kind, i];
                    if (!item.InUse) continue;
                    bool hit = RunnerRules.SweptOverlap(previousPlayer, currentPlayer, item.HitBounds, travel);
                    item.transform.position -= Vector3.forward * travel;
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
            for (int kind = 0; kind < 3; kind++)
                for (int i = 0; i < RunnerRules.PoolPerKind; i++)
                    if (items[kind, i].InUse && items[kind, i].HitBounds.Intersects(bounds)) return true;
            return false;
        }

        void FillAhead()
        {
            while (nextRow <= RunnerRules.Horizon)
            {
                safeLane = RunnerRules.NextSafeLane(safeLane, random);
                for (int lane = 0; lane < 3; lane++)
                    if (lane != safeLane)
                        Place((RunnerItemKind)random.Next(0, 3), lane, nextRow);
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
