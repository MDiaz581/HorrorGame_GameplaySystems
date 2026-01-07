using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Character Selection/Character")]
public class Character : ScriptableObject
{
    [SerializeField] private string characterName = default;
    [SerializeField] private GameObject previewCharacterPrefab = default;
    [SerializeField] private GameObject gameplayCharacterPrefab = default;

    public string CharacterName => characterName;
    public GameObject PreviewCharacterPrefab => previewCharacterPrefab;
    public GameObject GameplayCharacterPrefab => gameplayCharacterPrefab;

}
