﻿// <copyright file="DatabaseCommandFactory.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Services.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.SqlClient;
    using ClickerHeroesTrackerWebsite.Instrumentation;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// A SQL command provider for the default connection string
    /// </summary>
    public sealed class DatabaseCommandFactory : IDisposable, IDatabaseCommandFactory
    {
        private static Dictionary<string, Func<string, DbConnection>> connectionFactories = new Dictionary<string, Func<string, DbConnection>>(StringComparer.OrdinalIgnoreCase)
        {
            { "SqlServer", str => new SqlConnection(str) },
            { "Sqlite", str => new SqliteConnection(str) },
        };

        private readonly DatabaseSettings databaseSettings;

        private readonly ICounterProvider counterProvider;

        private DbConnection connection;

        public DatabaseCommandFactory(
            IOptions<DatabaseSettings> databaseSettingsOptions,
            ICounterProvider counterProvider)
        {
            this.databaseSettings = databaseSettingsOptions.Value;
            this.counterProvider = counterProvider;
        }

        /// <inheritdoc/>
        public IDatabaseCommand Create()
        {
            // Create the connection if it hasn't been created yet.
            if (this.connection == null)
            {
                using (this.counterProvider.Suspend(Counter.Internal))
                using (this.counterProvider.Measure(Counter.Dependency))
                {
                    this.connection = connectionFactories[this.databaseSettings.Kind](this.databaseSettings.ConnectionString);
                    this.connection.Open();
                }
            }

            return new DatabaseCommand(
                this.connection,
                this.counterProvider);
        }

        public void Dispose()
        {
            if (this.connection != null)
            {
                this.connection.Dispose();
                this.connection = null;
            }
        }

        private static DbConnection CreateSqlConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}