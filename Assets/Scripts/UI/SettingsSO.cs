using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SettingsSO", menuName = "Scripts/UI/SettingsSO")]
public class SettingsSO : ScriptableObject
{
    [Header("Game Settings")]
    public float timer = 300; // Change this to something with difficulty levels

    [Header("Sound Settings")]
    public float generalVolume = 1f;
    public float carVolume = 1f;
    public float menuVolume = 1f;
    public float musicVolume = 1f;

}
