using UnityEngine;

[CreateAssetMenu(fileName = "RoomTracker", menuName = "Scriptable Objects/RoomTracker")]
public class RoomTracker : ScriptableObject
{
    [System.NonSerialized]
    public GameObject currentRoom;
}
