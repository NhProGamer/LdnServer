﻿using LanPlayServer.Stats.Types;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;
using System;
using System.ComponentModel;
using System.Net;

namespace LanPlayServer.Stats
{
    internal static class StatsDumper
    {
        private static ConnectionMultiplexer _redisConnection;
        private static IDatabase _db;

        public static void Start(EndPoint redisEndpoint)
        {
            _redisConnection = ConnectionMultiplexer.Connect(new ConfigurationOptions()
            {
                ClientName = "LdnServer",
                EndPoints = new()
                {
                    redisEndpoint,
                },
            });

            _db = _redisConnection.GetDatabase();

            Console.WriteLine("Creating empty json objects for redis if necessary...");

            EnsureDBKeysExist(_db.JSON());

            Statistics.GameAnalyticsChanged += OnGameAnalyticsChanged;
            Statistics.LdnAnalyticsChanged += OnLdnAnalyticsPropertyChanged;
        }

        private static void EnsureDBKeysExist(IJsonCommands json)
        {
            json.Set("ldn", "$", new LdnAnalytics().ToJson(), When.NotExists);
            json.Set("games", "$", new {}, When.NotExists);
        }

        public static void Stop()
        {
            Statistics.GameAnalyticsChanged -= OnGameAnalyticsChanged;
            Statistics.LdnAnalyticsChanged -= OnLdnAnalyticsPropertyChanged;

            _redisConnection.Close();
            _redisConnection.Dispose();
        }

        private static void OnLdnAnalyticsPropertyChanged(LdnAnalytics analytics)
        {
            IJsonCommands json = _db.JSON();
            string analyticsJson = analytics.ToJson();

            EnsureDBKeysExist(json);
            json.Set("ldn", "$", analyticsJson);
        }

        private static void OnGameAnalyticsChanged(GameAnalytics analytics, bool created)
        {
            IJsonCommands json = _db.JSON();
            string analyticsJson = analytics.ToJson();

            EnsureDBKeysExist(json);

            if (created)
            {
                json.Set("games", $"$.{analytics.Id}", analyticsJson);
                analytics.PropertyChanged += OnGameAnalyticsPropertyChanged;
            }
            else
            {
                analytics.PropertyChanged -= OnGameAnalyticsPropertyChanged;
                json.Del("games", $"$.{analytics.Id}");
            }
        }

        private static void OnGameAnalyticsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GameAnalytics analytics = sender as GameAnalytics;
            IJsonCommands json = _db.JSON();

            if (analytics == null)
            {
                return;
            }

            EnsureDBKeysExist(json);

            if (analytics.PlayerCount == 0)
            {
                json.Del("games", $"$.{analytics.Id}");
                return;
            }

            string analyticsJson = analytics.ToJson();

            // This could be optimized to only update the changed property.
            json.Set("games", $"$.{analytics.Id}", analyticsJson);
        }
    }
}
