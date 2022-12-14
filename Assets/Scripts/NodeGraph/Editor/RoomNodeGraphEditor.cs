using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.Collections.Generic;

public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle;
    private GUIStyle selectedStyle;

    private static RoomNodeGraphSO currentRoomNodeGraph;

    private RoomNodeSO currentRoomNode = null;
    private RoomNodeTypeListSO roomNodeTypeList;

    private Vector2 graphOffset;
    private Vector2 graphDrag;

    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;
    private const int nodePadding = 25;
    private const int nodeBorder = 12;
    private const float connectingLineWidth = 3f;
    private const float connectingLineArrowSize = 6f;

    private const float gridLarge = 100f;
    private const float gridSmall = 25f;

    private void OnEnable()
    {
        Selection.selectionChanged += InspectorSelectionChanged;

        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);


        selectedStyle = new GUIStyle();
        selectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        selectedStyle.normal.textColor = Color.white;
        selectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        selectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);


        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    private void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

        if(roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }


    [MenuItem("Room Node Graph Editor", menuItem = "Window/DungeonEditor/Room Node Graph Editor")]
    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    [OnOpenAsset(0)] // 0 => Calling order
    public static bool OnDoubleClickAsset(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;
        if(roomNodeGraph == null)
            return false;

        OpenWindow();
        currentRoomNodeGraph = roomNodeGraph;
        return true;
    }

    private void OnGUI()
    {
        if (currentRoomNodeGraph != null)
        {
            DrawBackgroundGrid(gridSmall, .2f, Color.gray);
            DrawBackgroundGrid(gridLarge, .3f, Color.gray);

            DrawDraggedLine();

            ProcessEvents(Event.current);

            DrawRoomNodeConnections();

            DrawRoomNodes();
        }

        if (GUI.changed)
            Repaint();
    }

    private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor)
    {
        int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
        int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);
        

        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b,gridOpacity);

        graphOffset += graphDrag * .5f;


        Vector3 gridOffset = new Vector3(graphOffset.x % gridSize, graphOffset.y % gridSize, 0);
        
        for(int i = 0; i<verticalLineCount; i++)
        {
            Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0) + gridOffset, 
                new Vector3(gridSize * i, position.height + gridSize, 0) + gridOffset);
        }
        for (int i = 0; i < verticalLineCount; i++)
        {
            Handles.DrawLine(new Vector3(-gridSize, gridSize * i, 0) + gridOffset,
                new Vector3(position.width + gridSize, gridSize * i , 0) + gridOffset);
        }

        Handles.color = Color.white;
    }

    private void DrawDraggedLine()
    {
        if(currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center,
                currentRoomNodeGraph.linePosition,
                currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center,
                currentRoomNodeGraph.linePosition, Color.green,null,connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {
        graphDrag = Vector2.zero;

        if(currentRoomNode == null || !currentRoomNode.isLeftClickDragging)
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        if(currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
            ProcessRoomNodeGraphEvents(currentEvent);
        else
            currentRoomNode.ProcessEvents(currentEvent);
    }

    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for(int i = currentRoomNodeGraph.roomNodeList.Count-1; i >= 0; i--)
        {
            if(currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
                return currentRoomNodeGraph.roomNodeList[i];
        }

        return null;
    }



    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;
            default:
                break;
        }
    }
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if(currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

            if(roomNode != null)
            {
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildIDToRoomNode(roomNode.id))
                    roomNode.AddParentIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
            }


            ClearLineDrag();
        }
    }
    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }

    private void DrawRoomNodeConnections()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.childRoomNodeIDList.Count > 0)
            {
                foreach(string id in roomNode.childRoomNodeIDList)
                {
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(id))
                    {

                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[id]);
                        GUI.changed = true;
                    }
                }
            }
        }
    }
    private void DrawConnectionLine(RoomNodeSO parent, RoomNodeSO child)
    {
        Vector2 start = parent.rect.center;
        Vector2 end = child.rect.center;

        Vector2 mid = (end + start) / 2f;
        Vector2 direction = end - start;

        Vector2 arrowTailPoint1 = mid - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPoint2 = mid + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;

        Vector2 arrowHeadPoint = mid + direction.normalized * connectingLineArrowSize;

        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, 
            arrowTailPoint1, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint,
            arrowTailPoint2, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(start, end, start, end, Color.white, null, connectingLineWidth);


        GUI.changed = true;
    }
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if(currentEvent.button == 1)
        {
            ProcessRightMouseDragEvent(currentEvent);
        }
        else if(currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent.delta);
        }
    }

    private void ProcessLeftMouseDragEvent(Vector2 delta)
    {
        graphDrag = delta;

        for(int i = 0; i<currentRoomNodeGraph.roomNodeList.Count; i++)
        {
            currentRoomNodeGraph.roomNodeList[i].DragNode(delta);
        }

        GUI.changed = true;
    }

    private void ProcessRightMouseDragEvent(Event currentEvent)
    {
        if(currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
            GUI.changed = true;
        }
    }
    private void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if(currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }
        else if(currentEvent.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }
    private void ClearAllSelectedRoomNodes()
    {
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.isSelected = false;
                GUI.changed = true;
            }
        }
    }

    private void SelectAllRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.isSelected = true;
        }
        GUI.changed = true;
    }

    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Links"), false, DeleteSelectedLinks);
        menu.AddItem(new GUIContent("Delete Selected Nodes"), false, DeleteSelectedNodes);

        menu.ShowAsContext();
    }

    private void DeleteSelectedLinks()
    {
        foreach(RoomNodeSO node in currentRoomNodeGraph.roomNodeList)
        {
            if(node.isSelected && node.childRoomNodeIDList.Count > 0)
            {
                for(int i = node.childRoomNodeIDList.Count-1; i>=0; i--)
                {
                    RoomNodeSO child = currentRoomNodeGraph.GetRoomNode(node.childRoomNodeIDList[i]);

                    if(child != null && child.isSelected)
                    {
                        node.RemoveChildIDFromRoomNode(child.id);

                        child.RemoveParentIDFromRoomNode(node.id);
                    }
                }
            }
        }

        ClearAllSelectedRoomNodes();
    }

    private void DeleteSelectedNodes()
    {
        Queue<RoomNodeSO> deletionQueue = new Queue<RoomNodeSO>();
        foreach(RoomNodeSO node in currentRoomNodeGraph.roomNodeList)
        {
            if(node.isSelected && !node.roomNodeType.isEntrance)
            {
                deletionQueue.Enqueue(node);

                foreach (string childID in node.childRoomNodeIDList)
                {
                    RoomNodeSO child = currentRoomNodeGraph.GetRoomNode(childID);
                    if (child != null)
                    {
                        child.RemoveParentIDFromRoomNode(node.id);
                    }
                }

                foreach(string parentID in node.parentRoomNodeIDList)
                {
                    RoomNodeSO parent = currentRoomNodeGraph.GetRoomNode(parentID);

                    if(parent != null)
                    {
                        parent.RemoveChildIDFromRoomNode(node.id);
                    }
                }
            }

        }

        while (deletionQueue.Count > 0)
        {
            RoomNodeSO nodeToDelete = deletionQueue.Dequeue();

            currentRoomNodeGraph.roomNodeDictionary.Remove(nodeToDelete.id);

            currentRoomNodeGraph.roomNodeList.Remove(nodeToDelete);

            DestroyImmediate(nodeToDelete,true);

            AssetDatabase.SaveAssets();
        }

    }

    private void CreateRoomNode(object mousePositionObject)
    {
        if(currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200, 200), roomNodeTypeList.list.Find(x => x.isEntrance));
        }

        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }

    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        roomNode.Init(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);

        AssetDatabase.SaveAssets();

        currentRoomNodeGraph.OnValidate();
    }


    private void DrawRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
                roomNode.Draw(selectedStyle);
            else
                roomNode.Draw(roomNodeStyle);
        }

        GUI.changed = true;
    }
}
