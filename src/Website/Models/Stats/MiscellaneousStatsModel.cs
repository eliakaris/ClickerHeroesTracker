// <copyright file="MiscellaneousStatsModel.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Models.Stats
{
    using System;
    using System.Linq;
    using System.Numerics;
    using ClickerHeroesTrackerWebsite.Models.Game;
    using ClickerHeroesTrackerWebsite.Models.SaveData;

    /// <summary>
    /// The model for the miscellaneous table
    /// </summary>
    public class MiscellaneousStatsModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SuggestedAncientLevelsModel"/> class.
        /// </summary>
        public MiscellaneousStatsModel(
            GameData gameData,
            SavedGame savedGame)
        {
            this.HeroSoulsSpent = savedGame.AncientsData.Ancients.Values.Aggregate(BigInteger.Zero, (count, ancientData) => count + ancientData.SpentHeroSouls);
            this.HeroSoulsSacrificed = savedGame.HeroSoulsSacrificed;
            this.TitanDamage = savedGame.TitanDamage;
            this.TotalAncientSouls = savedGame.AncientSoulsTotal;
            var currentPhandoryssLevel = savedGame.OutsidersData != null && savedGame.OutsidersData.Outsiders != null
                ? savedGame.OutsidersData.Outsiders.GetOutsiderLevel(OutsiderIds.Phandoryss)
                : 0;
            this.TranscendentPower = savedGame.Transcendent
                ? (50 - (49 * Math.Pow(Math.E, -this.TotalAncientSouls / 10000d)) + (currentPhandoryssLevel * 0.05)) / 100
                : 0;
            this.Rubies = savedGame.Rubies;
            this.HighestZoneThisTranscension = savedGame.HighestFinishedZonePersist;
            this.HighestZoneLifetime = Math.Max(savedGame.TranscendentHighestFinishedZone, this.HighestZoneThisTranscension);
            this.AscensionsThisTranscension = savedGame.NumAscensionsThisTranscension != 0
                ? savedGame.NumAscensionsThisTranscension
                : savedGame.NumWorldResets;
            this.AscensionsLifetime = savedGame.NumWorldResets;

            var currentBorbLevel = savedGame.OutsidersData != null && savedGame.OutsidersData.Outsiders != null
                ? savedGame.OutsidersData.Outsiders.GetOutsiderLevel(OutsiderIds.Borb)
                : 0;

            // This is equivalent to: this.HeroSoulsSacrificed * (0.05 + (0.005 * currentBorbLevel))
            // but multiplied and then divided by 200 to defer the loss of precision division until the end.
            this.MaxTranscendentPrimalReward = (this.HeroSoulsSacrificed * new BigInteger(10 + currentBorbLevel)) / 200;

            if (savedGame.Transcendent)
            {
                var currentPonyboyLevel = savedGame.OutsidersData != null && savedGame.OutsidersData.Outsiders != null
                    ? (int)savedGame.OutsidersData.Outsiders.GetOutsiderLevel(OutsiderIds.Ponyboy)
                    : 0;

                double maxRewardLog;
                var currentSolomonLevel = savedGame.AncientsData.GetAncientLevel(AncientIds.Solomon);
                if (currentSolomonLevel > long.MaxValue)
                {
                    // Take a loss of precision for large numbers
                    var solomonMultiplier = (1 + currentPonyboyLevel) * (currentSolomonLevel / 100);
                    maxRewardLog = BigInteger.Log(this.MaxTranscendentPrimalReward / (20 * solomonMultiplier));
                }
                else
                {
                    double solomonMultiplier;
                    if (currentSolomonLevel < 21)
                    {
                        solomonMultiplier = 1 + (1 + currentPonyboyLevel) * (0.05 * (double)currentSolomonLevel);
                    }
                    else if (currentSolomonLevel < 41)
                    {
                        solomonMultiplier = 1 + (1 + currentPonyboyLevel) * (1 + (0.04 * ((double)currentSolomonLevel - 20)));
                    }
                    else if (currentSolomonLevel < 61)
                    {
                        solomonMultiplier = 1 + (1 + currentPonyboyLevel) * (1.8 + (0.03 * ((double)currentSolomonLevel - 40)));
                    }
                    else if (currentSolomonLevel < 81)
                    {
                        solomonMultiplier = 1 + (1 + currentPonyboyLevel) * (2.4 + (0.02 * ((double)currentSolomonLevel - 60)));
                    }
                    else
                    {
                        solomonMultiplier = 1 + (1 + currentPonyboyLevel) * (2.8 + (0.01 * ((double)currentSolomonLevel - 80)));
                    }

                    // If the numbers are sufficiently low enough, just cast and use exact values
                    if (this.MaxTranscendentPrimalReward < long.MaxValue)
                    {
                        maxRewardLog = Math.Log((double)this.MaxTranscendentPrimalReward / (20 * solomonMultiplier));
                    }
                    else
                    {
                        // If the numbers are sufficiently large enough, we can take a loss of precision
                        maxRewardLog = BigInteger.Log(DivideWithPrecisionLoss(this.MaxTranscendentPrimalReward, 20 * solomonMultiplier));
                    }
                }

                var bossNumber = Math.Ceiling(maxRewardLog / Math.Log(1 + this.TranscendentPower));

                // If the boss number is <= 0, that basically means the player is always at the cap. Since zone 100 always gives 0 from TP, the cap is technically 105.
                this.BossLevelToTranscendentPrimalCap = bossNumber > 0
                    ? new BigInteger((bossNumber * 5) + 100)
                    : 105;
            }

            this.HeroSouls = savedGame.HeroSouls;
            this.PendingSouls = savedGame.PendingSouls;
        }

        /// <summary>
        /// Gets the hero souls spent
        /// </summary>
        public BigInteger HeroSoulsSpent { get; }

        /// <summary>
        /// Gets the hero souls earned for the user's lifetime
        /// </summary>
        public BigInteger HeroSoulsSacrificed { get; }

        /// <summary>
        /// Gets the titan damage
        /// </summary>
        public BigInteger TitanDamage { get; }

        /// <summary>
        /// Gets the total ancient souls
        /// </summary>
        public long TotalAncientSouls { get; }

        /// <summary>
        /// Gets the transcendent power
        /// </summary>
        public double TranscendentPower { get; }

        /// <summary>
        /// Gets the rubies the user currently has
        /// </summary>
        public long Rubies { get; }

        /// <summary>
        /// Gets the user's highest zone reached this transcension
        /// </summary>
        public long HighestZoneThisTranscension { get; }

        /// <summary>
        /// Gets the user's highest zone ever reached
        /// </summary>
        public long HighestZoneLifetime { get; }

        /// <summary>
        /// Gets the number of ascensions this transcension
        /// </summary>
        public long AscensionsThisTranscension { get; }

        /// <summary>
        /// Gets the number of ascensions ever
        /// </summary>
        public long AscensionsLifetime { get; }

        /// <summary>
        /// Gets the max transcendent primal reward.
        /// </summary>
        public BigInteger MaxTranscendentPrimalReward { get; }

        /// <summary>
        /// Gets the boss level at which the TP primal cap is reached.
        /// </summary>
        public BigInteger BossLevelToTranscendentPrimalCap { get; }

        /// <summary>
        /// Gets or sets current number of souls.
        /// </summary>
        public BigInteger HeroSouls { get; set; }

        /// <summary>
        /// Gets or sets number of souls earned upon ascending.
        /// </summary>
        public BigInteger PendingSouls { get; }

        private static BigInteger DivideWithPrecisionLoss(BigInteger n, double divisor)
        {
            const long precision = long.MaxValue;
            n *= precision;
            n /= new BigInteger(divisor * precision);
            return n;
        }
    }
}