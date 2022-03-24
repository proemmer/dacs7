﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.ReadWrite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{
    public static class Dacs7ClientRegisterExtensions
    {

        /// <summary>
        /// Register shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns>Returns the registered shortcuts</returns>
        public static Task<IEnumerable<string>> RegisterAsync(this Dacs7Client client, params string[] values)
        {
            return client.RegisterAsync(values as IEnumerable<string>);
        }

        /// <summary>
        /// Register shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Task<IEnumerable<string>> RegisterAsync(this Dacs7Client client, IEnumerable<string> values)
        {
            List<KeyValuePair<string, ReadItem>> added = new();
            IEnumerator<string> enumerator = values.GetEnumerator();
            List<string> resList = client.CreateNodeIdCollection(values).Select(x =>
            {
                enumerator.MoveNext();
                added.Add(new KeyValuePair<string, ReadItem>(enumerator.Current, x));
                return x.ToString();
            }).ToList();

            client.UpdateRegistration(added, null);
            return Task.FromResult<IEnumerable<string>>(resList);
        }



        /// <summary>
        /// Remove shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Task<IEnumerable<string>> UnregisterAsync(this Dacs7Client client, params string[] values)
        {
            return client.UnregisterAsync(values as IEnumerable<string>);
        }

        /// <summary>
        /// Remove shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Task<IEnumerable<string>> UnregisterAsync(this Dacs7Client client, IEnumerable<string> values)
        {
            List<KeyValuePair<string, ReadItem>> removed = new();
            foreach (string item in values)
            {
                if (client.RegisteredTags.TryGetValue(item, out ReadItem obj))
                {
                    removed.Add(new KeyValuePair<string, ReadItem>(item, obj));
                }
            }

            client.UpdateRegistration(null, removed);
            return Task.FromResult<IEnumerable<string>>(removed.Select(x => x.Key).ToList());
        }

        /// <summary>
        /// Retruns true if the given tag is already registred
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static bool IsTagRegistered(this Dacs7Client client, string tag)
        {
            return client.RegisteredTags.ContainsKey(tag);
        }
    }
}
