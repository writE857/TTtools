using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

#if Ade_TT && !UNITY_EDITOR
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

namespace Ade_Framework
{
    public static class AdeDouyinCloudSave
    {
        public static void InitializeFromSettings(Action onReady = null)
        {
            AdeDouyinCloudSaveSettings settings = AdeDouyinCloudSaveSettings.Load();
            if (settings == null || !settings.EnableCloudSave)
            {
                onReady?.Invoke();
                return;
            }

            AdeCloudPlayerPrefs.InitDouyinCloud(settings.EnvId, settings.CollectionName, settings.DocumentKey, onReady);
        }
    }

    public static class AdeCloudPlayerPrefs
    {
        const string TypeInt = "int";
        const string TypeFloat = "float";
        const string TypeString = "string";

        public static void SetInt(string key, int value)
        {
#if Ade_TT && !UNITY_EDITOR
            SetCachedValue(key, TypeInt, value.ToString(CultureInfo.InvariantCulture));
            TT.PlayerPrefs.SetInt(key, value);
#else
            UnityEngine.PlayerPrefs.SetInt(key, value);
#endif
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
#if Ade_TT && !UNITY_EDITOR
            EnsureIndexLoaded();
            if (values.TryGetValue(key, out PrefValue cached) && int.TryParse(cached.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return parsed;
            }

            return TT.PlayerPrefs.GetInt(key, defaultValue);
#else
            return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
#endif
        }

        public static void SetFloat(string key, float value)
        {
#if Ade_TT && !UNITY_EDITOR
            SetCachedValue(key, TypeFloat, value.ToString("R", CultureInfo.InvariantCulture));
            TT.PlayerPrefs.SetFloat(key, value);
#else
            UnityEngine.PlayerPrefs.SetFloat(key, value);
#endif
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
#if Ade_TT && !UNITY_EDITOR
            EnsureIndexLoaded();
            if (values.TryGetValue(key, out PrefValue cached) && float.TryParse(cached.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }

            return TT.PlayerPrefs.GetFloat(key, defaultValue);
#else
            return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
#endif
        }

        public static void SetString(string key, string value)
        {
            string safeValue = value ?? string.Empty;
#if Ade_TT && !UNITY_EDITOR
            SetCachedValue(key, TypeString, safeValue);
            TT.PlayerPrefs.SetString(key, safeValue);
#else
            UnityEngine.PlayerPrefs.SetString(key, safeValue);
#endif
        }

        public static string GetString(string key, string defaultValue = "")
        {
#if Ade_TT && !UNITY_EDITOR
            EnsureIndexLoaded();
            if (values.TryGetValue(key, out PrefValue cached))
            {
                return cached.Value;
            }

            return TT.PlayerPrefs.GetString(key, defaultValue);
#else
            return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
#endif
        }

        public static bool HasKey(string key)
        {
#if Ade_TT && !UNITY_EDITOR
            EnsureIndexLoaded();
            return values.ContainsKey(key) || TT.PlayerPrefs.HasKey(key);
#else
            return UnityEngine.PlayerPrefs.HasKey(key);
#endif
        }

        public static void DeleteKey(string key)
        {
#if Ade_TT && !UNITY_EDITOR
            EnsureIndexLoaded();
            values.Remove(key);
            types.Remove(key);
            SaveLocalIndex();
            MarkLocalDirty();
            TT.PlayerPrefs.DeleteKey(key);
#else
            UnityEngine.PlayerPrefs.DeleteKey(key);
#endif
        }

        public static void DeleteAll()
        {
#if Ade_TT && !UNITY_EDITOR
            EnsureIndexLoaded();
            values.Clear();
            types.Clear();
            TT.PlayerPrefs.DeleteAll();
            SaveLocalIndex();
            MarkLocalDirty();
#else
            UnityEngine.PlayerPrefs.DeleteAll();
#endif
        }

        public static void Save()
        {
#if Ade_TT && !UNITY_EDITOR
            SaveLocalIndex();
            TT.PlayerPrefs.Save();
            FlushCloud();
#else
            UnityEngine.PlayerPrefs.Save();
#endif
        }

#if Ade_TT && !UNITY_EDITOR
        const string DocumentKeyField = "documentKey";
        const string PrefsJsonField = "prefsJson";
        const string UpdatedAtField = "updatedAt";
        const string LocalIndexKey = "__AdeCloudPlayerPrefs.Index";
        const string LocalUpdatedAtKey = "__AdeCloudPlayerPrefs.LocalUpdatedAtUtc";

        struct PrefValue
        {
            public string Type;
            public string Value;
        }

        static readonly Dictionary<string, PrefValue> values = new Dictionary<string, PrefValue>();
        static readonly Dictionary<string, string> types = new Dictionary<string, string>();
        static readonly List<Action> readyCallbacks = new List<Action>();

        static string documentKey = "project_player_prefs";
        static DouyinCloud cloud;
        static CloudDBCollection collection;
        static CloudDBDocument document;
        static bool indexLoaded;
        static bool initStarted;
        static bool initReady;
        static bool dirty;
        static bool flushInProgress;
        static bool flushAgain;
        static bool createInProgress;

        public static void InitDouyinCloud(string envId, string collectionName, string cloudDocumentKey = "project_player_prefs", Action onReady = null)
        {
            if (initReady)
            {
                onReady?.Invoke();
                return;
            }

            if (onReady != null)
            {
                readyCallbacks.Add(onReady);
            }

            if (initStarted)
            {
                return;
            }

            initStarted = true;
            documentKey = string.IsNullOrEmpty(cloudDocumentKey) ? "project_player_prefs" : cloudDocumentKey;

            if (string.IsNullOrEmpty(envId) || string.IsNullOrEmpty(collectionName))
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 抖音云环境 ID 或集合名为空，跳过云存档。");
                MarkReady();
                return;
            }

            EnsureIndexLoaded();

            DouyinCloud createdCloud;
            try
            {
                createdCloud = TT.CreateCloud();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 创建抖音云实例失败。\n" + exception);
                MarkReady();
                return;
            }

            InitDouyinCloudInternal(createdCloud, envId, collectionName);
        }

        public static void InitDouyinCloud(DouyinCloud douyinCloud, string envId, string collectionName, string cloudDocumentKey = "project_player_prefs", Action onReady = null)
        {
            if (initReady)
            {
                onReady?.Invoke();
                return;
            }

            if (onReady != null)
            {
                readyCallbacks.Add(onReady);
            }

            if (initStarted)
            {
                return;
            }

            initStarted = true;
            documentKey = string.IsNullOrEmpty(cloudDocumentKey) ? "project_player_prefs" : cloudDocumentKey;

            if (douyinCloud == null || string.IsNullOrEmpty(envId) || string.IsNullOrEmpty(collectionName))
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 抖音云实例、环境 ID 或集合名为空，跳过云存档。");
                MarkReady();
                return;
            }

            EnsureIndexLoaded();
            InitDouyinCloudInternal(douyinCloud, envId, collectionName);
        }

