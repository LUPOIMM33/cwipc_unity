
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System;
using System.Collections.Generic;

#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    /// <summary>
    /// MonoBehaviour that renders pointclouds with VFX Graph.
    /// </summary>
    public class PointCloudRenderer_VFX : MonoBehaviour
    {
        GraphicsBuffer pointBuffer = null;
        int pointCount = 0;
        [Header("Settings")]
        [Tooltip("Source of pointclouds. Can (and must) be empty if set dynamically through script.")]
        public AbstractPointCloudPreparer pointcloudSource;
        public IPointCloudPreparer preparer;

        [Header("VFX Settings")]
        public GameObject VFX_Elem_prefab;
        public Transform Reference;
        public int VFX_Elem_ChunkSize = 10000;
        public bool limitChunks = false;
        public int Chunklimit = 1;

        [Header("Events")]
        [Tooltip("Event emitted when the first point cloud is displayed")]
        public UnityEvent started;
        [Tooltip("Event emitted when the last point cloud has been displayed")]
        public UnityEvent finished;
        private bool started_emitted = false;
        private bool finished_emitted = false;

        [Header("Introspection (for debugging)")]
        [Tooltip("Renderer temporarily paused by a script")]
        [SerializeField] bool paused = false;

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        private List<GameObject> VFX_elems;

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        // Start is called before the first frame update
        void Start()
        {
            if (started == null)
            {
                started = new UnityEvent();
            }
            if (finished == null)
            {
                finished = new UnityEvent();
            }
            pointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 4);

            if (pointcloudSource != null)
            {
                SetPreparer(pointcloudSource);
            }

            VFX_elems = new List<GameObject>();

#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
        }

        public void PausePlayback(bool _paused)
        {
            paused = _paused;
        }

        public void SetPreparer(IPointCloudPreparer _preparer)
        {
            if (_preparer == null)
            {
                Debug.LogError($"Programmer error: attempt to set null preparer");
            }
            if (preparer != null)
            {
                Debug.LogError($"Programmer error: attempt to set second preparer");
            }
            preparer = _preparer;
        }

        private void Update()
        {
            if (preparer == null) return;
            preparer.Synchronize();
        }

        private void LateUpdate()
        {
            float pointSize = 0;
            if (preparer == null) return;
            if (paused) return;

            bool fresh = preparer.LatchFrame();
            if (fresh)
            {
                if (!started_emitted)
                {
                    started_emitted = true;
                    started.Invoke();
                }

                pointCount = preparer.FillGraphicsBuffer(ref pointBuffer);
                if (pointBuffer == null || !pointBuffer.IsValid())
                {
                    Debug.LogError($"{Name()}: Invalid pointBuffer");
                    return;
                }
                pointSize = preparer.GetPointSize();

                Render(pointBuffer, pointCount, pointSize);
            }
            else
            {
                if (!finished_emitted && preparer.EndOfData())
                {
                    finished_emitted = true;
                    finished.Invoke();
                }
            }
#if VRT_WITH_STATS
            stats.statsUpdate(pointCount, pointSize, preparer.currentTimestamp, preparer.getQueueDuration(), fresh);
#endif
        }

        public void OnDestroy()
        {
            if (pointBuffer != null)
            {
                pointBuffer.Release();
                pointBuffer = null;
            }
        }

    private void Render(GraphicsBuffer dataBuffer, int nPoints, float pointSize)
    {
        int optimalChunksCount;
        if (dataBuffer == null || nPoints == 0)
        {
            optimalChunksCount = 0;
        }
        else
        {
            optimalChunksCount = 1 + nPoints / VFX_Elem_ChunkSize;
        }
        int nChunks = optimalChunksCount;
        if(limitChunks && optimalChunksCount > Chunklimit)
        {
            nChunks = Mathf.Max(0, Chunklimit);
        }
        
        if (VFX_elems.Count < nChunks)
            AddElems(nChunks - VFX_elems.Count);
        if (VFX_elems.Count > nChunks)
            RemoveElems(VFX_elems.Count - nChunks);

        for (int chunkIndex = 0; chunkIndex < nChunks; chunkIndex++)
        {
            VFX_ELemRenderer renderer = VFX_elems[chunkIndex].GetComponent<VFX_ELemRenderer>();
            renderer.PassToVFX(dataBuffer, nPoints, chunkIndex, nChunks, pointSize);
        }
    }

    private void AddElems(int nElems)
    {
        for (int i = 0; i < nElems; i++)
        {
            GameObject newElem = GameObject.Instantiate(VFX_Elem_prefab);
            newElem.transform.parent = Reference;
            newElem.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            newElem.transform.localRotation = Quaternion.identity;
            newElem.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            VFX_elems.Add(newElem);
        }            
    }

    private void RemoveElems(int nElems)
    {
        for (int i = 0; i < nElems; i++)
        {
            Destroy(VFX_elems[0]);
            VFX_elems.Remove(VFX_elems[0]);
        }
    }

    public void EnableRenderer(bool enable)
    {
        foreach(GameObject elem in VFX_elems)
        {
            VFX_ELemRenderer renderer = elem.GetComponent<VFX_ELemRenderer>();
            if (renderer != null)
            {
                renderer.Enable_VFX(enable);
            }
        }
    }

    public void SetPointSize(float pointSize)
    {
        if(VFX_elems != null && VFX_elems.Count > 0)
        {
            foreach (GameObject elem in VFX_elems)
            {
                VFX_ELemRenderer renderer = elem.GetComponent<VFX_ELemRenderer>();
                if (renderer != null)
                {
                    renderer.SetPointSize(pointSize);
                }
            }
        }

        VFX_Elem_prefab.GetComponent<VFX_ELemRenderer>().SetPointSize(pointSize);
    }

    public float GetPointSize()
    {
        if(VFX_elems != null && VFX_elems.Count > 0)
        {
            VFX_ELemRenderer renderer = VFX_elems[0].GetComponent<VFX_ELemRenderer>();
            if (renderer != null)
            {
                return renderer.vfxGraph.GetFloat("PointSize");
            }
        }
        else
        {
            return VFX_Elem_prefab.GetComponent<VFX_ELemRenderer>().vfxGraph.GetFloat("PointSize");
        }
        return 0.0f;
    }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalPointcloudCount = 0;
            double statsTotalDisplayCount = 0;
            double statsTotalPointCount = 0;
            double statsTotalDisplayPointCount = 0;
            double statsTotalPointSize = 0;
            double statsTotalQueueDuration = 0;
            Timedelta statsMinLatency = 0;
            Timedelta statsMaxLatency = 0;

            public void statsUpdate(int pointCount, float pointSize, Timestamp timestamp, Timedelta queueDuration, bool fresh)
            {

                statsTotalDisplayPointCount += pointCount;
                statsTotalDisplayCount += 1;
                if (!fresh)
                {
                    // If this was just a re-display of a previously received pointcloud we don't need the rest of the data.
                    return;
                }
                statsTotalPointcloudCount += 1;
                statsTotalPointCount += pointCount;
                statsTotalPointSize += pointSize;
                statsTotalQueueDuration += queueDuration;

                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                if (timestamp > 0)
                {
                    Timedelta latency = (Timestamp)sinceEpoch.TotalMilliseconds - timestamp;
                    if (latency < statsMinLatency || statsMinLatency == 0) statsMinLatency = latency;
                    if (latency > statsMaxLatency) statsMaxLatency = latency;
                }

                if (ShouldOutput())
                {
                    double factor = statsTotalPointcloudCount == 0 ? 1 : statsTotalPointcloudCount;
                    double display_factor = statsTotalDisplayCount == 0 ? 1 : statsTotalDisplayCount;
                    Output($"fps={statsTotalPointcloudCount / Interval():F2}, latency_ms={statsMinLatency}, latency_max_ms={statsMaxLatency}, fps_display={statsTotalDisplayCount / Interval():F2}, points_per_cloud={(int)(statsTotalPointCount / factor)}, points_per_display={(int)(statsTotalDisplayPointCount / display_factor)}, avg_pointsize={(statsTotalPointSize / factor):G4}, renderer_queue_ms={(int)(statsTotalQueueDuration / factor)}, framenumber={UnityEngine.Time.frameCount},  timestamp={timestamp}");
                    Clear();
                    statsTotalPointcloudCount = 0;
                    statsTotalDisplayCount = 0;
                    statsTotalDisplayPointCount = 0;
                    statsTotalPointCount = 0;
                    statsTotalPointSize = 0;
                    statsTotalQueueDuration = 0;
                    statsMinLatency = 0;
                    statsMaxLatency = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}