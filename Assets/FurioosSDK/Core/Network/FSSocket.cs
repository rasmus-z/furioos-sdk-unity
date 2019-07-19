﻿using System;
using FurioosSDK.Core;
using UnityEngine;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace FurioosSDK.Core {

    public class FSSocketBehavior : WebSocketBehavior
    {
        public delegate void OnDataHandler(string data, byte[] rawData);

        public static event OnDataHandler OnData;

        public static FSSocketBehavior socket;

        public static void SendData(string data) {
            socket.Send(data);
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            socket = this;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            Debug.Log(e.Reason);

            FSSocket.RestartServer();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);

            Debug.Log(e.Message);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Action Handler = () =>
            {
                OnData?.Invoke(
                    System.Text.Encoding.UTF8.GetString(e.RawData),
                    e.RawData
                );
            };

           FSSocket.QueueJob(Handler);
        }
    }

    public class FSSocket : FSBehaviour {
        static WebSocketServer server;
        static List<Action> jobs;
        static int maxJobsPerFrame = 1000;

        public delegate void OnDataHandler(string data, byte[] rawData);

        public static event OnDataHandler OnData;

        public void Start() {
            jobs = new List<Action>();

            /*
            server = new WebSocketServer(4321);
            server.AddWebSocketService<FSSocket>("/sdk");
            server.Start();
			Console.ReadKey(true);
            */

            Connect();
        }

        public void Connect() {
            var ws = new WebSocket("ws://localhost:80");

            ws.OnMessage += (sender, e) => {
                Action Handler = () => {
                    OnData?.Invoke(e.Data, e.RawData);
                };

                FSSocket.QueueJob(Handler);
            };

            ws.OnOpen += (sender, e) => {
                Debug.Log("connected");
            };

            ws.OnClose += (sender, e) => {
                Debug.Log(e.Code);
                //Connect();
            };

            ws.OnError += (sender, e) => {
                Debug.Log(e.Message);
            };

            ws.ConnectAsync();
        }

        public void Update()  {
            if (jobs != null) {
                var jobsExecutedCount = 0;
                while (jobs.Count > 0 && jobsExecutedCount++ < maxJobsPerFrame) {
                    var job = jobs[0];
                    jobs.RemoveAt(0);

                    try {
                        job.Invoke();
                    }
                    catch (System.Exception e) {
                        Debug.Log("Job invoke exception: " + e.Message);
                    }
                }
            }
        }

        public static void QueueJob(System.Action Job) {
            if (jobs == null) {
                jobs = new List<System.Action>();
            }

            jobs.Add(Job);
        }

        public static void RestartServer()
        {
            server.Start();
        }
    }
}