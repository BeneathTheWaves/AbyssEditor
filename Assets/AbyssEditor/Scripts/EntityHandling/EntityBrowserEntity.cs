using UnityEngine;

public class EntityBrowserEntity : EntityBrowserEntryBase
{
    public EntityData EntityData { get; }
    public override string Name => EntityData.Name;

    public override Sprite Sprite => EntityDatabase.main.defaultEntitySprite;

    public EntityBrowserEntity(string path, EntityData entity) : base(path)
    {
        EntityData = entity;
    }

    public override void OnInteract()
    {
        DebugOverlay.LogError($"Failed to spawn entity by Class ID '{EntityData.ClassId}' (behavior not implemented yet!");
    }
}