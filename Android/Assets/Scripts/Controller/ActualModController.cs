using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Datas;

public class ActualModController : IngameController
{
    #region Public Members
    public int currentCorrect;
    public int currentFail;
    public int totalAcc;
    public int currentBar = -1;
    public float currentAcc = 1;
    #endregion

    #region Private Members
    private int[] _lastInputTiming = new int[88];
    private int noteCheckCoroutineCnt = 0;

    private bool _isSceneOnSwap = false;
    private bool _isIntro = true;

    private Dictionary<int, List<KeyValuePair<int, int>>> _noteRecords;
    #endregion

    public void Init()
    {
        base.Init();

        currentCorrect = 0;
        currentFail = 0;

        totalAcc = Managers.Midi.totalDeltaTime;
        tempo = Managers.Midi.tempo;

        _uiController = Managers.UI.currentUIController as ActualModUIController;
        (_uiController as ActualModUIController).BindIngameUI();
        _uiController.songTitleTMP.text = songTitle.Replace("_", " ");
        _uiController.songNoteMountTMP.text = $"0/{totalNote}";
        _uiController.songTempoTMP.text = $"{tempo}";
        _uiController.songBeatTMP.text = $"{Managers.Midi.beat.Key}/{Managers.Midi.beat.Value}";
        _uiController.songTimeSlider.maxValue = Managers.Midi.songLengthDelta;

        _noteRecords = new Dictionary<int, List<KeyValuePair<int, int>>>();

        // Managers.Input.keyAction -= InputKeyEvent;
        // Managers.Input.keyAction += InputKeyEvent;

        if (Managers.Input.isPianoConnected)
        {
            Managers.Input.noteAction -= OnEventReceived;
            Managers.Input.noteAction += OnEventReceived;
        }

        Managers.InitManagerPosition();
        StartCoroutine(DelayForSeconds(3));
    }

    void Update()
    {
        Scroll();
        CheckNotesStatus();
        if (currentDeltaTime > Managers.Midi.songLengthDelta && !_isSceneOnSwap)
            SwapScene();
        StartCoroutine(ToggleKeyHighlight());
    }

