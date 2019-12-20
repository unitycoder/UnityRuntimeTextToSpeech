using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ESpeakWrapper
{

    [StructLayout(LayoutKind.Sequential)]
    struct Event
    {

        public enum EventType
        {
            ListTerminated,
            Word,
            Sentence,
            Mark,
            Play,
            End,
            MessageTerminated,
            Phoneme,
            SetSampleRate
        }

        public EventType Type;
        public uint UniqueIdentifier;
        public int TextPosition;
        public int Length;
        public int AudioPosition;
        public int Sample;
        public IntPtr UserData;
        public int Id;



    }
}
