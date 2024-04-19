using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using static Datas;

public class PracticeModController : IngameController
{
    #region Public Members
    public int currentNoteIndex;
    public int loopStartDeltaTime;
    public int loopEndDeltaTime;
    public int loopStartNoteIndex;
    public int loopStartPassedNote;

    public bool isLoop;
    public bool isPlaying;
    public bool isSongEnd = false;
    #endregion

    #region Private Members
    private bool _isInputTiming = false;
    private bool _isWaitInput = true;
    #endregion

    PracticeModUIController _uiController;

    public void Init()
    {
        base.Init();

        currentNoteIndex = 0;

        isLoop = false;
        isPlaying = true;
        loopStartDeltaTime = -1;
        loopEndDeltaTime = -1;
        loopStartNoteIndex = 0;
        loopStartPassedNote = 0;

        _uiController = Managers.UI.currentUIController as PracticeModUIController;
        _uiController.BindIngameUI();
        _uiController.songTitleTMP.text = songTitle.Replace("_", " ");
        _uiController.songNoteMountTMP.text = $"0/{totalNote}";
        _uiController.songBpmTMP.text = $"{Managers.Midi.tempo}";
        _uiController.songBeatTMP.text = $"{Managers.Midi.beat.Key}/{Managers.Midi.beat.Value}";
        _uiController.songTimeSlider.maxValue = Managers.Midi.songLengthDelta;

        for (int i = 0; i < Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]].Count; i++)
        {
            correctNoteKeys.Add(Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Key);
        }

        Managers.Input.keyAction -= InputKeyEvent;
        Managers.Input.keyAction += InputKeyEvent;

        if (Managers.Input.inputDevice != null)
        {
            Managers.Input.inputDevice.EventReceived -= OnEventReceived;
            Managers.Input.inputDevice.EventReceived += OnEventReceived;
        }

        Managers.InitManagerPosition();
    }

    void Update()
    {
        WaitMidiInput();
        Scroll();
        StartCoroutine(ToggleKeyHighlight());
    }
    void Scroll()
    {
        if (!isPlaying || isSongEnd)
            return;
        if (isLoop)
        {
            if (currentDeltaTimeF >= loopEndDeltaTime)
            {
                currentNoteIndex = loopStartNoteIndex;
                passedNote = loopStartPassedNote;
                _uiController.UpdatePassedNote();
                currentDeltaTimeF = loopStartDeltaTime;
                SyncDeltaTime(false);
                transform.position = new Vector3(0, 0, -currentDeltaTimeF / Managers.Midi.song.division * Managers.Midi.noteScale + notePosOffset);
            }
        }
        if (currentNoteIndex < Managers.Midi.noteTiming.Count)
        {
            if (Managers.Midi.noteTiming[currentNoteIndex] <= currentDeltaTimeF && _isWaitInput)
            {
                currentDeltaTime = Managers.Midi.noteTiming[currentNoteIndex];
                if (currentDeltaTime != currentDeltaTimeF)
                    SyncDeltaTime(true);
                transform.position = new Vector3(0, 0, -currentDeltaTimeF / Managers.Midi.song.division * Managers.Midi.noteScale + notePosOffset);
                _isInputTiming = true;
                return;
            }
        }
        currentDeltaTimeF += 2 * Datas.DEFAULT_QUARTER_NOTE_MILLISEC / Managers.Midi.song.tempoMap[0].milliSecond * Managers.Midi.song.division * Time.deltaTime;
        SyncDeltaTime(false);
        transform.Translate(new Vector3(0, 0, -2 * Datas.DEFAULT_QUARTER_NOTE_MILLISEC / Managers.Midi.song.tempoMap[0].milliSecond * Managers.Midi.noteScale * Time.deltaTime));
    }

    void WaitMidiInput()
    {
        if (currentNoteIndex >= Managers.Midi.noteTiming.Count)
        {
            if (currentDeltaTime >= Managers.Midi.songLengthDelta)
            {
                if (!isSongEnd)
                {
                    _uiController.ToggleSongEndPanel();
                }
                isSongEnd = true;
            }
        }
        if (!_isInputTiming || isSongEnd)
            return;
        if (currentNoteIndex < Managers.Midi.noteTiming.Count)
        {
            for (int i = 0; i < Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]].Count; i++)
            {
                Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i] = new KeyValuePair<int, bool>(Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Key, Managers.Input.keyChecks[Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Key]);
                vPianoKeyEffect[Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Key].color = vPianoKeyEffectColors[2];
                if (!Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Value)
                {
                    if (Managers.Input.inputDevice != null && _initInputTiming[Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Key] < currentDeltaTime)
                        return;
                    _isInputTiming = false;
                    return;
                }
            }
            IncreaseCurrentNoteIndex();
        }
    }

    public void IncreaseCurrentNoteIndex()
    {
        if (Managers.Midi.noteTiming[currentNoteIndex] - currentDeltaTime > 0)
            return;
        correctNoteKeys.Clear();
        passedNote += Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]].Count;
        _uiController.UpdatePassedNote();
        currentNoteIndex += 1;

        for (int i = 0; i < Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]].Count; i++)
        {
            correctNoteKeys.Add(Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]][i].Key);
        }
    }

    public void AutoScroll()
    {
        _isWaitInput = !_isWaitInput;
    }

    public void TurnOffLoop()
    {
        isLoop = false;
        loopStartDeltaTime = -1;
        loopEndDeltaTime = -1;
    }

    public void SyncDeltaTime(bool isIntToFloat)
    {
        if (isIntToFloat)
        {
            currentDeltaTimeF = currentDeltaTime;
        }
        else
        {
            currentDeltaTime = currentDeltaTimeF - (int)currentDeltaTimeF < 0.5f ? (int)currentDeltaTimeF : (int)currentDeltaTimeF + 1;
        }
        _uiController.songTimeSlider.SetValueWithoutNotify(currentDeltaTime);
    }

    public IEnumerator UpdateNotePosByTime()
    {
        transform.position = new Vector3(0, 0, -currentDeltaTimeF / Managers.Midi.song.division * Managers.Midi.noteScale + notePosOffset);
        while (true)
        {
            if (currentNoteIndex > 0)
            {
                if (Managers.Midi.noteTiming[currentNoteIndex - 1] >= currentDeltaTime)
                {
                    currentNoteIndex--;
                    passedNote -= Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]].Count;
                    _uiController.UpdatePassedNote();
                    continue;
                }
            }
            if (currentNoteIndex < Managers.Midi.noteTiming.Count - 1)
            {
                if (Managers.Midi.noteTiming[currentNoteIndex] < currentDeltaTime)
                {
                    passedNote += Managers.Midi.noteSetBySameTime[Managers.Midi.noteTiming[currentNoteIndex]].Count;
                    currentNoteIndex++;
                    _uiController.UpdatePassedNote();
                    continue;
                }
            }
            break;
        }
        yield return null;
    }

    void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        var midiDevice = (MidiDevice)sender;
        if (e.Event.EventType != MidiEventType.ActiveSensing)
        {
            NoteEvent noteEvent = e.Event as NoteEvent;

            if (isSongEnd && noteEvent.Velocity != 0)
            {
                isSongEnd = false;
                currentDeltaTime = 0;
                SyncDeltaTime(true);
                StartCoroutine(UpdateNotePosByTime());
                _uiController.ToggleSongEndPanel();
            }
            // 노트 입력 시작
            if (noteEvent.Velocity != 0)
            {
                _initInputTiming[noteEvent.NoteNumber - 1 - DEFAULT_KEY_NUM_OFFSET] = currentDeltaTime;
                Managers.Input.keyChecks[noteEvent.NoteNumber - 1 - DEFAULT_KEY_NUM_OFFSET] = true;
                Debug.Log(noteEvent);
            }
            // 노트 입력 종료
            else if (noteEvent.Velocity == 0)
            {
                _initInputTiming[noteEvent.NoteNumber - 1 - DEFAULT_KEY_NUM_OFFSET] = -1;
                Managers.Input.keyChecks[noteEvent.NoteNumber - 1 - DEFAULT_KEY_NUM_OFFSET] = false;
                Debug.Log(noteEvent);
            }
        }
    }

    void InputKeyEvent(KeyCode keyCode, Define.InputType inputType)
    {
        if (isSongEnd && keyCode != KeyCode.Escape)
        {
            isSongEnd = false;
            currentDeltaTime = 0;
            SyncDeltaTime(true);
            StartCoroutine(UpdateNotePosByTime());
            _uiController.ToggleSongEndPanel();
            return;
        }
        switch (inputType)
        {
            case Define.InputType.OnKeyDown:
                switch (keyCode)
                {
                    case KeyCode.Escape:
                        if (isSongEnd)
                        {
                            Managers.CleanManagerChilds();
                            Managers.Input.keyAction = null;
                            Managers.Scene.LoadScene(Define.Scene.SongSelectScene);
                        }
                        break;

                    case KeyCode.LeftBracket:
                        SetStartDeltaTime();
                        break;

                    case KeyCode.RightBracket:
                        SetEndDeltaTime();
                        break;
                }
                break;
            case Define.InputType.OnKeyUp:
                break;
        }
    }

    void SetStartDeltaTime()
    {
        loopStartDeltaTime = currentDeltaTime;
        loopStartNoteIndex = currentNoteIndex;
        loopStartPassedNote = passedNote;
        _uiController.SetLoopStartMarker();
        if (loopEndDeltaTime >= 0 && loopStartDeltaTime > loopEndDeltaTime)
        {
            int temp = loopEndDeltaTime;
            loopEndDeltaTime = loopStartDeltaTime;
            loopStartDeltaTime = temp;
            Debug.Log($"Start/End Time Swaped! {loopStartDeltaTime} ~ {loopEndDeltaTime}");
            _uiController.SwapStartEndMarker();
        }
        Debug.Log($"Loop Start Delta Time Set to {loopStartDeltaTime}");
        if (loopEndDeltaTime >= 0)
        {
            isLoop = true;
            _uiController.ActiveLoopBtn();
        }
    }

    void SetEndDeltaTime()
    {
        if (loopStartDeltaTime < 0)
            return;
        loopEndDeltaTime = currentDeltaTime;
        _uiController.SetLoopEndMarker();
        if (loopStartDeltaTime >= 0 && loopStartDeltaTime > loopEndDeltaTime)
        {
            int temp = loopEndDeltaTime;
            loopEndDeltaTime = loopStartDeltaTime;
            loopStartDeltaTime = temp;
            Debug.Log($"Start/End Time Swaped! {loopStartDeltaTime} ~ {loopEndDeltaTime}");
            _uiController.SwapStartEndMarker();
        }
        Debug.Log($"Loop End Delta Time Set to {loopEndDeltaTime}");
        if (loopStartDeltaTime >= 0)
        {
            isLoop = true;
            _uiController.ActiveLoopBtn();
        }
    }
}
