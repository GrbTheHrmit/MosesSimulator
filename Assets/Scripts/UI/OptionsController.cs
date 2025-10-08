using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsController : MonoBehaviour
{
    private static OptionsController _instance;
    public static OptionsController Instance
    {
        get { return _instance; }

    }

    public SettingsSO optionsSettings;

    private bool isPlaying = false;

    private void Start()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }


    private void StartGame()
    {
        isPlaying = true;
    }
}
