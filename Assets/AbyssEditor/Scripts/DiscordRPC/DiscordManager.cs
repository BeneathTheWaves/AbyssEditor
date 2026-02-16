using System;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.VoxelTech;
using Discord;
using UnityEngine;

namespace AbyssEditor.Scripts.DiscordRPC
{
    public class DiscordManager : MonoBehaviour
    {
        // Your Discord Application Client ID
        private Discord.Discord discord;
        private long startTimestamp;

        private void Awake()
        {
            discord = new Discord.Discord(1472757634426994904, (UInt64)Discord.CreateFlags.NoRequireDiscord);
        }

        private void Start()
        {
            startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            InvokeRepeating(nameof(UpdateCallbacks), 0f, 5f);
            InvokeRepeating(nameof(UpdateActivity), 0f, 4f);
        }

        private void UpdateCallbacks()
        {
            discord?.RunCallbacks();
        }

        private void OnApplicationQuit()
        {
            discord?.Dispose();
        }

        private void UpdateActivity()
        {
            ActivityManager activityManager = discord.GetActivityManager();

            int numBatchesLoaded = VoxelMetaspace.metaspace.meshes.Count;
            
            string descriptionKey = "DiscordGameSDK_State";
            if (numBatchesLoaded != 1) descriptionKey = "DiscordGameSDK_State_Plural";

            Activity activity = new Activity
            {
                Details = Language.main.Get("DiscordGameSDK_Details"),
                State = Language.main.Get(descriptionKey).Replace("%numbatches%", ""+VoxelMetaspace.metaspace.meshes.Count),
                Timestamps =
                {
                    Start = startTimestamp,
                },
                Assets =
                {
                    LargeImage = "voideditoricon", //Asset key is creating within discord dev portal, do not change this unless it's been update there
                    LargeText = Language.main.Get("Title"),
                },
                Instance = true
            };

            activityManager.UpdateActivity(activity, result => { });
        }
    }
}
