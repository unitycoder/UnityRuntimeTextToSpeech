// https://github.com/unitycoder/UnityRuntimeTextToSpeech

using UnityEngine;
using System.IO;
using ESpeakWrapper;
using System.Threading;
using System.Collections.Generic;

namespace UnityLibrary
{
    // run before regular scripts
    [DefaultExecutionOrder(-100)]
    public class Speech : MonoBehaviour
    {
        // singleton isntance
        public static Speech instance;

        // queue for tts strings
        Queue<string> messages = new Queue<string>();
        bool isClosing = false;
        bool isRunning = false;

        void Awake()
        {
            instance = this;

            // initialize with espeak voices folder
            string datafolder = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data/");
            datafolder = datafolder.Replace("\\", "/");
            Client.Initialize(datafolder);

            // select voice
            string voiceID = "Tweaky";
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
            if (isClosing == false && isRunning == true)
            {
                messages.Enqueue(msg);
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