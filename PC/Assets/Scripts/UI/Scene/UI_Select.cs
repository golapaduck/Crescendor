using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Select : UI_Scene
{
    enum GameObjects
    {
        SongPanel,
        RankPanel,
    }

    enum Buttons
    {
        RankButton,
    }

    GameObject rankPanelObj;
    GameObject noRankSignPanelObj;
    Define.RankRecordList rankRecords;

    void Start()
    {
        Init();
    }
    
    public override void Init()
    {
        PlayerPrefs.SetString("trans_SongTitle", "");

        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));
        GameObject songPanel = Get<GameObject>((int)GameObjects.SongPanel);
        Managers.Song.LoadSongsFromConvertsFolder();
        foreach (Transform child in songPanel.transform)
            Managers.Data.Destroy(child.gameObject);

        rankPanelObj = Get<GameObject>((int)GameObjects.RankPanel);
        noRankSignPanelObj = rankPanelObj.transform.parent.Find("NoRankExists").gameObject;

        // SongManager�� �� ������ �̿��Ͽ� ��ư ����
        for (int i = 0; i < Managers.Song.songs.Count; i++)
        {
            // SongButton ������ �ε�
            GameObject songButtonPrefab = Managers.Data.Instantiate($"UI/Sub/SongButton", songPanel.transform);
            // SongButton ����
            if (songButtonPrefab != null)
            {
                Button button = songButtonPrefab.GetComponent<Button>();

                // Song ������ ��ư�� ǥ��
                if (button != null)
                {
                    // ���÷� Song�� songTitle�� ��ư�� ǥ��
                    button.GetComponentInChildren<TextMeshProUGUI>().text = Managers.Song.songs[i].songTitle;
                    button.onClick.AddListener(() => OnSongButtonClick(button.GetComponentInChildren<TextMeshProUGUI>().text));
                }
            }
            else
            {
                Debug.LogError($"Failed to load SongButton prefab");
            }
        }
    }

    public void OnSongButtonClick(string songName)
    {
        if (!PlayerPrefs.HasKey("trans_SongTitle"))
            PlayerPrefs.SetString("trans_SongTitle", "");
        string currentSongTitle = PlayerPrefs.GetString("trans_SongTitle");
        if (currentSongTitle != songName)
        {
            PlayerPrefs.SetString("trans_SongTitle", songName);
            UpdateRankList();
        }
        else if (currentSongTitle == songName)
        {
            (Managers.UI.currentUIController as SongSelectUIController).ShowPopupUI<UI_SongPopup>();
        }
    }

    public void OnRankButtonClick(PointerEventData data)
    {
        (Managers.UI.currentUIController as SongSelectUIController).ShowPopupUI<UI_RankPopUp>();
    }

    void UpdateRankList()
    {
        foreach (Transform child in rankPanelObj.transform)
        {
            Destroy(child.gameObject);
        }

        string testSongTitle = PlayerPrefs.GetString("trans_SongTitle");
        Managers.Data.GetRankListFromServer(testSongTitle);

        rankRecords = JsonUtility.FromJson<Define.RankRecordList>(Managers.Data.jsonDataFromServer);
        Managers.Data.jsonDataFromServer = "init data";

        if (rankRecords.records.Count == 0)
        {
            noRankSignPanelObj.SetActive(true);
        }

        else
        {
            noRankSignPanelObj.SetActive(false);
        }

        for (int i = 0; i < rankRecords.records.Count; i++)
        {
            GameObject rankButtonPrefab = Managers.Data.Instantiate($"UI/Sub/RankButton", rankPanelObj.transform);
            if (rankButtonPrefab != null)
            {
                Button button = rankButtonPrefab.GetComponent<Button>();
                button.gameObject.BindEvent(OnRankButtonClick);

                if (button != null)
                {
                    button.transform.Find("Ranking").GetComponent<TextMeshProUGUI>().text = $"{i + 1}";
                    button.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = $"{rankRecords.records[i].user_id}";
                    button.transform.Find("Accuracy").GetComponent<TextMeshProUGUI>().text = $"Accuracy: {rankRecords.records[i].score}";
                    button.onClick.AddListener(() => OnSongButtonClick(button.GetComponentInChildren<TextMeshProUGUI>().text));
                }
            }
            else
            {
                Debug.LogError($"Failed to load RankButton prefab");
            }
        }
    }
}
