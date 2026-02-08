using System.Collections.Generic;
using AbyssEditor.Scripts.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace AbyssEditor.Scripts.EntityHandling
{
    public class EntityDatabase : MonoBehaviour
    {
        public static EntityDatabase main;
        public Sprite folderSprite;
        public Sprite defaultEntitySprite;

        // Key: class id
        private Dictionary<string, EntityData> entities = new Dictionary<string, EntityData>();

        public TextAsset[] vanillaEntityJsonFiles;

        // Key: path
        private Dictionary<string, EntityBrowserFolder> folders = new Dictionary<string, EntityBrowserFolder>();
        private Dictionary<string, EntityBrowserEntity> entityBrowserEntries = new Dictionary<string, EntityBrowserEntity>();

        public EntityBrowserFolder RootFolder { get; } = new EntityBrowserFolder(string.Empty);

        public IReadOnlyCollection<EntityBrowserEntity> AllEntitiesInBrowser => entityBrowserEntries.Values;
        public IReadOnlyCollection<EntityData> AllEntities => entities.Values;
        public IReadOnlyCollection<EntityBrowserFolder> AllFolders => folders.Values;

        private void Awake()
        {
            main = this;

            foreach (var textAsset in vanillaEntityJsonFiles)
            {
                RegisterEntitiesFromJson(textAsset.text);
            }
        }

        public void RegisterEntities(List<EntityData> newEntities)
        {
            foreach (var entity in newEntities)
            {
                entities.Add(entity.ClassId, entity);
            }
        }

        public void RegisterEntitiesFromJson(string jsonData)
        {
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            foreach (var entity in data)
            {
                var entityData = new EntityData(entity.Key, entity.Value);
                entityData.SetEntityPreview(new BasicEntityPreview(entityData));
                entities.Add(entity.Key, entityData);
            }
            RebuildFolderStructure();
            DebugOverlay.LogMessage($"Loaded {data.Count} entities from json data into {folders.Count} folders!");
        }

        public bool TryGetEntity(string entityClassId, out EntityData entity)
        {
            return entities.TryGetValue(entityClassId, out entity);
        }

        public bool TryGetFolder(string path, out EntityBrowserFolder folder)
        {
            return folders.TryGetValue(path, out folder);
        }

        private void RebuildFolderStructure()
        {
            folders = new Dictionary<string, EntityBrowserFolder>();
            entityBrowserEntries = new Dictionary<string, EntityBrowserEntity>();

            foreach (var entity in entities)
            {
                var path = entity.Value.Path;
                var containingFolderPath = PathUtils.GetParentDirectory(path);
                var folder = EnsureFolderExists(containingFolderPath);
                var browserEntry = new EntityBrowserEntity(path, entity.Value);
                entityBrowserEntries.Add(path, browserEntry);
                folder.Subentries.Add(browserEntry);
            }

            foreach (var folder in folders.Values)
            {
                folder.SortSubentries();
            }
        }

        private EntityBrowserFolder EnsureFolderExists(string path)
        {
            if (path == null) path = string.Empty;
            if (folders.TryGetValue(path, out var folder))
            {
                return folder;
            }
            var containingFolder = PathUtils.GetParentDirectory(path);
            EntityBrowserFolder parentFolder;
            if (string.IsNullOrEmpty(containingFolder))
            {
                parentFolder = RootFolder;
            }
            else if (!folders.TryGetValue(containingFolder, out parentFolder))
            {
                parentFolder = EnsureFolderExists(containingFolder);
            }
            var newFolder = new EntityBrowserFolder(path);
            if (parentFolder != null) parentFolder.Subentries.Add(newFolder);
            folders.Add(path, newFolder);
            return newFolder;
        }
    }
}