using UnityEngine;

public class TestUI : MonoBehaviour
{
    MidiTest midiTest;

    void Start()
    {
        midiTest = GameObject.Find("@Manager").GetComponent<MidiTest>();
    }

    public void ScrollNextBtn()
    {
        midiTest.IncreaseCurrentNoteIndex();
    }

    public void DisconnectPianoBtn()
    {
        midiTest.DisconnectPiano();
    }

    public void AutoScrollBtn()
    {
        midiTest.AutoScroll();
    }

    public void TurnOffLoop()
    {
        midiTest.TurnOffLoop();
    }
}