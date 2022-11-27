using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
    [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;


    #region EditorCode

#if UNITY_EDITOR

    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    public void Init(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeGraph = nodeGraph;
        this.roomNodeType = roomNodeType;


        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
        
    }

    public void Draw(GUIStyle nodeStyle)
    {
        GUILayout.BeginArea(rect, nodeStyle);
        EditorGUI.BeginChangeCheck();

        if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
        {
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);

            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];

            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor
                || !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor
                || !roomNodeTypeList.list[selected].isBosRoom && roomNodeTypeList.list[selection].isBosRoom)
            {
                if (childRoomNodeIDList.Count > 0)
                {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                        RoomNodeSO child = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);

                        if (child != null)
                        {
                            RemoveChildIDFromRoomNode(child.id);

                            child.RemoveParentIDFromRoomNode(id);
                        }
                    }
                }
            }

        }

        if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(this);

            GUILayout.EndArea();

    }

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];

        for(int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if(roomNodeTypeList.list[i].displayInNodeGraphEditor)
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
        }

        return roomArray;
    }

    public void ProcessEvents(Event current)
    {
        switch (current.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(current);
                break;

            case EventType.MouseUp:
                ProcessMouseUpEvent(current);
                break;
            case EventType.MouseDrag:
                ProcessMouseDragEvent(current);
                break;
            default:
                break;
        }
    }

    private void ProcessMouseDownEvent(Event current)
    {
        if(current.button == 0)
        {
            ProcessLeftClickDownEvent();
        }
        else if(current.button == 1)
        {
            ProcessRightClickDownEvent(current);
        }
    }

    private void ProcessRightClickDownEvent(Event current)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, current.mousePosition);
    }

    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;
        
        isSelected = !isSelected;
    }

    private void ProcessMouseUpEvent(Event current)
    {
        if(current.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging)
            isLeftClickDragging = false;
    }

    private void ProcessMouseDragEvent(Event current)
    {
        if(current.button == 0)
        {
            ProcessLeftMouseDragEvent(current);
        }
    }

    private void ProcessLeftMouseDragEvent(Event current)
    {
        isLeftClickDragging = true;

        DragNode(current.delta);
        GUI.changed = true;
    }

    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    public bool AddChildIDToRoomNode(string childID)
    {
        if (IsChildRoomValid(childID))
        {
            childRoomNodeIDList.Add(childID);
            return true;
        }
        return false;
    }

    private bool IsChildRoomValid(string childID)
    {
        RoomNodeSO roomNode = roomNodeGraph.GetRoomNode(childID);

        bool isConnectedBossNodeAlready = false;
        foreach(RoomNodeSO node in roomNodeGraph.roomNodeList)
        {
            if(node.roomNodeType.isBosRoom && node.parentRoomNodeIDList.Count > 0)
                isConnectedBossNodeAlready = true;
        }

        if (roomNode.roomNodeType.isBosRoom && isConnectedBossNodeAlready)
            return false;

        if (roomNode.roomNodeType.isNone)
            return false;

        if (childRoomNodeIDList.Contains(childID))
            return false;

        if (id == childID)
            return false;

        if (parentRoomNodeIDList.Contains(childID))
            return false;

        if (roomNode.parentRoomNodeIDList.Count > 0)
            return false;

        if (roomNode.roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;

        if (!roomNode.roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;

        if (roomNode.roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
            return false;

        if (roomNode.roomNodeType.isEntrance)
            return false;

        if (!roomNode.roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
            return false;

        return true;

    }

    public bool AddParentIDToRoomNode(string parentID)
    {
        parentRoomNodeIDList.Add(parentID);
        return true;
    }

    public bool RemoveChildIDFromRoomNode(string ID)
    {
        if (childRoomNodeIDList.Contains(ID))
        {
            childRoomNodeIDList.Remove(ID);
            return true;
        }
        return false;
    }

    public bool RemoveParentIDFromRoomNode(string ID)
    {
        if (parentRoomNodeIDList.Contains(ID))
        {
            parentRoomNodeIDList.Remove(ID);
            return true;
        }
        return false;
    }
#endif

#endregion EditorCode
}
