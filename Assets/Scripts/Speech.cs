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

        // queue for tts strings
        Queue<string> messages = new Queue<string>();
        bool isClosing = false;
        bool isRunning = false;

        Mutex audio_list_mutex = new Mutex();
        List<float[]> audio_files = new List<float[]>();
        public AudioSource audio_source;

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
            isRunning = true;
            while (isClosing == false)
            {
                if (messages.Count > 0)
                {
                    try
                    {
                        var msg = messages.Dequeue();

                        Client.Speak(msg);

                        // could use SSML also
                        //Client.SpeakSSML(msg);
                        if(Client.VoiceFinished()) {
                            byte[] new_voice = Client.PopVoice();
                            float[] voice_float = new float[new_voice.Length/2];

                            for(int i = 0; i < voice_float.Length; i++) {
                                //if(BitConverter.IsLittleEndian) 
                                voice_float[i] = (float)BitConverter.ToInt16(new_voice, i*2)/(float)short.MaxValue;
                            }
                            audio_list_mutex.WaitOne();      
                            audio_files.Add(voice_float);
                            audio_list_mutex.ReleaseMutex();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else
                {
                    Thread.Sleep(16);
                }
            }
            isRunning = false;
        }

        // adds string to TTS queue
        public void Say(string msg)
        {
            if (isClosing == true || isRunning == false) return;
            if (string.IsNullOrEmpty(msg)) return;

            messages.Enqueue(msg);
        }

        public void Update()
        {
            float[] data = null;
            audio_list_mutex.WaitOne();
            Debug.Log(audio_files.Count);
            if(audio_files.Count > 0) {
                data = audio_files[0];
                audio_files.RemoveAt(0);
            }
            audio_list_mutex.ReleaseMutex();
            if(data != null) {
                AudioClip ac = AudioClip.Create("voice", data.Length, 1, Client.sampleRate, false);
                ac.SetData(data,0);
                audio_source.clip = ac;
                audio_source.loop = true;
                audio_source.Play();
            }
        }

        private void OnDestroy()
        {
            Client.Stop();
            isClosing = true;

            // NOTE this will hang unity, until speech has stopped (otherwise crash)
            while (isRunning) { };
        }

    } // class
} // namespace