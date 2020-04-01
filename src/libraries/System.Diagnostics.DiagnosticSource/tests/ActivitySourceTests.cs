// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ActivitySourceTests : IDisposable
    {
        [Fact]
        public void TestConstruction()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource as1 = new ActivitySource("Source1", new Version()))
                {
                    Assert.Equal("Source1", as1.Name);
                    Assert.Equal(new Version(), as1.Version);
                    Assert.False(as1.HasListeners);
                    using (ActivitySource as2 =  new ActivitySource("Source2", new Version(1, 1, 1, 2)))
                    {
                        Assert.Equal("Source2", as2.Name);
                        Assert.Equal(new Version(1, 1, 1, 2), as2.Version);
                        Assert.False(as2.HasListeners);
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestStartActivityWithNoListener()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource =  new ActivitySource("SourceActivity", new Version()))
                {
                    Assert.Equal("SourceActivity", aSource.Name);
                    Assert.Equal(new Version(), aSource.Version);
                    Assert.False(aSource.HasListeners);

                    Activity current = Activity.Current;
                    using (Activity a1 = aSource.StartActivity("a1"))
                    {
                        // no listeners, we should get null activity.
                        Assert.Null(a1);
                        Assert.Equal(Activity.Current, current);
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivityWithListenerNoActivityCreate()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource =  new ActivitySource("SourceActivityListener", new Version()))
                {
                    Assert.False(aSource.HasListeners);

                    using (IDisposable listener1 = ActivitySource.AddActivityListener(
                                    (activitySource) => object.ReferenceEquals(aSource, activitySource),
                                    (source, name, kind, context, tags, links) => ActivityDataRequest.None,
                                    (source, name, kind, parentId, tags, links) => ActivityDataRequest.None,
                                    activity => Assert.NotNull(activity), activity => Assert.NotNull(activity)))
                    {
                        Assert.True(aSource.HasListeners);

                        // The listener is not allowing to create a new Activity.
                        Assert.Null(aSource.StartActivity("nullActivity"));
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivityWithListenerActivityCreateNoDataAllowed()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource = new ActivitySource("SourceActivityListener", new Version()))
                {
                    int counter = 0;
                    Assert.False(aSource.HasListeners);

                    using (IDisposable listener1 = ActivitySource.AddActivityListener(
                                    (activitySource) => object.ReferenceEquals(aSource, activitySource),
                                    (source, name, kind, parentId, tags, links) => ActivityDataRequest.PropagationData,
                                    (source, name, kind, context, tags, links) => ActivityDataRequest.PropagationData,
                                    activity => counter++, activity => counter--))
                    {
                        Assert.True(aSource.HasListeners);
                        Activity current = Activity.Current;
                        // The listener allow creating activity but not allowing adding any data to it.
                        Activity activity = aSource.StartActivity("NotAllDataRequestedActivity");
                        Assert.NotNull(activity);
                        Assert.False(activity.IsAllDataRequested);
                        Assert.Equal(1, counter);
                        Assert.Equal(activity, Activity.Current);
                        Assert.Equal(current, activity.Parent);

                        Assert.Equal(0, activity.Tags.Count());
                        Assert.Equal(0, activity.Baggage.Count());
                        Assert.Equal(0, activity.Links.Count());
                        Assert.Equal(0, activity.Events.Count());

                        Assert.True(object.ReferenceEquals(activity, activity.AddTag("key", "value")));
                        Assert.True(object.ReferenceEquals(activity, activity.AddBaggage("key", "value")));
                        Assert.True(object.ReferenceEquals(activity, activity.AddLink(new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, null)))));
                        Assert.True(object.ReferenceEquals(activity, activity.AddEvent(new ActivityEvent("e1"))));

                        Assert.Equal(0, activity.Tags.Count());
                        Assert.Equal(0, activity.Baggage.Count());
                        Assert.Equal(0, activity.Links.Count());
                        Assert.Equal(0, activity.Events.Count());

                        activity.Dispose();
                        Assert.Equal(0, counter);
                        Assert.Equal(current, Activity.Current);
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivityWithListenerActivityCreateAndAllDataRequested()
        {
            RemoteExecutor.Invoke(() => {
                using (ActivitySource aSource = new ActivitySource("SourceActivityListener", new Version()))
                {
                    int counter = 0;
                    Assert.False(aSource.HasListeners);

                    using (IDisposable listener1 = ActivitySource.AddActivityListener(
                                    (activitySource) => object.ReferenceEquals(aSource, activitySource),
                                    (source, name, kind, context, tags, links) => ActivityDataRequest.AllDataAndRecorded,
                                    (source, name, kind, parentId, tags, links) => ActivityDataRequest.AllDataAndRecorded,
                                    activity => counter++, activity => counter--))
                    {
                        Assert.True(aSource.HasListeners);

                        // The listener allow creating activity but not allowing adding any data to it.
                        using (Activity activity = aSource.StartActivity("AllDataRequestedActivity"))
                        {
                            Assert.NotNull(activity);
                            // Assert.True(activity.IsAllDataRequested);
                            Assert.Equal(1, counter);

                            Assert.Equal(0, activity.Tags.Count());
                            Assert.Equal(0, activity.Baggage.Count());

                            Assert.True(object.ReferenceEquals(activity, activity.AddTag("key", "value")));
                            Assert.True(object.ReferenceEquals(activity, activity.AddBaggage("key", "value")));

                            Assert.Equal(1, activity.Tags.Count());
                            Assert.Equal(1, activity.Baggage.Count());

                            using (Activity activity1 = aSource.StartActivity("AllDataRequestedActivity1"))
                            {
                                Assert.NotNull(activity1);
                                Assert.True(activity1.IsAllDataRequested);
                                Assert.Equal(2, counter);

                                Assert.Equal(0, activity1.Links.Count());
                                Assert.Equal(0, activity1.Events.Count());
                                Assert.True(object.ReferenceEquals(activity1, activity1.AddLink(new ActivityLink(new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None, null)))));
                                Assert.True(object.ReferenceEquals(activity1, activity1.AddEvent(new ActivityEvent("e1"))));
                                Assert.Equal(1, activity1.Links.Count());
                                Assert.Equal(1, activity1.Events.Count());
                            }
                            Assert.Equal(1, counter);
                        }

                        Assert.Equal(0, counter);
                    }
                }
            }).Dispose();
        }

        [Fact]
        public void TestActivitySourceAttachedObject()
        {
            RemoteExecutor.Invoke(() => {
                // All Activities created through the constructor should have same source.
                Assert.True(object.ReferenceEquals(new Activity("a1").Source, new Activity("a2").Source));
                Assert.True(string.IsNullOrEmpty(new Activity("a3").Source.Name));
                Assert.Equal(new Version(), new Activity("a4").Source.Version);

                using (ActivitySource aSource = new ActivitySource("SourceToTest", new Version(1, 2, 3, 4)))
                {
                    //Ensure at least we have a listener to allow Activity creation
                    using (IDisposable listener1 = ActivitySource.AddActivityListener(
                                    (activitySource) => object.ReferenceEquals(aSource, activitySource),
                                    (source, name, kind, context, tags, links) => ActivityDataRequest.AllData,
                                    (source, name, kind, parentId, tags, links) => ActivityDataRequest.AllData,
                                    activity => Assert.NotNull(activity), activity => Assert.NotNull(activity)))
                    {
                        using (Activity activity = aSource.StartActivity("ActivityToTest"))
                        {
                            Assert.True(object.ReferenceEquals(aSource, activity.Source));
                        }
                    }
                }
            }).Dispose();
        }

        public void Dispose() => Activity.Current = null;
    }
}
