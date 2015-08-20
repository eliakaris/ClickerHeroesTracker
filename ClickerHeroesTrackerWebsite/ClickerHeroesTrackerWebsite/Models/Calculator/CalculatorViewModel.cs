﻿namespace ClickerHeroesTrackerWebsite.Models.Calculator
{
    using Upload;
    using SaveData;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;

    public class CalculatorViewModel
    {
        private static readonly JsonSerializer serializer = CreateSerializer();

        public CalculatorViewModel(string encodedSaveData, string userId)
        {
            // Decode the save
            var jsonData = DecodeSaveData(encodedSaveData);
            if (jsonData == null)
            {
                return;
            }

            // Deserialize the save
            var savedGame = DeserializeSavedGame(jsonData);
            if (savedGame == null)
            {
                return;
            }

            var userSettings = new UserSettings(userId);
            userSettings.Fill();

            // Finally, populate the view models
            this.IsPermitted = true;
            this.IsValid = true;
            this.UploadTime = DateTime.UtcNow;

            this.AncientLevelSummaryViewModel = new AncientLevelSummaryViewModel(savedGame.AncientsData);
            ////this.HeroLevelSummaryViewModel = new HeroLevelSummaryViewModel(savedGame.HeroesData);
            this.ComputedStatsViewModel = new ComputedStatsViewModel(savedGame);
            this.SuggestedAncientLevelsViewModel = new SuggestedAncientLevelsViewModel(
                this.AncientLevelSummaryViewModel.AncientLevels,
                this.ComputedStatsViewModel.OptimalLevel,
                userSettings);
        }

        public CalculatorViewModel(int uploadId, string userId)
        {
            var userSettings = new UserSettings(userId);
            userSettings.Fill();

            using (var command = new DatabaseCommand("GetUploadDetails"))
            {
                command.AddParameter("@UploadId", uploadId);

                var reader = command.ExecuteReader();

                // General upload data
                if (reader.Read())
                {
                    var uploadUserId = (string)reader["UserId"];
                    var uploadTime = (DateTime)reader["UploadTime"];

                    this.IsPermitted = GetIsPermitted(userId, uploadUserId);
                    this.UploadTime = uploadTime;
                }
                else
                {
                    return;
                }

                if (!reader.NextResult())
                {
                    return;
                }

                // Get ancient levels
                this.AncientLevelSummaryViewModel = new AncientLevelSummaryViewModel(reader);

                if (!reader.NextResult())
                {
                    return;
                }

                this.ComputedStatsViewModel = new ComputedStatsViewModel(reader);

                this.SuggestedAncientLevelsViewModel = new SuggestedAncientLevelsViewModel(
                    this.AncientLevelSummaryViewModel.AncientLevels,
                    this.ComputedStatsViewModel.OptimalLevel,
                    userSettings);

                this.IsValid = true;
            }
        }

        public bool IsPermitted { get; private set; }

        public bool IsValid { get; private set; }

        public DateTime UploadTime { get; private set; }

        public AncientLevelSummaryViewModel AncientLevelSummaryViewModel { get; private set; }

        ////public HeroLevelSummaryViewModel HeroLevelSummaryViewModel { get; private set; }

        public ComputedStatsViewModel ComputedStatsViewModel { get; private set; }

        public SuggestedAncientLevelsViewModel SuggestedAncientLevelsViewModel { get; private set; }

        private static JsonSerializer CreateSerializer()
        {
            var settings = new JsonSerializerSettings();
            settings.Error += (sender, args) =>
            {
                // Just swallow
                args.ErrorContext.Handled = true;
            };

            return JsonSerializer.Create(settings);
        }

        internal static byte[] DecodeSaveData(string encodedSaveData)
        {
            const string AntiCheatCode = "Fe12NAfA3R6z4k0z";
            var antiCheatCodeIndex = encodedSaveData.IndexOf(AntiCheatCode);
            if (antiCheatCodeIndex == -1)
            {
                // Couldn't find anti-cheat
                return null;
            }

            // Remove every other character, AKA "unsprinkle"
            var unsprinkledChars = new char[antiCheatCodeIndex / 2];
            for (var i = 0; i < antiCheatCodeIndex; i += 2)
            {
                unsprinkledChars[i / 2] = encodedSaveData[i];
            }

            // Validation
            const string Salt = "af0ik392jrmt0nsfdghy0";
            var expectedHashStart = antiCheatCodeIndex + AntiCheatCode.Length;
            var saltedChars = new char[unsprinkledChars.Length + Salt.Length];
            unsprinkledChars.CopyTo(saltedChars, 0);
            Salt.CopyTo(0, saltedChars, unsprinkledChars.Length, Salt.Length);
            using (MD5 md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(saltedChars));
                var actualHash = new StringBuilder(data.Length * 2);
                for (int i = 0; i < data.Length; i++)
                {
                    var expectedHashPartIndex = expectedHashStart + (i * 2);
                    var actualHashPart = data[i].ToString("x2");
                    if (actualHashPart[0] != encodedSaveData[expectedHashPartIndex]
                        || actualHashPart[1] != encodedSaveData[expectedHashPartIndex + 1])
                    {
                        // Hash didn't match
                        return null;
                    }
                }
            }

            // Decode and return
            return Convert.FromBase64CharArray(unsprinkledChars, 0, unsprinkledChars.Length);
        }

        internal static SavedGame DeserializeSavedGame(byte[] saveData)
        {
            using (var stream = new MemoryStream(saveData))
            {
                using (var reader = new StreamReader(stream))
                {
                    return serializer.Deserialize<SavedGame>(new JsonTextReader(reader));
                }
            }
        }

        private static bool GetIsPermitted(string userId, string uploadUserId)
        {
            if (userId == uploadUserId)
            {
                return true;
            }

            var uploadUserSettings = new UserSettings(uploadUserId);
            uploadUserSettings.Fill();

            return uploadUserSettings.AreUploadsPublic;
        }
    }
}