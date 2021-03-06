﻿// <copyright file="ProgressData.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Models.Dashboard
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Numerics;
    using ClickerHeroesTrackerWebsite.Models.Game;
    using ClickerHeroesTrackerWebsite.Services.Database;
    using ClickerHeroesTrackerWebsite.Utility;
    using Microsoft.ApplicationInsights;

    /// <summary>
    /// An aggregation of progress data for a user.
    /// </summary>
    public class ProgressData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressData"/> class.
        /// </summary>
        public ProgressData(
            GameData gameData,
            TelemetryClient telemetryClient,
            IDatabaseCommandFactory databaseCommandFactory,
            string userId,
            DateTime? startTime,
            DateTime? endTime)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@UserId", userId },
                { "@StartTime", startTime },
                { "@EndTime", endTime },
            };

            const string GetProgressDataCommandText = @"
                -- Create a temp table that scopes the Uploads
                CREATE TABLE #ScopedUploads
                (
                    Id  INT NOT NULL, 
                    UploadTime  DATETIME2(0) NOT NULL, 
                );

                -- Populate temp table
                INSERT INTO #ScopedUploads (Id, UploadTime)
                SELECT Id, UploadTime
                FROM Uploads
                WHERE UserId = @UserId
                AND UploadTime >= ISNULL(@StartTime, '0001-01-01 00:00:00')
                AND UploadTime <= ISNULL(@EndTime, '9999-12-31 23:59:59');

                -- Computed Stats
                SELECT #ScopedUploads.UploadTime,
                       ComputedStats.TitanDamage,
                       ComputedStats.SoulsSpent,
                       ComputedStats.HeroSoulsSacrificed,
                       ComputedStats.TotalAncientSouls,
                       ComputedStats.TranscendentPower,
                       ComputedStats.Rubies,
                       ComputedStats.HighestZoneThisTranscension,
                       ComputedStats.HighestZoneLifetime,
                       ComputedStats.AscensionsThisTranscension,
                       ComputedStats.AscensionsLifetime
                FROM ComputedStats
                INNER JOIN #ScopedUploads
                ON ComputedStats.UploadId = #ScopedUploads.Id;

                -- Ancient Levels
                SELECT #ScopedUploads.UploadTime, AncientLevels.AncientId, AncientLevels.Level
                FROM AncientLevels
                INNER JOIN #ScopedUploads
                ON AncientLevels.UploadId = #ScopedUploads.Id;

                -- Outsider Levels
                SELECT #ScopedUploads.UploadTime, OutsiderLevels.OutsiderId, OutsiderLevels.Level
                FROM OutsiderLevels
                INNER JOIN #ScopedUploads
                ON OutsiderLevels.UploadId = #ScopedUploads.Id;

                -- Drop the temp table
                DROP TABLE #ScopedUploads;";
            using (var command = databaseCommandFactory.Create(
                GetProgressDataCommandText,
                parameters))
            using (var reader = command.ExecuteReader())
            {
                this.TitanDamageData = new SortedDictionary<DateTime, BigInteger>();
                this.SoulsSpentData = new SortedDictionary<DateTime, BigInteger>();
                this.HeroSoulsSacrificedData = new SortedDictionary<DateTime, BigInteger>();
                this.TotalAncientSoulsData = new SortedDictionary<DateTime, double>();
                this.TranscendentPowerData = new SortedDictionary<DateTime, double>();
                this.RubiesData = new SortedDictionary<DateTime, double>();
                this.HighestZoneThisTranscensionData = new SortedDictionary<DateTime, double>();
                this.HighestZoneLifetimeData = new SortedDictionary<DateTime, double>();
                this.AscensionsThisTranscensionData = new SortedDictionary<DateTime, double>();
                this.AscensionsLifetimeData = new SortedDictionary<DateTime, double>();

                while (reader.Read())
                {
                    // The DateTime is a datetime2 which has no timezone so comes out as DateTimeKind.Unknown. Se need to specify the kind so it gets serialized correctly.
                    var uploadTime = DateTime.SpecifyKind(Convert.ToDateTime(reader["UploadTime"]), DateTimeKind.Utc);

                    this.TitanDamageData.AddOrUpdate(uploadTime, reader["TitanDamage"].ToString().ToBigInteger());
                    this.SoulsSpentData.AddOrUpdate(uploadTime, reader["SoulsSpent"].ToString().ToBigInteger());
                    this.HeroSoulsSacrificedData.AddOrUpdate(uploadTime, reader["HeroSoulsSacrificed"].ToString().ToBigInteger());
                    this.TotalAncientSoulsData.AddOrUpdate(uploadTime, Convert.ToDouble(reader["TotalAncientSouls"]));
                    this.TranscendentPowerData.AddOrUpdate(uploadTime, 100 * Convert.ToDouble(reader["TranscendentPower"]));
                    this.RubiesData.AddOrUpdate(uploadTime, Convert.ToDouble(reader["Rubies"]));
                    this.HighestZoneThisTranscensionData.AddOrUpdate(uploadTime, Convert.ToDouble(reader["HighestZoneThisTranscension"]));
                    this.HighestZoneLifetimeData.AddOrUpdate(uploadTime, Convert.ToDouble(reader["HighestZoneLifetime"]));
                    this.AscensionsThisTranscensionData.AddOrUpdate(uploadTime, Convert.ToDouble(reader["AscensionsThisTranscension"]));
                    this.AscensionsLifetimeData.AddOrUpdate(uploadTime, Convert.ToDouble(reader["AscensionsLifetime"]));
                }

                if (!reader.NextResult())
                {
                    return;
                }

                this.AncientLevelData = new SortedDictionary<string, IDictionary<DateTime, BigInteger>>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    // The DateTime is a datetime2 which has no timezone so comes out as DateTimeKind.Unknown. Se need to specify the kind so it gets serialized correctly.
                    var uploadTime = DateTime.SpecifyKind(Convert.ToDateTime(reader["UploadTime"]), DateTimeKind.Utc);
                    var ancientId = Convert.ToInt32(reader["AncientId"]);
                    var level = reader["Level"].ToString().ToBigInteger();

                    if (!gameData.Ancients.TryGetValue(ancientId, out var ancient))
                    {
                        telemetryClient.TrackEvent("Unknown Ancient", new Dictionary<string, string> { { "AncientId", ancientId.ToString() } });
                        continue;
                    }

                    if (!this.AncientLevelData.TryGetValue(ancient.Name, out var levelData))
                    {
                        levelData = new SortedDictionary<DateTime, BigInteger>();
                        this.AncientLevelData.Add(ancient.Name, levelData);
                    }

                    levelData.AddOrUpdate(uploadTime, level);
                }

                if (!reader.NextResult())
                {
                    return;
                }

                this.OutsiderLevelData = new SortedDictionary<string, IDictionary<DateTime, double>>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    // The DateTime is a datetime2 which has no timezone so comes out as DateTimeKind.Unknown. Se need to specify the kind so it gets serialized correctly.
                    var uploadTime = DateTime.SpecifyKind(Convert.ToDateTime(reader["UploadTime"]), DateTimeKind.Utc);
                    var outsiderId = Convert.ToInt32(reader["OutsiderId"]);
                    var level = Convert.ToDouble(reader["Level"]);

                    if (!gameData.Outsiders.TryGetValue(outsiderId, out var outsider))
                    {
                        telemetryClient.TrackEvent("Unknown Outsider", new Dictionary<string, string> { { "OutsiderId", outsiderId.ToString() } });
                        continue;
                    }

                    if (!this.OutsiderLevelData.TryGetValue(outsider.Name, out var levelData))
                    {
                        levelData = new SortedDictionary<DateTime, double>();
                        this.OutsiderLevelData.Add(outsider.Name, levelData);
                    }

                    levelData.AddOrUpdate(uploadTime, level);
                }
            }

            this.IsValid = true;
        }

        public bool IsValid { get; }

        public IDictionary<DateTime, BigInteger> TitanDamageData { get; }

        public IDictionary<DateTime, BigInteger> SoulsSpentData { get; }

        public IDictionary<DateTime, BigInteger> HeroSoulsSacrificedData { get; }

        public IDictionary<DateTime, double> TotalAncientSoulsData { get; }

        public IDictionary<DateTime, double> TranscendentPowerData { get; }

        public IDictionary<DateTime, double> RubiesData { get; }

        public IDictionary<DateTime, double> HighestZoneThisTranscensionData { get; }

        public IDictionary<DateTime, double> HighestZoneLifetimeData { get; }

        public IDictionary<DateTime, double> AscensionsThisTranscensionData { get; }

        public IDictionary<DateTime, double> AscensionsLifetimeData { get; }

        public IDictionary<string, IDictionary<DateTime, BigInteger>> AncientLevelData { get; }

        public IDictionary<string, IDictionary<DateTime, double>> OutsiderLevelData { get; }
    }
}