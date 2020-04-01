// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Collections.Generic;

namespace System.Diagnostics
{
    public sealed class ActivitySource : IDisposable
    {
        private static SynchronizedList<ActivitySource> s_activeSources = new SynchronizedList<ActivitySource>();
        private static SynchronizedList<ActivityListener> s_allListeners = new SynchronizedList<ActivityListener>();
        private SynchronizedList<ActivityListener>? _listeners;

        private ActivitySource() { throw new InvalidOperationException(); }

        /// <summary>
        /// Construct an ActivitySource object with the input name
        /// </summary>
        /// <param name="name">The name of the ActivitySource object</param>
        /// <param name="version">The version of the component publishing the tracing info.</param>
        public ActivitySource(string name, Version version)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Version = version;

            s_activeSources.Add(this);

            if (s_allListeners.Count > 0)
            {
                s_allListeners.EnumWithAction(listener => {
                    if (listener.ListenToSource(this))
                    {
                        AddListener(listener);
                    }
                });
            }
        }

        /// <summary>
        /// Returns the ActivitySource name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the ActivitySource version.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Check if there is any listeners for this ActivitySource.
        /// This property can be helpful to tell if there is no listener, then no need to create Activity object
        /// and avoid creating the objects needed to create Activity (e.g. ActivityContext)
        /// Example of that is http scenario which can avoid reading the context data from the wire.
        /// </summary>
        public bool HasListeners => _listeners != null && _listeners.Count > 0;

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity, returns null otherwise.
        /// </summary>
        /// <param name="name">The operation name of the Activity</param>
        /// <param name="kind">The <see cref="ActivityKind"/></param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any event listener.</returns>
        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
            => StartActivity(name, kind, default, null, null, null, default);

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity events, returns null otherwise.
        /// </summary>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="kind">The <see cref="ActivityKind"/></param>
        /// <param name="parentContext">The parent <see cref="ActivityContext"/> object to initialize the created Activity object with.</param>
        /// <param name="tags">The optional tags list to initialize the created Activity object with.</param>
        /// <param name="links">The optional <see cref="ActivityLink"/> list to initialize the created Activity object with.</param>
        /// <param name="startTime">The optional start timestamp to set on the created Activity object.</param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any listener.</returns>
        public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, string?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
            => StartActivity(name, kind, parentContext, null, tags, links, startTime);

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity events, returns null otherwise.
        /// </summary>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="kind">The <see cref="ActivityKind"/></param>
        /// <param name="parentId">The parent Id to initialize the created Activity object with.</param>
        /// <param name="tags">The optional tags list to initialize the created Activity object with.</param>
        /// <param name="links">The optional <see cref="ActivityLink"/> list to initialize the created Activity object with.</param>
        /// <param name="startTime">The optional start timestamp to set on the created Activity object.</param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any listener.</returns>
        public Activity? StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, string?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
            => StartActivity(name, kind, default, parentId, tags, links, startTime);

        private Activity? StartActivity(string name, ActivityKind kind, ActivityContext context, string? parentId, IEnumerable<KeyValuePair<string, string?>>? tags, IEnumerable<ActivityLink>? links, DateTimeOffset startTime)
        {
            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners == null || listeners.Count == 0)
            {
                return null;
            }

            Activity? activity = null;

            ActivityDataRequest dateRequest = ActivityDataRequest.None;

            if (parentId != null)
            {
                listeners.EnumWithFunc(listener => {
                    ActivityDataRequest dr = listener.GetActivityDataRequestUsingParentId(this, name, kind, parentId, tags, links);
                    if (dr > dateRequest)
                    {
                        dateRequest = dr;
                    }

                    // Stop the enumeration if we get the max value RecordingAndSampling.
                    return dateRequest != ActivityDataRequest.AllDataAndRecorded;
                });
            }
            else
            {
                listeners.EnumWithFunc(listener => {
                    ActivityDataRequest dr = listener.GetActivityDataRequestUsingContext(this, name, kind, context, tags, links);
                    if (dr > dateRequest)
                    {
                        dateRequest = dr;
                    }

                    // Stop the enumeration if we get the max value RecordingAndSampling.
                    return dateRequest != ActivityDataRequest.AllDataAndRecorded;
                });
            }

            if (dateRequest != ActivityDataRequest.None)
            {
                activity = Activity.CreateAndStart(this, name, kind, parentId, default, tags, links, startTime, dateRequest);
                listeners.EnumWithAction(listener => listener.OnActivityStarted(activity));
            }

