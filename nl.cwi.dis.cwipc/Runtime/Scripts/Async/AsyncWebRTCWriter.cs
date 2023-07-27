﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using Cwipc;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    /// <summary>
    /// Class that writes frames over WebRTC
    ///
    /// The class supports sending tiled streams, by creating multiple servers (on increasing port numbers).
    /// </summary>
    public class AsyncWebRTCWriter : AsyncWriter
    {

        // xxxjack The next two types have to be replaced with whatever identifies our
        // outgoing connection to our peer and whetever identifies the thing that we use to transmit a
        // sequence of frames over
        protected class XxxjackPeerConnection { };
        protected class XxxjackTrackOrStream { };
        protected struct WebRTCStreamDescription
        {
            public int index;
            public XxxjackTrackOrStream trackOrStream;
            public uint fourcc;
            public QueueThreadSafe inQueue;
        };

        protected XxxjackPeerConnection peerConnection;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        protected WebRTCStreamDescription[] descriptions;

        protected class WebRTCPushThread
        {
            AsyncWebRTCWriter parent;
            WebRTCStreamDescription description;
            System.Threading.Thread myThread;
          
            public WebRTCPushThread(AsyncWebRTCWriter _parent, WebRTCStreamDescription _description)
            {
                parent = _parent;
                description = _description;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
#if VRT_WITH_STATS
                stats = new Stats(Name());
#endif
                Debug.Log($"{Name()}:  Should initialize stream or track");
            }

            public string Name()
            {
                return $"{parent.Name()}.{description.index}";
            }

            public void Start()
            {
                myThread.Start();
            }

            public void Stop()
            {
               Debug.Log($"{Name()}:  should close stream or track");
            }

            public void Join()
            {
                myThread.Join();
            }

            protected void run()
            {
                try
                {
                    Debug.Log($"{Name()}: thread started");
                    QueueThreadSafe queue = description.inQueue;
                    while (!queue.IsClosed())
                    {
                        
                        NativeMemoryChunk mc = (NativeMemoryChunk)queue.Dequeue();
                        if (mc == null) continue; // Probably closing...
#if VRT_WITH_STATS
                        stats.statsUpdate(mc.length);
#endif
                        byte[] hdr = new byte[16];
                        var hdr1 = BitConverter.GetBytes((UInt32)description.fourcc);
                        hdr1.CopyTo(hdr, 0);
                        var hdr2 = BitConverter.GetBytes((Int32)mc.length);
                        hdr2.CopyTo(hdr, 4);
                        var hdr3 = BitConverter.GetBytes(mc.metadata.timestamp);
                        hdr3.CopyTo(hdr, 8);
                        var buf = new byte[mc.length];
                        System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                        Debug.Log($"{Name()}: should transmit header and {mc.length} bytes");
                        
                    }
                    Debug.Log($"{Name()}: thread stopped");
                }
#pragma warning disable CS0168
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    throw;
#else
                    Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                    Debug.LogError("Error while sending visual representation or audio to other participants");
#endif
                }

            }

#if VRT_WITH_STATS
            protected class Stats : Statistics
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes = 0;
                double statsTotalPackets = 0;
                int statsAggregatePackets = 0;

                public void statsUpdate(int nBytes)
                {
 
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    statsAggregatePackets++;

                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / (statsTotalPackets == 0 ? 1 : statsTotalPackets))}, aggregate_packets={statsAggregatePackets}");
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                    }
                }
            }

            protected Stats stats;
#endif
        }
 
        WebRTCPushThread[] pusherThreads;

        protected AsyncWebRTCWriter() : base()
        {
           
        }

        /// <summary>
        /// Setup a WebRTC output (transmitter)
        /// Probably needs to open a connection to a WebRTC server.
        /// </summary>
        /// <param name="_url">Where the server should ser on</param>
        /// <param name="fourcc">4CC media type</param>
        /// <param name="_descriptions">Array of stream descriptions</param>
        public AsyncWebRTCWriter(string _url, string fourcc, OutgoingStreamDescription[] _descriptions) : base()
        {
            NoUpdateCallsNeeded();
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception($"{Name()}: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"{Name()}: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = StreamSupport.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
            Uri url = new Uri(_url);
            
            WebRTCStreamDescription[] ourDescriptions = new WebRTCStreamDescription[_descriptions.Length];
            // We use the lowest ports for the first quality, for each tile.
            // The the next set of ports is used for the next quality, and so on.
            int maxTileNumber = -1;
            for(int i=0; i<_descriptions.Length; i++)
            {
                if (_descriptions[i].tileNumber > maxTileNumber) maxTileNumber = (int)_descriptions[i].tileNumber;
            }
            int portsPerQuality = maxTileNumber+1;
            for(int i=0; i<_descriptions.Length; i++)
            {
                ourDescriptions[i] = new WebRTCStreamDescription
                {
                    index = (int)_descriptions[i].tileNumber + (portsPerQuality * _descriptions[i].qualityIndex),
                    trackOrStream = new XxxjackTrackOrStream(),
                    fourcc = fourccInt,
                    inQueue = _descriptions[i].inQueue

                };
            }
            descriptions = ourDescriptions;
            Start();
        }

        protected override void Start()
        {
            base.Start();
            int nThreads = descriptions.Length;
            pusherThreads = new WebRTCPushThread[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new WebRTCPushThread(this, descriptions[i]);
#if VRT_WITH_STATS
                Statistics.Output(base.Name(), $"pusher={pusherThreads[i].Name()}, stream={i}");
#endif
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void AsyncOnStop()
        {
            // Signal that no more data is forthcoming to every pusher
            for (int i = 0; i < descriptions.Length; i++)
            {
                var d = descriptions[i];
                if (!d.inQueue.IsClosed())
                {
                    Debug.LogWarning($"{Name()}.{i}: input queue not closed. Closing.");
                    d.inQueue.Close();
                }
            }
            // Stop our thread
            base.AsyncOnStop();
            // wait for pusherThreads to terminate
            foreach (var t in pusherThreads)
            {
                t.Stop();
                t.Join();
            }
            Debug.Log($"{Name()} Stopped");
        }

        protected override void AsyncUpdate()
        {
        }

#if xxxjack_disabled

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);

            return new SyncConfig.ClockCorrespondence
            {
                wallClockTime = (Timestamp)sinceEpoch.TotalMilliseconds,
                streamClockTime = uploader.get_media_time(1000)
            };
        }
#endif
    }
}