        static void InitDouyinCloudInternal(DouyinCloud douyinCloud, string envId, string collectionName)
        {
            try
            {
                cloud = douyinCloud;
                collection = cloud.CloudDb().GenDBCollection(envId, collectionName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 创建抖音云数据库失败。\n" + exception);
                MarkReady();
                return;
            }

            QueryCloudDocument(loadedDocument =>
            {
                if (loadedDocument == null)
                {
                    CreateCloudDocument();
                    return;
                }

                if (!TryBindCloudDocument(loadedDocument))
                {
                    Debug.LogWarning("[AdeCloudPlayerPrefs] 读取云存档文档 ID 失败。");
                    MarkReady();
                    return;
                }

                TryApplyCloudDocument(loadedDocument);
                if (dirty)
                {
                    FlushCloud();
                }

                MarkReady();
            }, response =>
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 查询云存档失败: " + BuildResponseMessage(response));
                MarkReady();
            });
        }

        static void MarkReady()
        {
            initReady = true;
            initStarted = false;

            Action[] callbacks = readyCallbacks.ToArray();
            readyCallbacks.Clear();
            for (int i = 0; i < callbacks.Length; i++)
            {
                callbacks[i]?.Invoke();
            }
        }

        static void SetCachedValue(string key, string type, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            EnsureIndexLoaded();
            values[key] = new PrefValue
            {
                Type = type,
                Value = value ?? string.Empty
            };
            types[key] = type;
            SaveLocalIndex();
            MarkLocalDirty();
        }

