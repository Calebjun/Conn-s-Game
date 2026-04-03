using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame.Core
{
    [Serializable]
    public class NoteData
    {
        public int lane;           // 0-3 (four lanes)
        public float beatTime;     // Time in seconds from bar start when note should be hit
        public NoteType noteType;
    }

    [Serializable]
    public enum NoteType
    {
        Normal,
        Hold
    }

    [Serializable]
    public class BarData
    {
        public int barIndex;       // 0-17 matching your music array
        public List<NoteData> notes = new List<NoteData>();
    }
}
