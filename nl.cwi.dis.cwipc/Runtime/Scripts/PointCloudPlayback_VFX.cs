using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Cwipc
{
    /// <summary>
    /// Play a sequence of prerecorded pointclouds (think: volumetric video)
    /// </summary>
    public class PointCloudPlayback_VFX : MonoBehaviour
    {
        [Tooltip("Point cloud reader prefab")]
        public AbstractPointCloudSource reader_prefab;
        [Tooltip("Point cloud renderer prefab")]
        public PointCloudRenderer_VFX renderer_prefab;
        [Tooltip("If true start playback on Start")]
        public bool playOnStart = false;
        [Tooltip("Directory with point cloud files")]
        public string url = "";
        [Tooltip("Invoked when playback starts")]
        public UnityEvent started;
        [Tooltip("Invoked when playback finishes")]
        public UnityEvent finished;
        [Tooltip("(introspection) point cloud reader")]
        public AbstractPointCloudSource cur_reader;
        [Tooltip("(introspection) point cloud renderer")]
        public PointCloudRenderer_VFX cur_renderer;

        public string Name()
        {
            return $"{GetType().Name}";
        }

        // Start is called before the first frame update
        void Start()
        {
            if (playOnStart)
            {
                Play(url);
            }
        }

        public void Play(string new_url)
        {
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.LogError($"{Name()}: Play() called while playing");
                return;
            }
            cur_reader = Instantiate(reader_prefab, transform);
            cur_renderer = Instantiate(renderer_prefab, transform);
            cur_renderer.pointcloudSource = cur_reader;
            cur_renderer.started.AddListener(RendererStarted);
            cur_renderer.finished.AddListener(RendererFinished);
            Debug.Log($"{Name()}: Play({url})");
            url = new_url;
            StartCoroutine(startPlay());
        }

        private IEnumerator startPlay()
        {
            yield return null;
            StreamedPointCloudReader rdr = cur_reader as StreamedPointCloudReader;
            if (rdr != null) 
            {
                rdr.url = url;
            }
            cur_reader.gameObject.SetActive(true);
            cur_renderer.gameObject.SetActive(true);
        }

        public void Stop()
        {
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.Log($"{Name()}: Stop");
                StartCoroutine(stopPlay());
            }
        }

        private IEnumerator stopPlay()
        {
            yield return null;
            cur_reader.Stop();
            finished.Invoke(); // xxxjack or should this be done after the fade out?
            cur_renderer.started.RemoveListener(RendererStarted);
            cur_renderer.finished.RemoveListener(RendererFinished);
            Destroy(cur_reader.gameObject);
            Destroy(cur_renderer.gameObject);
            cur_reader = null;
            cur_renderer = null;
        }

        private void RendererStarted()
        {
            Debug.Log($"{Name()}: Renderer started");
            started.Invoke();
        }

        private void RendererFinished()
        {
            Debug.Log($"{Name()}: Renderer finished");
            StartCoroutine(stopPlay());
        }
    }
}
