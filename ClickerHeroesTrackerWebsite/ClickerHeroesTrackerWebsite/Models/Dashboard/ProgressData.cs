﻿namespace ClickerHeroesTrackerWebsite.Models.Dashboard
{
    using ClickerHeroesTrackerWebsite.Models.Game;
    using System;
    using System.Collections.Generic;

    public class ProgressData
    {
        public ProgressData(string userId)
        {
            using (var command = new DatabaseCommand("GetProgressData"))
            {
                command.AddParameter("@UserId", userId);

                ////command.AddParameter("@StartTime", default(DateTime));
                ////command.AddParameter("@EndTime", default(DateTime));

                this.OptimalLevelData = new SortedDictionary<DateTime, short>();
                this.SoulsPerHourData = new SortedDictionary<DateTime, int>();

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var uploadTime = (DateTime)reader["UploadTime"];
                    this.OptimalLevelData.Add(uploadTime, (short)reader["OptimalLevel"]);
                    this.SoulsPerHourData.Add(uploadTime, (int)reader["SoulsPerHour"]);
                }

                if (!reader.NextResult())
                {
                    return;
                }

                this.AncientLevelData = new SortedDictionary<Ancient, IDictionary<DateTime, int>>(AncientComparer.Instance);
                while (reader.Read())
                {
                    // UploadTime, AncientId, Level
                    var uploadTime = (DateTime)reader["UploadTime"];
                    var ancientId = (byte)reader["AncientId"];
                    var level = (int)reader["Level"];

                    var ancient = Ancient.Get(ancientId);

                    // Skip ancients with max levels for now
                    if (ancient.MaxLevel > 0)
                    {
                        continue;
                    }

                    IDictionary<DateTime, int> levelData;
                    if (!this.AncientLevelData.TryGetValue(ancient, out levelData))
                    {
                        levelData = new SortedDictionary<DateTime, int>();
                        this.AncientLevelData.Add(ancient, levelData);
                    }

                    levelData.Add(uploadTime, level);
                }
            }
        }

        public IDictionary<DateTime, short> OptimalLevelData { get; private set; }

        public IDictionary<DateTime, int> SoulsPerHourData { get; private set; }

        public IDictionary<Ancient, IDictionary<DateTime, int>> AncientLevelData { get; private set; }

        private class AncientComparer : IComparer<Ancient>
        {
            public static AncientComparer Instance = new AncientComparer();

            public int Compare(Ancient x, Ancient y)
            {
                return x.Name.CompareTo(y.Name);
            }
        }
    }
}