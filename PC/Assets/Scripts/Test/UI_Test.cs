using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UI_Test : MonoBehaviour
{
    void Start()
    {
        // 옵션창 생성
        Managers.ManagerInstance.AddComponent<OutGameUIController>().ShowPopupUI<UI_Option>();
    }

}