            return activity;
        }

        /// <summary>
        /// Dispose the ActivitySource object and remove the current instance from the global list. empty the listeners list too.
        /// </summary>
        public void Dispose()
        {
            _listeners = null;
            s_activeSources.Remove(this);
        }

        /// <summary>
        /// Add a listener to the <see cref="Activity"/> starting and stopping events.
        /// </summary>
        /// <param name="listenToSource">The callback which will get called every time the ActivitySource get created to decide if need to listen to this source.</param>
        /// <param name="getActivityDataRequestUsingContext">The callback which will get called every time the ActivitySource try to creates a new Activity object using ActivityContext.</param>
        /// <param name="getActivityDataRequestUsingParentId">The callback which will get called every time the ActivitySource try to creates a new Activity object using ParentId.</param>
        /// <param name="onActivityStarted">The callback which get called everytime the activity get started.</param>
        /// <param name="onActivityStopped">The callback which get called everytime the activity get stopped.</param>
        /// <returns>The disposable object represent the listener. When disposing the listener, it will stop the Activity event notification.</returns>
        public static IDisposable AddActivityListener(
                                System.Func<System.Diagnostics.ActivitySource, bool> listenToSource,
                                System.Func<System.Diagnostics.ActivitySource, string, System.Diagnostics.ActivityKind, System.Diagnostics.ActivityContext, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>>?, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>?, System.Diagnostics.ActivityDataRequest> getActivityDataRequestUsingContext,
                                System.Func<System.Diagnostics.ActivitySource, string, System.Diagnostics.ActivityKind, string, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string?>>?, System.Collections.Generic.IEnumerable<System.Diagnostics.ActivityLink>?, System.Diagnostics.ActivityDataRequest> getActivityDataRequestUsingParentId,
                                Action<Activity> onActivityStarted,
                                Action<Activity> onActivityStopped)
        {
            ActivityListener listener = new ActivityListener(listenToSource, getActivityDataRequestUsingContext, getActivityDataRequestUsingParentId, onActivityStarted, onActivityStopped);
            s_allListeners.Add(listener);

            s_activeSources.EnumWithAction(source => {
                if (listener.ListenToSource(source))
                {
                    source.AddListener(listener);
                }
            });

            return listener;
        }

        internal void AddListener(ActivityListener listener)
        {
            if (_listeners == null)
            {
                Interlocked.CompareExchange(ref _listeners, new SynchronizedList<ActivityListener>(), null);
            }

            _listeners.AddIfNotExist(listener);
        }

        internal static void DetachListener(ActivityListener listener)
        {
            s_activeSources.EnumWithAction(source => {
                var listeners = source._listeners;
                listeners?.Remove(listener);
            });
            s_allListeners.Remove(listener);
        }

        internal void NotifyActivityStart(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null &&  listeners.Count > 0)
            {
                listeners.EnumWithAction(listener => listener.OnActivityStarted(activity));
            }
        }

        internal void NotifyActivityStop(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null &&  listeners.Count > 0)
            {
                listeners.EnumWithAction(listener => listener.OnActivityStopped(activity));
            }
        }
    }

    // SynchronizedList<T> is a helper collection which ensure thread safety on the collection
    // and allow enumerating the collection items and execute some action on the enumerated item and can detect any change in the collection
    // during the enumeration which force restarting the enumeration again.
    // Causion: We can have teh action executed on the same item more than once which is ok in our scenarios.
    internal class SynchronizedList<T>
    {
        private List<T> _list;
        private uint _version;

        public SynchronizedList() => _list = new List<T>();

        public void Add(T item)
        {
            lock (_list)
            {
                _list.Add(item);
                _version++;
            }
        }

        public void AddIfNotExist(T item)
        {
            lock (_list)
            {
                if (!_list.Contains(item))
                {
                    _list.Add(item);
                    _version++;
                }
            }
        }

        public bool Remove(T item)
        {
            lock (_list)
            {
                if (_list.Remove(item))
                {
                    _version++;
                    return true;
                }
                return false;
            }
        }

        public int Count => _list.Count;

        public void EnumWithFunc(Func<T, bool> func)
        {
            uint version = _version;
            int index = 0;

            while (index < _list.Count)
            {
                T item;
                lock (_list)
                {
                    if (version != _version)
                    {
                        version = _version;
                        index = 0;
                        continue;
                    }

                    item = _list[index];
                    index++;
                }

                // Important to call the func outside the lock.
                // This is the whole point we are having this wrapper class.
                if (!func(item))
                {
                    break;
                }
            }
        }

        public void EnumWithAction(Action<T> action)
        {
            uint version = _version;
            int index = 0;

            while (index < _list.Count)
            {
                T item;
                lock (_list)
                {
                    if (version != _version)
                    {
                        version = _version;
                        index = 0;
                        continue;
                    }

                    item = _list[index];
                    index++;
                }

                // Important to call the action outside the lock.
                // This is the whole point we are having this wrapper class.
                action(item);
            }
        }

    }
}
