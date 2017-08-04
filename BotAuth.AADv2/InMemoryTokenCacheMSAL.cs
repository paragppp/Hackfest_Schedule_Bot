﻿using Microsoft.Identity.Client;
using System.Collections.Generic;

namespace BotAuth.AADv2
{
    public class InMemoryTokenCacheMSAL : TokenCache
    {
        string CacheId = string.Empty;
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        public InMemoryTokenCacheMSAL()
        {
            CacheId = "MSAL_TokenCache";
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            Load();
        }

        public InMemoryTokenCacheMSAL(byte[] tokenCache)
        {
            CacheId = "MSAL_TokenCache";
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.Deserialize(tokenCache);
        }

        public void SaveUserStateValue(string state)
        {
            cache[CacheId + "_state"] = state;
        }
        public string ReadUserStateValue()
        {
            string state = string.Empty;
            state = (string)cache[CacheId + "_state"];
            return state;
        }
        public void Load()
        {
            if (cache.ContainsKey(CacheId))
                this.Deserialize((byte[])cache[CacheId]);
        }

        public void Persist()
        {
            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            this.HasStateChanged = false;

            // Reflect changes in the persistent store
            cache[CacheId] = this.Serialize();
        }

        // Empties the persistent store.
        public override void Clear(string cliendId)
        {
            base.Clear(cliendId);
            cache.Remove(CacheId);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after ADAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.HasStateChanged)
            {
                Persist();
            }
        }
    }
}
