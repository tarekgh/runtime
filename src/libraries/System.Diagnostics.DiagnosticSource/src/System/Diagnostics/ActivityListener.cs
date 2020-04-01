// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityListener allows listening to the start and stop Activity events and give the oppertunity to decide creating the Activity for sampling scenarios.
    /// </summary>
    internal class ActivityListener : IDisposable
    {
        internal System.Func<System.Diagnostics.ActivitySource, bool> ListenToSource  { get; }
        internal Func<ActivitySource, string, ActivityKind, ActivityContext, IEnumerable<KeyValuePair<string, string?>>?, IEnumerable<ActivityLink>?, ActivityDataRequest> GetActivityDataRequestUsingContext { get; }
        internal Func<ActivitySource, string, ActivityKind, string, IEnumerable<KeyValuePair<string, string?>>?, IEnumerable<ActivityLink>?, ActivityDataRequest> GetActivityDataRequestUsingParentId { get; }
        internal Action<Activity> OnActivityStarted { get; }
        internal Action<Activity> OnActivityStopped { get; }

        /// <summary>
        /// Create a listener using the callback delegates. This factory method is useful in the scenario of the Dependency Injection (DI) which need a way to easily create  a listener
        /// using the reflection without the need to subclass the ActivityListener.
        /// </summary>
        internal ActivityListener(
                    System.Func<System.Diagnostics.ActivitySource, bool> listenToSource,
                    Func<ActivitySource, string, ActivityKind, ActivityContext, IEnumerable<KeyValuePair<string, string?>>?, IEnumerable<ActivityLink>?, ActivityDataRequest> getActivityDataRequestUsingContext,
                    Func<ActivitySource, string, ActivityKind, string, IEnumerable<KeyValuePair<string, string?>>?, IEnumerable<ActivityLink>?, ActivityDataRequest> getActivityDataRequestUsingParentId,
                    Action<Activity> onActivityStarted,
                    Action<Activity> onActivityStopped)
        {
            if (listenToSource == null)
                throw new ArgumentNullException(nameof(listenToSource));

            if (getActivityDataRequestUsingContext == null)
                throw new ArgumentNullException(nameof(getActivityDataRequestUsingContext));

            if (getActivityDataRequestUsingParentId == null)
                throw new ArgumentNullException(nameof(getActivityDataRequestUsingParentId));

            if (onActivityStarted == null)
                throw new ArgumentNullException(nameof(onActivityStarted));

            if (onActivityStopped == null)
                throw new ArgumentNullException(nameof(onActivityStopped));

            ListenToSource = listenToSource;
            GetActivityDataRequestUsingContext = getActivityDataRequestUsingContext;
            GetActivityDataRequestUsingParentId = getActivityDataRequestUsingParentId;
            OnActivityStarted = onActivityStarted;
            OnActivityStopped = onActivityStopped;
        }

        public void Dispose() => ActivitySource.DetachListener(this);
   }
}
