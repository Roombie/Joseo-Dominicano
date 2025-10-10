using UnityEngine;

public class PlayerRoomTracker : MonoBehaviour
{
    public RoomTracker playerCurrentRoom;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D collision)
    {
       //Debug.Log("Triggered with: " + collision.gameObject.name);

        if (collision.gameObject.layer == LayerMask.NameToLayer("RoomsID"))
        {
           //Debug.Log("Player entered room: " + collision.gameObject.name);

            playerCurrentRoom.currentRoom = collision.gameObject;
           //Debug.Log("Gameobject assigned to currentRoom: " + playerCurrentRoom.currentRoom);
        }

    }
}
