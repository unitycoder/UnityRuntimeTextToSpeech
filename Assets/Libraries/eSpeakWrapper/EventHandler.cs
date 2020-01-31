using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ESpeakWrapper
{
    class EventHandler
    {
        public delegate int SynthCallback(IntPtr wavePtr, int bufferLength, IntPtr eventsPtr);
        public delegate void OnVoiceFinished();

        public static OnVoiceFinished ovf = null;

        static MemoryStream Stream;
        public static Mutex audio_files_mutex = new Mutex();
        public static List<byte[]> audio_files = new List<byte[]>();

        public static int Handle(IntPtr wavePtr, int bufferLength, IntPtr eventsPtr)
        {
            Console.WriteLine("Received event!");
            Console.WriteLine("Buffer length is " + bufferLength);

            // crash, but console log gets printed to output.log
            //Debug.Log("Buffer length is " + bufferLength);

            //Assume that synthesiz is final if buffer length is zero?
            if (bufferLength == 0)
            {
                //var file = new FileStream("alarm01.wav", FileMode.Open);
                //Stream.Seek(0, SeekOrigin.Begin);
                //file.CopyTo(Stream);

                //PlayAudio();
                //Console.Write(ConvertHeadersToString(Stream.GetBuffer()));

                Stream.Flush();
                audio_files_mutex.WaitOne();
                audio_files.Add(Stream.ToArray());
                audio_files_mutex.ReleaseMutex();
                Stream.Dispose();
                return 0;
            }

            WriteAudioToStream(wavePtr, bufferLength);

            var events = MarshalEvents(eventsPtr);

            foreach (Event anEvent in events)
            {
                Console.WriteLine(anEvent.Type);
                Console.WriteLine(anEvent.Id);
            }

            return 0; // continue synthesis
        }

        static List<Event> MarshalEvents(IntPtr eventsPtr)
        {
            var events = new List<Event>();
            int structSize = Marshal.SizeOf(typeof(Event));

            for (int i = 0; true; i++)
            {
                IntPtr data = new IntPtr(eventsPtr.ToInt64() + structSize * i);
                Event currentEvent = (Event)Marshal.PtrToStructure(data, typeof(Event));
                if (currentEvent.Type == Event.EventType.ListTerminated)
                {
                    break;
                }
                events.Add(currentEvent);
            }

            return events;
        }

        static int WriteAudioToStream(IntPtr wavePtr, int bufferLength)
        {
            if (wavePtr == IntPtr.Zero)
            {
                return 0; // Continue synthesis
            }

            if (Stream == null)
            {
                Stream = new MemoryStream();
                //InitializeStream();
            }

            byte[] audio = new byte[bufferLength * 2];
            Marshal.Copy(wavePtr, audio, 0, audio.Length);
            Stream.Write(audio, 0, audio.Length);

            return 0;
        }

        /*
        static void InitializeStream()
        {
            var ascii = Encoding.ASCII;
            Stream.Write(ascii.GetBytes("RIFF"), 0, 4);
            // Will fill in this block in PlayAudio()
            Stream.Write(BitConverter.GetBytes(0), 0, 4);
            Stream.Write(ascii.GetBytes("WAVEfmt "), 0, 8);
            // subchunk1 size: 16 for all PCM audio
            Stream.Write(BitConverter.GetBytes(16), 0, 4);
            // audio format: PCM is 1, which means linear quantization. Other values indicate some form of compression
            Stream.Write(BitConverter.GetBytes((short)1), 0, 2);
            // number of channels: 1 is mono, 2 is stereo
            Stream.Write(BitConverter.GetBytes((short)1), 0, 2);
            // sample rate
            Stream.Write(BitConverter.GetBytes(22050), 0, 4);
            // The complete formula for byte rate is sample rate * number of channels * bits per sample / 8
            // The number  of channels is 1, because eSpeak generates audio in mono
            // Bits per sample is 16, because each sample is represented by eSpeak as `short`, which has 16 bits
            // That's why we're multiplying the sample rate by 2 below. 1 * 16 / 8 is 2
            Stream.Write(BitConverter.GetBytes(22050 * 2), 0, 4);
            // block align: number of channels * bits per sample / 8, 1 * 16 / 8 = 2
            Stream.Write(BitConverter.GetBytes((short)2), 0, 2);
            // bits per sample: eSpeak has this at 16
            Stream.Write(BitConverter.GetBytes((short)16), 0, 2);
            // The data section
            Stream.Write(ascii.GetBytes("DATA"), 0, 4);
            // audio size: will fill this in the PlayAudio() method
            Stream.Write(BitConverter.GetBytes(0), 0, 4);
        }
        */

        /*
        static void PlayAudio()
        {
            Stream.Seek(4, SeekOrigin.Begin);
            Stream.Write(BitConverter.GetBytes(Stream.Length - 8), 0, 4);

            Stream.Seek(40, SeekOrigin.Begin);
            Stream.Write(BitConverter.GetBytes(Stream.Length - 44), 0, 4);

            Stream.Seek(0, SeekOrigin.Begin); // have to do this, otherwise the player will give a bogus error
            //var player = new SoundPlayer(Stream);

            //try
            //{
            //    player.Play();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}

            // NOTE this gets always generated.. we dont need?
            //using (var file = new FileStream("test.wav", FileMode.Create))
            //{
            //    Stream.WriteTo(file);
            //}

            if(ovf != null) {
                ovf();
            }
        }
        */

        static string PrintBytes(byte[] byteArray)
        {
            var sb = new StringBuilder("new byte[] { ");
            for (var i = 0; i < byteArray.Length; i++)
            {
                var b = byteArray[i];
                sb.Append(b);
                if (i < byteArray.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" }");
            return sb.ToString();
        }

        static string ConvertHeadersToString(byte[] buffer)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("The stream length is {0}.\n", Stream.Length);
            sb.Append(Encoding.ASCII.GetChars(buffer, 0, 4));
            sb.Append(BitConverter.ToInt32(buffer, 4));
            sb.Append(Encoding.ASCII.GetChars(buffer, 8, 8));
            sb.Append(BitConverter.ToInt32(buffer, 16));
            sb.Append(BitConverter.ToInt16(buffer, 20));
            sb.Append(BitConverter.ToInt16(buffer, 22));
            sb.Append(BitConverter.ToInt32(buffer, 24));
            sb.Append(BitConverter.ToInt32(buffer, 28));
            sb.Append(BitConverter.ToInt16(buffer, 32));
            sb.Append(BitConverter.ToInt16(buffer, 34));
            sb.Append(Encoding.ASCII.GetChars(buffer, 36, 4));
            sb.Append(BitConverter.ToInt32(buffer, 40));

            return sb.ToString();
        }

    }
}
