﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LanPlayServer
{
    class MacAddressMemory
    {
        private HashSet<string> _reservedAddresses = new HashSet<string>();
        private ConcurrentDictionary<string, byte[]> _idToAddress = new ConcurrentDictionary<string, byte[]>();
        private Random _random = new Random();
        private object _lock = new object();

        private byte[] GetNewMac()
        {
            byte[] mac = new byte[6];
            string stringMac;

            lock (_lock)
            {
                do
                {
                    _random.NextBytes(mac);

                    stringMac = LdnHelper.ByteArrayToString(mac);
                }
                while (_reservedAddresses.Contains(stringMac));

                _reservedAddresses.Add(stringMac);
            }

            return mac;
        }

        public byte[] TryFind(string id, byte[] macAddress, string newId)
        {
            byte[] result;

            if (!_idToAddress.TryGetValue(id, out result) || !result.SequenceEqual(macAddress))
            {
                result = GetNewMac();
            }

            _idToAddress.TryAdd(newId, result);

            return result;
        }
    }
}