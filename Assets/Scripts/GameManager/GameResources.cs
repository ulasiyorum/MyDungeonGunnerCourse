using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResources : MonoBehaviour
{
    private static GameResources instance;
    public static GameResources Instance
    {
        get
        {
            if(instance == null)
            {
                instance = Resources.Load<GameResources>("GameResources");
            }
            return instance;
        }
    }

    #region Header
    [Space(10)]
    [Header("DUNGEON")]
    #endregion Header
    #region Tooltip
    [Tooltip("Populate with the Dungeon RoomNodeTypeListSO")]
    #endregion Tooltip

    public RoomNodeTypeListSO roomNodeTypeList;
}
