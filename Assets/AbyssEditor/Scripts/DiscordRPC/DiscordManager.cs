using System;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.VoxelTech;
using DiscordRPC;
using DiscordRPC.Message;
using UnityEngine;

namespace AbyssEditor.Scripts.DiscordRPC
{
    public class DiscordManager : MonoBehaviour
    {
        private const string DISCORD_APP_ID = "1472757634426994904";
        private bool clientConnected = false;
        private static DiscordRpcClient client;

        private void Awake()
        {
            client = new DiscordRpcClient(DISCORD_APP_ID);
            client.OnReady += (sender, readyMessage) => OnClientReady(readyMessage); 
        }

        private void Start()
        {
            client.Initialize();
            InvokeRepeating(nameof(UpdateActivity), 0, 4f);
        }

        private void OnClientReady(ReadyMessage msg)
        {
            Debug.Log($"Connected to discord with user {msg.User.Username}! Updating status...");
            clientConnected = true;
        }

        private void UpdateActivity()
        {
            if (!clientConnected) return;
            
            client.SetPresence(new RichPresence()
            {
                Name = Language.main.Get("Title"),
                Details = Language.main.Get("DiscordGameSDK_Details"),
                State = Language.main.Get("DiscordGameSDK_State"),
                Assets = new Assets()
                {
                    LargeImageKey = "voideditoricon", //Asset key is creating within discord dev portal, do not change this unless it's been update there
                    LargeImageText = Language.main.Get("Title"),
                    SmallImageKey = "btwlogo",
                    SmallImageUrl = "https://discord.gg/jKdBdPD46h",
                },
                Buttons = new Button[]
                {
                    new Button() { Label = "Join Discord", Url = "https://discord.gg/jKdBdPD46h" },
                }
            });
        }
        
        private void OnApplicationQuit()
        {
            client.Dispose();
        }
    }
}
