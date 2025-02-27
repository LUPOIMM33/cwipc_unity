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
            Debug.Log(Name() + ": Ask Play(" + new_url + ")");
            StartCoroutine(startPlay(new_url));
        }


        private IEnumerator startPlay(string new_url)
        {
            Debug.Log(Name() + ": Starting playback of " + new_url);
            if (cur_reader != null || cur_renderer != null)
            {
                Debug.Log(Name() + ": Stopping current playback to start new one");
                yield return stopPlay();
            }
            url = new_url;
            cur_reader = Object.Instantiate(reader_prefab, base.transform);
            cur_renderer = Object.Instantiate(renderer_prefab, base.transform);
            cur_renderer.pointcloudSource = cur_reader;
            cur_renderer.started.AddListener(RendererStarted);
            cur_renderer.finished.AddListener(RendererFinished);
            
            yield return null;
            StreamedPointCloudReader rdr = cur_reader as StreamedPointCloudReader;
            if (rdr != null)
            {
                rdr.url = url;
            }

            cur_reader.gameObject.SetActive(true);
            cur_renderer.gameObject.SetActive(true);
            Debug.Log($"{Name()}: Started playback of {new_url}");
        }

        public void Stop()
        {
            Debug.Log($"{Name()}: Ask Stop");
            StartCoroutine(stopPlay());
        }

        private IEnumerator stopPlay()
        {
            Debug.Log($"{Name()}: Stopping playback");
            if (cur_reader == null && cur_renderer == null)
            {
                yield break;
            }
            yield return null;
            cur_reader.Stop();
            finished.Invoke(); // xxxjack or should this be done after the fade out?
            cur_renderer.started.RemoveListener(RendererStarted);
            cur_renderer.finished.RemoveListener(RendererFinished);
            Destroy(cur_reader.gameObject);
            Destroy(cur_renderer.gameObject);
            cur_reader = null;
            cur_renderer = null;
            Debug.Log($"{Name()}: Stopped playback");
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
