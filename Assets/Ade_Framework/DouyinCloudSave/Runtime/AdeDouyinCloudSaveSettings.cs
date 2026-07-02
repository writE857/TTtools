using UnityEngine;

namespace Ade_Framework
{
    public class AdeDouyinCloudSaveSettings : ScriptableObject
    {
        public const string ResourceName = "AdeDouyinCloudSaveSettings";
        public const string AssetPath = "Assets/Ade_Framework/DouyinCloudSave/Resources/AdeDouyinCloudSaveSettings.asset";

        [SerializeField] bool enableCloudSave = true;
        [SerializeField] string envId = "env-qyvrRQrMt";
        [SerializeField] string collectionName = "demo";
        [SerializeField] string documentKey = "project_player_prefs";

        public bool EnableCloudSave => enableCloudSave;
        public string EnvId => envId;
        public string CollectionName => collectionName;
        public string DocumentKey => documentKey;

        public static AdeDouyinCloudSaveSettings Load()
        {
            AdeDouyinCloudSaveSettings settings = Resources.Load<AdeDouyinCloudSaveSettings>(ResourceName);
            if (settings != null)
            {
                return settings;
            }

            AdeDouyinCloudSaveSettings fallback = CreateInstance<AdeDouyinCloudSaveSettings>();
            fallback.name = ResourceName;
            return fallback;
        }
    }
}
