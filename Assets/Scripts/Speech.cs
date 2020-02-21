// https://github.com/unitycoder/UnityRuntimeTextToSpeech

using UnityEngine;
using System.IO;
using ESpeakWrapper;
using System.Threading;
using System.Collections.Generic;
using System;

namespace UnityLibrary
{
    // run before regular scripts
    [DefaultExecutionOrder(-100)]
    public class Speech : MonoBehaviour
    {
        public string voiceID = "Tweaky";

        // singleton isntance
        public static Speech instance;

        public delegate void TTSCallback(string message, AudioClip audio);

        public enum IncomingMessageType {
            Say,
            SetRate,
            SetVolume,
            SetPitch,
            SetRange,
            SetWordGap,
            SetCapitals,
            SetIntonation,
            SetVoice,
        }
        public class IncomingMessage {
            public IncomingMessageType type;
            public int param1;
            public string message;
            public TTSCallback callback;
        }
        

        // queue for tts strings
        Mutex message_mutex = new Mutex();
        Queue<IncomingMessage> messages = new Queue<IncomingMessage>();
        bool isClosing = false;
        bool isRunning = false;


        enum OutgoingMessageType {
            VoiceLineFinished,
        }

        class OutgoingMessage {
            public OutgoingMessageType type;
            public string message;
            public float[] data;
            public TTSCallback callback;
        }

        Mutex outgoing_message_mutex = new Mutex();
        Queue<OutgoingMessage> outgoing_messages = new Queue<OutgoingMessage>();

        void Awake()
        {
            instance = this;

            // initialize with espeak voices folder
            string datafolder = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data/");
            datafolder = datafolder.Replace("\\", "/");
            Client.Initialize(datafolder);

            // select voice
            var setvoice = Client.SetVoiceByName(voiceID);
            if (setvoice == false) Debug.Log("Failed settings voice: " + voiceID);

            // start thread for processing received TTS strings
            Thread thread = new Thread(new ThreadStart(SpeakerThread));
            thread.Start();
        }

        void SpeakerThread()
        {
            bool waiting_for_line = false;
            string message_waited_for = "";
            TTSCallback callback_waited_for = null;
            SetIsRunning(true);
            while (IsClosing() == false) {
                if(waiting_for_line) {
                    if(Client.VoiceFinished()) {
                        byte[] new_voice = Client.PopVoice();
                        float[] voice_float = new float[new_voice.Length/2];

                        for(int i = 0; i < voice_float.Length; i++) {
                            //if(BitConverter.IsLittleEndian) 
                            voice_float[i] = (float)BitConverter.ToInt16(new_voice, i*2)/(float)short.MaxValue;
                        }
                        OutgoingMessage om = new OutgoingMessage();
                        om.type = OutgoingMessageType.VoiceLineFinished;
                        om.data = voice_float;
                        om.message = message_waited_for;
                        om.callback = callback_waited_for;

                        outgoing_message_mutex.WaitOne();
                        outgoing_messages.Enqueue(om);
                        outgoing_message_mutex.ReleaseMutex();
                        waiting_for_line = false;
                        message_waited_for = "";
                        callback_waited_for = null;
                    }
                } else if (HasMessage()) {
                    try
                    {
                        IncomingMessage msg = PopMessage();
                        switch(msg.type) { 
                            case IncomingMessageType.Say:
                                Client.Speak(msg.message);
                                //Client.SpeakSSML(msg);

                                message_waited_for = msg.message;
                                callback_waited_for = msg.callback;
                                waiting_for_line = true;
                                break;
                            case IncomingMessageType.SetPitch:
                                Client.SetPitch(msg.param1);
                                break;
                            case IncomingMessageType.SetRange:
                                Client.SetRange(msg.param1);
                                break;
                            case IncomingMessageType.SetRate:
                                Client.SetRate(msg.param1);
                                break;
                            case IncomingMessageType.SetVolume:
                                Client.SetVolume(msg.param1);
                                break;
                            case IncomingMessageType.SetWordGap:
                                Client.SetWordgap(msg.param1);
                                break;
                            case IncomingMessageType.SetCapitals:
                                Client.SetCapitals(msg.param1);
                                break;
                            case IncomingMessageType.SetIntonation:
                                Client.SetIntonation(msg.param1);
                                break;
                            case IncomingMessageType.SetVoice:
                                Client.SetVoiceByName(msg.message);
                                break;

                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                Thread.Sleep(8);
            }
            isRunning = false;
        }

        // adds string to TTS queue
        public void Say(string msg, TTSCallback callback)
        {
            if (IsClosing() == true || IsRunning() == false) return;
            if (string.IsNullOrEmpty(msg)) return;

            IncomingMessage im = new IncomingMessage();
            im.type = IncomingMessageType.Say;
            im.message = msg;
            im.callback = callback;
            QueueMessage(im);
        }

        public void QueueMessage(IncomingMessage im) {
            message_mutex.WaitOne();
            messages.Enqueue(im);
            message_mutex.ReleaseMutex();
        }

        private bool HasMessage()
        {
            bool ret = false;
            message_mutex.WaitOne();
            if(messages.Count > 0) {
                ret = true;
            }
            message_mutex.ReleaseMutex();
            return ret;
        }

        private IncomingMessage PopMessage() {
            IncomingMessage im = null;
            message_mutex.WaitOne();
            if(messages.Count > 0) {
                im = messages.Dequeue();
            }
            message_mutex.ReleaseMutex();
            return im;
        }

        public void SetIsClosing(bool val) {
            message_mutex.WaitOne();
            isClosing = val;
            message_mutex.ReleaseMutex();
        }

        public void SetIsRunning(bool val) { 
            message_mutex.WaitOne();
            isRunning = val;
            message_mutex.ReleaseMutex();
        }

        public bool IsClosing() {
            bool val = false;
            message_mutex.WaitOne();
            val = isClosing;
            message_mutex.ReleaseMutex();
            return val;
        }

        public bool IsRunning() {
            bool val = false;
            message_mutex.WaitOne();
            val = isRunning;
            message_mutex.ReleaseMutex();
            return val;
        }

        public void Update()
        {
            OutgoingMessage om = null;
            outgoing_message_mutex.WaitOne();
            if(outgoing_messages.Count > 0) {
                om = outgoing_messages.Dequeue();
            }
            outgoing_message_mutex.ReleaseMutex();
            if(om != null) {
                AudioClip ac = AudioClip.Create("voice", om.data.Length, 1, Client.sampleRate, false);
                ac.SetData(om.data,0);
                om.callback(om.message,ac);
            }
        }

        private void OnDestroy()
        {
            Client.Stop();
            SetIsClosing(true);

            int wait_counter = 2000;
            // NOTE this will hang unity, until speech has stopped (otherwise crash)
            while (IsRunning()) { 
                Thread.Sleep(1); 
                if(wait_counter-- < 0) {
                    Debug.LogError("Sound system dindn't shut down in time.");
                    break;
                }
            };
        }

    } // class
} // namespace