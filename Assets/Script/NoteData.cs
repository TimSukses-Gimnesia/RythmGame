using System.Collections.Generic;

[System.Serializable]
public class NoteData
{
    public float beat;        // posisi dalam beat
    public string type;       // "note" / "hold" / "obstacle"(opsional)
    public string dir;        // "left" / "down" / "up" / "right"
    public float holdBeat;    // durasi hold dalam beat (0 jika bukan hold)
}

[System.Serializable]
public class NoteDataList
{
    public List<NoteData> notes;
}