    public IEnumerator DelayForSeconds(float seconds)
    {
        for (int i = (int)seconds / 1; i > 0; i--)
        {
            (_uiController as ActualModUIController).introCountTMP.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        (_uiController as ActualModUIController).introCountTMP.gameObject.SetActive(false);
        _isIntro = false;
    }

    void SwapScene()
    {
        Debug.Log(noteCheckCoroutineCnt);
        if (noteCheckCoroutineCnt > 0)
            return;

        Managers.Input.keyAction = null;
        if (!PlayerPrefs.HasKey("trans_SongTitle"))
            PlayerPrefs.SetString("trans_SongTitle", "");
        PlayerPrefs.SetString("trans_SongTitle", songTitle);

        if (!PlayerPrefs.HasKey("trans_FailMount"))
            PlayerPrefs.SetInt("trans_FailMount", 0);
        PlayerPrefs.SetInt("trans_FailMount", currentFail);
        if (!PlayerPrefs.HasKey("trans_OutlinerMount"))
            PlayerPrefs.SetInt("trans_OutlinerMount", 0);
        PlayerPrefs.SetInt("trans_OutlinerMount", 0);

        Managers.Data.userReplayRecord = new Define.UserReplayRecord(_noteRecords, tempo, songTitle, currentAcc);
        Managers.CleanManagerChilds();
        Managers.Scene.LoadScene(Define.Scene.ResultScene);
        _isSceneOnSwap = true;
    }

    void Scroll()
    {
        if (_isIntro)
            return;
        if (currentBar < currentDeltaTime / Managers.Midi.song.division)
        {
            Managers.Sound.metronomeAction.Invoke();
            currentBar++;
        }
        currentDeltaTimeF += 2 * Datas.DEFAULT_QUARTER_NOTE_MILLISEC / Managers.Midi.song.tempoMap[0].milliSecond * Managers.Midi.song.division * Time.deltaTime;
        SyncDeltaTime(false);
        transform.Translate(new Vector3(0, 0, -2 * Datas.DEFAULT_QUARTER_NOTE_MILLISEC / Managers.Midi.song.tempoMap[0].milliSecond * Managers.Midi.noteScale * Time.deltaTime));
        UpdateTempo();
        UpdateBeat();
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

    void CheckNotesStatus()
    {
        for (int i = 0; i < 88; i++)
        {
            if (Managers.Midi.noteSetByKey.ContainsKey(i))
            {
                if (Managers.Midi.noteSetByKey[i].Count > 0 && Managers.Midi.noteSetByKey[i].Count > Managers.Midi.nextKeyIndex[i])
                {
                    CheckNotesStatus(i);
                }
            }
        }
    }

    void CheckNotesStatus(int keyNum)
    {
        noteCheckCoroutineCnt += 1;
        if (_lastInputTiming[keyNum] == 0 && Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key != 0)
            _lastInputTiming[keyNum] = Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key;

        if (Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Value - currentDeltaTime < 0)
        {
            if (_lastInputTiming[keyNum] < Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Value)
            {
                if (!Managers.Input.keyChecks[keyNum])
                {
                    currentFail += Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Value - _lastInputTiming[keyNum];
                    currentAcc = (totalAcc - currentFail) / (float)totalAcc;
                }
            }
            Managers.Midi.nextKeyIndex[keyNum]++;
        }

        // �Ϲ����� ��Ʈ ��Ȯ�� �˻� �κ�, �ε��� ������ �������� ���ǹ�
        if (Managers.Midi.noteSetByKey[keyNum].Count > Managers.Midi.nextKeyIndex[keyNum])
        {
            if (Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key - currentDeltaTime < 0)
            {
                if (_lastInputTiming[keyNum] < Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key)
                    _lastInputTiming[keyNum] = Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key;

                // �ǾƳ� �Է�������, ����ģ ���Է��� �ƴ��� üũ
                if ((!Managers.Input.keyChecks[keyNum] && _lastInputTiming[keyNum] < Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Value) ||
                    (!Managers.Input.keyChecks[keyNum] && _initInputTiming[keyNum] > Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key && _lastInputTiming[keyNum] < Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Value - Managers.Midi.song.division / 10f) ||
                    _initInputTiming[keyNum] < Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key - Managers.Midi.song.division / 10f)
                {
                    currentFail += currentDeltaTime - _lastInputTiming[keyNum];
                }
                else if (Managers.Input.keyChecks[keyNum] && _lastInputTiming[keyNum] >= Managers.Midi.noteSetByKey[keyNum][Managers.Midi.nextKeyIndex[keyNum]].Key)
                {
                    currentCorrect += currentDeltaTime - _lastInputTiming[keyNum];
                }

                _lastInputTiming[keyNum] = currentDeltaTime;
                currentAcc = (totalAcc - currentFail) / (float)totalAcc;
            }
        }

        (_uiController as ActualModUIController).UpdateAccuracy();

        noteCheckCoroutineCnt -= 1;
    }

    void OnEventReceived(int noteNum, int velocity)
    {
        // ��Ʈ �Է� ����
        if (velocity != 0)
        {
            _initInputTiming[noteNum - 1 - DEFAULT_KEY_NUM_OFFSET] = currentDeltaTime;
            Managers.Input.keyChecks[noteNum - 1 - DEFAULT_KEY_NUM_OFFSET] = true;
            if (!_noteRecords.ContainsKey(noteNum - DEFAULT_KEY_NUM_OFFSET))
                _noteRecords.Add(noteNum - DEFAULT_KEY_NUM_OFFSET, new List<KeyValuePair<int, int>>());
            Debug.Log(noteNum);
        }
        // ��Ʈ �Է� ����
        else if (velocity == 0)
        {
            _noteRecords[noteNum - DEFAULT_KEY_NUM_OFFSET].Add(new KeyValuePair<int, int>(_initInputTiming[noteNum - 1 - DEFAULT_KEY_NUM_OFFSET], currentDeltaTime));
            _initInputTiming[noteNum - 1 - DEFAULT_KEY_NUM_OFFSET] = -1;
            Managers.Input.keyChecks[noteNum - 1 - DEFAULT_KEY_NUM_OFFSET] = false;
        }
    }
}