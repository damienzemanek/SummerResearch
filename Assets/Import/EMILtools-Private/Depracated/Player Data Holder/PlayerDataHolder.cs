using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DesignPatterns.CreationalPatterns;
using Sirenix.OdinInspector;

public class PlayerDataHolder : Singleton<PlayerDataHolder>
{
    [SerializeField, InlineEditor] PlayerData _data;
    public PlayerData data { get => _data; set => _data = value; }
}
