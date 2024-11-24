
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System;
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
        public VisualEffect vfxGraph; // Reference to your VFX Graph
        public PointCloud_VFX pc_VFX;

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

        private Vector4[] positions = null;
        private Color[] colors = null;


        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

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

            if (vfxGraph == null)
            {
                Debug.LogError("PointCloudSource or VFX Graph is not assigned.");
            }
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

                pc_VFX.PassToVFX(pointBuffer, pointCount, pointSize);
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

/*
 *  Asynchron Test
 * 
 * 
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cwipc
{
    /// <summary>
    /// MonoBehaviour that renders pointclouds with VFX Graph.
    /// </summary>
    public class PointCloudRenderer_VFX : MonoBehaviour
    {
        ComputeBuffer pointBuffer = null;
        int pointCount = 0;
        [Header("Settings")]
        [Tooltip("Source of pointclouds. Can (and must) be empty if set dynamically through script.")]
        public AbstractPointCloudPreparer pointcloudSource;
        public IPointCloudPreparer preparer;

        [Header("VFX Settings")]
        public VisualEffect vfxGraph; // Reference to your VFX Graph
        public PointCloud_VFX pc_VFX;

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

        private Vector3[] positions;
        private Color[] colors;
        byte[] bufferData;

        Thread ThreadWorker;
        private readonly object lockObject = new object();
        private CancellationTokenSource cancellationTokenSource;
        private bool newFrameAvailable = false;


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
            pointBuffer = new ComputeBuffer(1, sizeof(float) * 4);

            if (pointcloudSource != null)
            {
                SetPreparer(pointcloudSource);
            }

            if (vfxGraph == null)
            {
                Debug.LogError("PointCloudSource or VFX Graph is not assigned.");
            }

            if (ThreadWorker == null || !ThreadWorker.IsAlive)
            {
                cancellationTokenSource = new CancellationTokenSource();
                ThreadWorker = new Thread(() => DoWork(cancellationTokenSource.Token));
                ThreadWorker.IsBackground = true;
                ThreadWorker.Start();
            }
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
            if (paused) return;

            lock (lockObject)
            {
                preparer.Synchronize();
                pointCount = preparer.GetComputeBuffer(ref pointBuffer);

                if (newFrameAvailable)
                {
                    newFrameAvailable = false;
                    Debug.Log("Pass to VFX : " + pointCount + " points");
                    
                    pc_VFX.PassToVFX(positions, colors);
                    
                    if (!started_emitted)
                    {
                        started_emitted = true;
                        started.Invoke();
                    }
                }
                else
                {
                    if (!finished_emitted && preparer.EndOfData())
                    {
                        finished_emitted = true;
                        finished.Invoke();
                    }
                }
            }
        }

        void DoWork(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        bool fresh = preparer.LatchFrame();

                        if (fresh)
                        {
                            pointCount = ExtractDataFromComputeBuffer(pointBuffer, ref positions, ref colors);
                            newFrameAvailable = true;
                        }
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DoWork error: {ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }


        void Cleanup()
        {
            cancellationTokenSource?.Dispose(); // Clean up the CancellationTokenSource
            cancellationTokenSource = null;
        }

        public void OnDestroy()
        {
            cancellationTokenSource?.Cancel(); // Request cancellation
            ThreadWorker?.Join(); // Wait for the thread to finish
            Cleanup();

            if (pointBuffer != null)
            {
                pointBuffer.Release();
                pointBuffer = null;
            }
        }

        private int ExtractDataFromComputeBuffer(ComputeBuffer computeBuffer, ref Vector3[] pointPositions, ref Color[] pointColors)
        {
            if (computeBuffer == null || computeBuffer.count == 0)
            {
                pointPositions = new Vector3[0];
                pointColors = new Color[0];
                return 0;
            }

            // Get the point count from the compute buffer
            int nPoints = computeBuffer.count;

            // Ensure arrays are correctly sized
            if (pointPositions == null || pointPositions.Length != nPoints)
            {
                pointPositions = new Vector3[nPoints];
            }
            if (pointColors == null || pointColors.Length != nPoints)
            {
                pointColors = new Color[nPoints];
            }

            // Allocate a buffer to retrieve data from the compute buffer
            byte[] bufferData = new byte[nPoints * sizeof(float) * 4];

            // Get the data from the compute buffer
            computeBuffer.GetData(bufferData);

            const int sizeofPoint = sizeof(float) * 4; // x, y, z, and packed color

            // Extract points and colors from the buffer
            for (int i = 0; i < nPoints; i++)
            {
                // Extract position (x, y, z) from the buffer
                float x = BitConverter.ToSingle(bufferData, i * sizeofPoint);
                float y = BitConverter.ToSingle(bufferData, i * sizeofPoint + sizeof(float));
                float z = BitConverter.ToSingle(bufferData, i * sizeofPoint + 2 * sizeof(float));

                // Extract packed color data (RGBA)
                uint packedColor = BitConverter.ToUInt32(bufferData, i * sizeofPoint + 3 * sizeof(float));

                // Populate the position array
                pointPositions[i] = new Vector3(x, y, z);

                // Decode and populate the color array
                float r = ((packedColor >> 16) & 0xFF) / 255.0f;
                float g = ((packedColor >> 8) & 0xFF) / 255.0f;
                float b = (packedColor & 0xFF) / 255.0f;
                pointColors[i] = new Color(r, g, b, 1.0f);
            }

            return nPoints;
        }
        private int ExtractDataFromBytes(byte[] BytesData, ref Vector3[] pointPositions, ref Color[] pointColors)
        {
            if (BytesData == null)
            {
                pointPositions = new Vector3[0];
                pointColors = new Color[0];
                return 0;
            }

            // Get the point count from the compute buffer
            int nPoints = BytesData.Length / (sizeof(float) * 4);

            // Ensure arrays are correctly sized
            if (pointPositions == null || pointPositions.Length != nPoints)
            {
                pointPositions = new Vector3[nPoints];
            }
            if (pointColors == null || pointColors.Length != nPoints)
            {
                pointColors = new Color[nPoints];
            }

            const int sizeofPoint = sizeof(float) * 4; // x, y, z, and packed color

            // Extract points and colors from the buffer
            for (int i = 0; i < nPoints; i++)
            {
                // Extract position (x, y, z) from the buffer
                float x = BitConverter.ToSingle(BytesData, i * sizeofPoint);
                float y = BitConverter.ToSingle(BytesData, i * sizeofPoint + sizeof(float));
                float z = BitConverter.ToSingle(BytesData, i * sizeofPoint + 2 * sizeof(float));

                // Extract packed color data (RGBA)
                uint packedColor = BitConverter.ToUInt32(BytesData, i * sizeofPoint + 3 * sizeof(float));

                // Populate the position array
                pointPositions[i] = new Vector3(x, y, z);

                // Decode and populate the color array
                float r = ((packedColor >> 16) & 0xFF) / 255.0f;
                float g = ((packedColor >> 8) & 0xFF) / 255.0f;
                float b = (packedColor & 0xFF) / 255.0f;
                pointColors[i] = new Color(r, g, b, 1.0f);
            }

            return nPoints;
        }

    }
}

*/