// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dacs7.Alarms
{
    public static class Dacs7ClientAlarmExtensions
    {

        /// <summary>
        /// Reads all pending alarms from the plc
        /// </summary>
        /// <param name="client"></param>
        /// <returns>A list of <see cref="IPlcAlarm"/></returns>
        public static Task<IEnumerable<IPlcAlarm>> ReadPendingAlarmsAsync(this Dacs7Client client)
        {
            return client.ProtocolHandler.ReadPendingAlarmsAsync();
        }

        /// <summary>
        /// This method returns if the plcalarms get updated.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        /// <returns><see cref="AlarmUpdateResult"/></returns>
        [Obsolete("ReceiveAlarmUpdatesAsync is deprecated, please use AlarmSubscription class instead.")]
        public static Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(this Dacs7Client client, CancellationToken ct)
        {
            return client.ProtocolHandler.ReceiveAlarmUpdatesAsync(ct);
        }


        /// <summary>
        /// Create a alarm subscription.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static AlarmSubscription CreateAlarmSubscription(this Dacs7Client client)
        {
            return client.ProtocolHandler.CreateAlarmSubscription();
        }

        /// <summary>
        /// Wait for a alarm event
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<AlarmUpdateResult> ReceiveAlarmUpdatesAsync(this AlarmSubscription subscription, CancellationToken ct)
        {
            return subscription.ProtocolHandler.ReceiveAlarmUpdatesAsync(subscription, ct);
        }
    }
}