        static void EnsureIndexLoaded()
        {
            if (indexLoaded)
            {
                return;
            }

            indexLoaded = true;
            string index = TT.PlayerPrefs.GetString(LocalIndexKey, string.Empty);
            if (string.IsNullOrEmpty(index))
            {
                return;
            }

            string[] rows = index.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rows.Length; i++)
            {
                int separator = rows[i].IndexOf(':');
                if (separator <= 0 || separator >= rows[i].Length - 1)
                {
                    continue;
                }

                string type = rows[i].Substring(0, separator);
                string key = Decode(rows[i].Substring(separator + 1));
                if (string.IsNullOrEmpty(key) || !TT.PlayerPrefs.HasKey(key))
                {
                    continue;
                }

                types[key] = type;
                values[key] = new PrefValue
                {
                    Type = type,
                    Value = ReadLocalValue(key, type)
                };
            }
        }

        static void SaveLocalIndex()
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in types)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.Value);
                builder.Append(':');
                builder.Append(Encode(entry.Key));
            }

            TT.PlayerPrefs.SetString(LocalIndexKey, builder.ToString());
        }

        static string ReadLocalValue(string key, string type)
        {
            if (type == TypeInt)
            {
                return TT.PlayerPrefs.GetInt(key, 0).ToString(CultureInfo.InvariantCulture);
            }

            if (type == TypeFloat)
            {
                return TT.PlayerPrefs.GetFloat(key, 0f).ToString("R", CultureInfo.InvariantCulture);
            }

            return TT.PlayerPrefs.GetString(key, string.Empty);
        }

        static void QueryCloudDocument(Action<JsonData> onSuccess, Action<DouyinCloud.DBResponse> onFail)
        {
            if (collection == null)
            {
                onFail?.Invoke(null);
                return;
            }

            collection.Where(new Dictionary<string, object>
            {
                [DocumentKeyField] = documentKey
            }).Limit(1).Get(response =>
            {
                onSuccess?.Invoke(ExtractFirstDocument(response != null ? response.Data : null));
            }, response =>
            {
                onFail?.Invoke(response);
            });
        }

        static void CreateCloudDocument()
        {
            if (collection == null)
            {
                MarkReady();
                return;
            }

            if (createInProgress)
            {
                return;
            }

            createInProgress = true;
            JsonData payload = BuildCloudPayload();
            string payloadUpdatedAt = payload.OptGetString(UpdatedAtField, string.Empty);
            collection.Add(payload, response =>
            {
                createInProgress = false;
                QueryCloudDocument(loadedDocument =>
                {
                    if (loadedDocument != null && TryBindCloudDocument(loadedDocument))
                    {
                        TryMarkLocalSynced(payloadUpdatedAt);
                        dirty = false;
                    }
                    else
                    {
                        Debug.LogWarning("[AdeCloudPlayerPrefs] 云存档已创建，但回读文档 ID 失败。");
                    }

                    MarkReady();
                }, queryFail =>
                {
                    Debug.LogWarning("[AdeCloudPlayerPrefs] 创建后回读云存档失败: " + BuildResponseMessage(queryFail));
                    MarkReady();
                });
            }, response =>
            {
                createInProgress = false;
                Debug.LogWarning("[AdeCloudPlayerPrefs] 创建云存档失败: " + BuildResponseMessage(response));
                MarkReady();
            });
        }

        static bool TryBindCloudDocument(JsonData data)
        {
            string documentId = ExtractDocumentId(data);
            if (string.IsNullOrEmpty(documentId) || collection == null)
            {
                return false;
            }

            try
            {
                document = collection.Doc(documentId);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 绑定云存档文档失败。\n" + exception);
                return false;
            }
        }

        static JsonData ExtractFirstDocument(JsonData data)
        {
            data = NormalizeJsonData(data);
            if (data == null)
            {
                return null;
            }

            if (data.IsObject)
            {
                if (!string.IsNullOrEmpty(data.OptGetString("_id", null)) ||
                    !string.IsNullOrEmpty(data.OptGetString("id", null)) ||
                    data.OptGetObject("_id", null) != null ||
                    !string.IsNullOrEmpty(data.OptGetString(PrefsJsonField, null)))
                {
                    return data;
                }

                JsonData list = data.OptGetJsonArray("list", null);
                if (list != null && list.Count > 0)
                {
                    return ExtractFirstDocument(list[0]);
                }

                JsonData nested = data.OptGetObject("data", null);
                if (nested != null)
                {
                    return ExtractFirstDocument(nested);
                }
            }

            if (data.IsArray && data.Count > 0)
            {
                return ExtractFirstDocument(data[0]);
            }

            return null;
        }

        static JsonData NormalizeJsonData(JsonData data)
        {
            if (data == null)
            {
                return null;
            }

            if (!data.IsString)
            {
                return data;
            }

            try
            {
                return JsonMapper.ToObject(data.ToString());
            }
            catch
            {
                return null;
            }
        }

        static string ExtractDocumentId(JsonData data)
        {
            data = NormalizeJsonData(data);
            if (data == null)
            {
                return null;
            }

            if (data.IsObject)
            {
                JsonData idObject = data.OptGetObject("_id", null);
                if (idObject != null)
                {
                    string oid = idObject.OptGetString("$oid", null);
                    if (!string.IsNullOrEmpty(oid))
                    {
                        return oid;
                    }
                }

                string id = data.OptGetString("_id", null);
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                id = data.OptGetString("id", null);
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                JsonData nested = data.OptGetObject("data", null);
                if (nested != null)
                {
                    string nestedId = ExtractDocumentId(nested);
                    if (!string.IsNullOrEmpty(nestedId))
                    {
                        return nestedId;
                    }
                }

                JsonData list = data.OptGetJsonArray("list", null);
                if (list != null && list.Count > 0)
                {
                    return ExtractDocumentId(list[0]);
                }
            }

            if (data.IsArray && data.Count > 0)
            {
                return ExtractDocumentId(data[0]);
            }

            return null;
        }

        static void TryApplyCloudDocument(JsonData data)
        {
            string cloudUpdatedAt = ExtractUpdatedAt(data);
            if (!ShouldApplyCloudSnapshot(cloudUpdatedAt))
            {
                EnsureLocalUpdatedAt();
                dirty = HasTrackedData();
                return;
            }

            string prefsJson = ExtractPrefsJson(data);
            if (string.IsNullOrEmpty(prefsJson))
            {
                return;
            }

            try
            {
                ApplyPrefsJson(prefsJson);
                SaveLocalIndex();
                SetLocalUpdatedAt(string.IsNullOrEmpty(cloudUpdatedAt) ? GetCurrentUtcTimestamp() : cloudUpdatedAt);
                TT.PlayerPrefs.Save();
                dirty = false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdeCloudPlayerPrefs] 解析云存档失败。\n" + exception);
            }
        }

        static string ExtractPrefsJson(JsonData data)
        {
            data = NormalizeJsonData(data);
            if (data == null)
            {
                return null;
            }

            if (data.IsObject)
            {
                string direct = data.OptGetString(PrefsJsonField, null);
                if (!string.IsNullOrEmpty(direct))
                {
                    return direct;
                }

                JsonData nested = data.OptGetObject("data", null);
                if (nested != null)
                {
                    string nestedValue = ExtractPrefsJson(nested);
                    if (!string.IsNullOrEmpty(nestedValue))
                    {
                        return nestedValue;
                    }
                }

                JsonData list = data.OptGetJsonArray("list", null);
                if (list != null && list.Count > 0)
                {
                    return ExtractPrefsJson(list[0]);
                }
            }

            if (data.IsArray && data.Count > 0)
            {
                return ExtractPrefsJson(data[0]);
            }

            return null;
        }

        static string ExtractUpdatedAt(JsonData data)
        {
            data = NormalizeJsonData(data);
            if (data == null)
            {
                return null;
            }

            if (data.IsObject)
            {
                string direct = data.OptGetString(UpdatedAtField, null);
                if (!string.IsNullOrEmpty(direct))
                {
                    return direct;
                }

                JsonData nested = data.OptGetObject("data", null);
                if (nested != null)
                {
                    string nestedValue = ExtractUpdatedAt(nested);
                    if (!string.IsNullOrEmpty(nestedValue))
                    {
                        return nestedValue;
                    }
                }

                JsonData list = data.OptGetJsonArray("list", null);
                if (list != null && list.Count > 0)
                {
                    return ExtractUpdatedAt(list[0]);
                }
            }

            if (data.IsArray && data.Count > 0)
            {
                return ExtractUpdatedAt(data[0]);
            }

            return null;
        }

        static void ApplyPrefsJson(string prefsJson)
        {
            JsonData root = JsonMapper.ToObject(prefsJson);
            JsonData entries = root.OptGetJsonArray("entries", null);
            HashSet<string> staleKeys = new HashSet<string>(types.Keys);
            values.Clear();
            types.Clear();

            if (entries == null)
            {
                foreach (string staleKey in staleKeys)
                {
                    TT.PlayerPrefs.DeleteKey(staleKey);
                }

                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                JsonData entry = NormalizeJsonData(entries[i]);
                if (entry == null || !entry.IsObject)
                {
                    continue;
                }

                string key = entry.OptGetString("key", string.Empty);
                string type = entry.OptGetString("type", TypeString);
                string value = entry.OptGetString("value", string.Empty);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                staleKeys.Remove(key);
                values[key] = new PrefValue
                {
                    Type = type,
                    Value = value
                };
                types[key] = type;
                WriteLocalValue(key, type, value);
            }

            foreach (string staleKey in staleKeys)
            {
                TT.PlayerPrefs.DeleteKey(staleKey);
            }
        }

        static void WriteLocalValue(string key, string type, string value)
        {
            if (type == TypeInt && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                TT.PlayerPrefs.SetInt(key, intValue);
                return;
            }

            if (type == TypeFloat && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                TT.PlayerPrefs.SetFloat(key, floatValue);
                return;
            }

            TT.PlayerPrefs.SetString(key, value ?? string.Empty);
        }

        static void FlushCloud()
        {
            if (!dirty || document == null)
            {
                return;
            }

            if (flushInProgress)
            {
                flushAgain = true;
                return;
            }

            flushInProgress = true;
            flushAgain = false;
            JsonData payload = BuildCloudPayload();
            string payloadUpdatedAt = payload.OptGetString(UpdatedAtField, string.Empty);

            document.Set(payload, response =>
            {
                flushInProgress = false;
                TryMarkLocalSynced(payloadUpdatedAt);
                dirty = false;
                if (flushAgain)
                {
                    dirty = true;
                    FlushCloud();
                }
            }, response =>
            {
                flushInProgress = false;
                Debug.LogWarning("[AdeCloudPlayerPrefs] 保存云存档失败: " + BuildResponseMessage(response));
            });
        }

        static JsonData BuildCloudPayload()
        {
            string updatedAt = EnsureLocalUpdatedAt();
            return new JsonData
            {
                [DocumentKeyField] = documentKey,
                [PrefsJsonField] = BuildPrefsJson(),
                [UpdatedAtField] = updatedAt
            };
        }

        static string BuildPrefsJson()
        {
            JsonData root = new JsonData();
            JsonData entries = JsonData.NewJsonArray();
            root["version"] = 1;
            root["entries"] = entries;

            foreach (KeyValuePair<string, string> entry in types)
            {
                JsonData item = new JsonData
                {
                    ["key"] = entry.Key,
                    ["type"] = entry.Value,
                    ["value"] = values.TryGetValue(entry.Key, out PrefValue value) ? value.Value : ReadLocalValue(entry.Key, entry.Value)
                };
                ((IList)entries).Add(item);
            }

            return root.ToJson();
        }

        static bool ShouldApplyCloudSnapshot(string cloudUpdatedAt)
        {
            if (!HasTrackedData())
            {
                return true;
            }

            DateTime localUtc = ParseUtcTimestamp(TT.PlayerPrefs.GetString(LocalUpdatedAtKey, string.Empty));
            DateTime cloudUtc = ParseUtcTimestamp(cloudUpdatedAt);
            return localUtc != DateTime.MinValue && cloudUtc != DateTime.MinValue && cloudUtc >= localUtc;
        }

        static bool HasTrackedData()
        {
            return types.Count > 0 || values.Count > 0;
        }

        static void MarkLocalDirty()
        {
            SetLocalUpdatedAt(GetCurrentUtcTimestamp());
            dirty = true;
        }

        static string EnsureLocalUpdatedAt()
        {
            string updatedAt = TT.PlayerPrefs.GetString(LocalUpdatedAtKey, string.Empty);
            if (!string.IsNullOrEmpty(updatedAt) && ParseUtcTimestamp(updatedAt) != DateTime.MinValue)
            {
                return updatedAt;
            }

            updatedAt = GetCurrentUtcTimestamp();
            SetLocalUpdatedAt(updatedAt);
            return updatedAt;
        }

        static void SetLocalUpdatedAt(string updatedAt)
        {
            TT.PlayerPrefs.SetString(LocalUpdatedAtKey, updatedAt ?? string.Empty);
        }

        static void TryMarkLocalSynced(string syncedUpdatedAt)
        {
            if (string.IsNullOrEmpty(syncedUpdatedAt))
            {
                return;
            }

            string currentUpdatedAt = TT.PlayerPrefs.GetString(LocalUpdatedAtKey, string.Empty);
            DateTime currentUtc = ParseUtcTimestamp(currentUpdatedAt);
            DateTime syncedUtc = ParseUtcTimestamp(syncedUpdatedAt);
            if (currentUtc == DateTime.MinValue || syncedUtc == DateTime.MinValue || syncedUtc >= currentUtc)
            {
                SetLocalUpdatedAt(syncedUpdatedAt);
                TT.PlayerPrefs.Save();
            }
        }

        static string GetCurrentUtcTimestamp()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        static DateTime ParseUtcTimestamp(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DateTime.MinValue;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed)
                ? parsed.ToUniversalTime()
                : DateTime.MinValue;
        }

        static string BuildResponseMessage(DouyinCloud.DBResponse response)
        {
            return response == null ? "null response" : "statusCode=" + response.StatusCode + ", errMsg=" + response.ErrMsg;
        }

        static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        static string Decode(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }
#else
        public static void InitDouyinCloud(string envId, string collectionName, string cloudDocumentKey = "project_player_prefs", Action onReady = null)
        {
            onReady?.Invoke();
        }
#endif
    }
}
