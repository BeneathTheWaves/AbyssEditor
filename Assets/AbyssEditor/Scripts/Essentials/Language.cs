using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace AbyssEditor.Scripts
{
    /// <summary>
    /// This mirrors Subnautica's language system (but is more basic)
    /// TODO: This class is more of a skeleton for now and doesnt do anything beyond loading the english json into the dictionary
    /// </summary>
    public class Language : MonoBehaviour
    {
        private static string localizationFolder = Path.Combine(Application.streamingAssetsPath, "Localization");
        private const string DEFAULT_LANGUAGE = "English";
        
        public static Language main;
        
        [SerializeField] private readonly List<string> availableLanguages = new();
        
        //We will need another Dictionary in the future when we have multiple languages loaded but those languages don't have every key
        [SerializeField] private Dictionary<string, string> languageKeys;

        private void Awake()
        {
            if (main != null)
            {
                Debug.LogError($"{nameof(Language)} already exists! Do not try to instantiate two");
            }
            main = this;
            ParseAvailableLanguages();
            ParseLanguageFile(DEFAULT_LANGUAGE);
        }
        
        private void ParseAvailableLanguages()
        {
            //for now im just leaving this as a stub, but we should parse the language folder and check what is available
            availableLanguages.Add(DEFAULT_LANGUAGE);
        }

        private void ParseLanguageFile(string languageFileName)
        {
            string path = Path.Combine(localizationFolder, $"{languageFileName}.json");
            
            string json = File.ReadAllText(path);

            languageKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
        
        public string Get(string key)
        {
            return languageKeys.GetValueOrDefault(key, key);
        }


        public event Action OnLanguageChanged
        {
            add => Language.main.OnLanguageChanged += value;
            remove => Language.main.OnLanguageChanged -= value;
        }
    }
}
