using UnityEngine;

[CreateAssetMenu(menuName="Collectibles/Trash Item")]
public class TrashItemSO : ScriptableObject
{
    [SerializeField] private string id = System.Guid.NewGuid().ToString();
    [SerializeField, Min(0f)] private float weightKg = 1f;
    [SerializeField, Min(0)]  private int worth = 1;
    [SerializeField, Min(0)] private int pickUpSpace = 1;

    public string Id => id;
    public float WeightKg => weightKg;
    public int Worth => worth;
    public int PickUpSpace => pickUpSpace;
}