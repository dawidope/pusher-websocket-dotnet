﻿using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using PusherClient.Tests.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace PusherClient.Tests.AcceptanceTests
{
    /// <summary>
    /// Tests for <see cref="PusherEventEmitter"/>.
    /// </summary>
    public partial class EventEmitterTest
    {
        #region Presence channels

        [Test]
        public async Task PusherEventEmitterPresenceChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await PusherEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPresenceChannelActionErrorTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await PusherEventEmitterTestAsync(channelType, raiseEmitterActionError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPresenceChannelUnbindAllListenersTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await PusherEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 2, 3 }).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPresenceChannelUnbindAllGeneralListenersTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await PusherEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0, 1 }).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPresenceChannelUnbindAllTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Presence;
            await PusherEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0, 1, 2, 3 }).ConfigureAwait(false);
        }

        #endregion

        #region Private channels

        [Test]
        public async Task PusherEventEmitterPrivateChannelTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await PusherEventEmitterTestAsync(channelType).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPrivateChannelActionErrorTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await PusherEventEmitterTestAsync(channelType, raiseEmitterActionError: true).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPrivateChannelUnbindListenerTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await PusherEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 3 }).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPrivateChannelUnbindGeneralListenerTestAsync()
        {
            ChannelTypes channelType = ChannelTypes.Private;
            await PusherEventEmitterUnbindTestAsync(channelType, listenersToUnbind: new List<int> { 0 }).ConfigureAwait(false);
        }

        [Test]
        public async Task PusherEventEmitterPrivateChannelMultipleEventHandlersTestAsync()
        {
            // Arrange
            var pusherServer = new PusherServer.Pusher(Config.AppId, Config.AppKey, Config.AppSecret, new PusherServer.PusherOptions()
            {
                HostName = Config.HttpHost,
            });

            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            Dictionary<string, AutoResetEvent> channelEventReceived = new Dictionary<string, AutoResetEvent>
            {
                {"my-event-1",  new AutoResetEvent(false)},
                {"my-event-2",  new AutoResetEvent(false)},
                {"my-event-3",  new AutoResetEvent(false)},
            };

            Dictionary<string, PusherEvent> channelEvent = new Dictionary<string, PusherEvent>
            {
                {"my-event-1",  null},
                {"my-event-2",  null},
                {"my-event-3",  null},
            };

            Dictionary<string, int> channelEventReceivedCount = new Dictionary<string, int>
            {
                {"my-event-1",  0},
                {"my-event-2",  0},
                {"my-event-3",  0},
            };

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync("private-multiple-event-channel").ConfigureAwait(false);

            void Listener1(PusherEvent eventData)
            {
                string key = "my-event-1";
                channelEventReceivedCount[key]++;
                if (eventData.EventName == key)
                {
                    channelEvent[key] = eventData;
                    channelEventReceived[key].Set();
                }
            }

            void Listener2(PusherEvent eventData)
            {
                string key = "my-event-2";
                channelEventReceivedCount[key]++;
                if (eventData.EventName == key)
                {
                    channelEvent[key] = eventData;
                    channelEventReceived[key].Set();
                }
            }

            void Listener3(PusherEvent eventData)
            {
                string key = "my-event-3";
                channelEventReceivedCount[key]++;
                if (eventData.EventName == key)
                {
                    channelEvent[key] = eventData;
                    channelEventReceived[key].Set();
                }
            }

            List<EventTestData> data = new List<EventTestData>()
            {
                new EventTestData { TextField = "1", IntegerField = 1, },
                new EventTestData { TextField = "2", IntegerField = 2, },
                new EventTestData { TextField = "3", IntegerField = 3, },
            };

            // Act
            localChannel.Bind("my-event-1", Listener1);
            localChannel.Bind("my-event-2", Listener2);
            localChannel.Bind("my-event-3", Listener3);
            await pusherServer.TriggerAsync(localChannel.Name, "my-event-1", data[0]).ConfigureAwait(false);
            await pusherServer.TriggerAsync(localChannel.Name, "my-event-2", data[1]).ConfigureAwait(false);
            await pusherServer.TriggerAsync(localChannel.Name, "my-event-3", data[2]).ConfigureAwait(false);

            // Assert
            foreach (var eventReceived in channelEventReceived)
            {
                Assert.IsTrue(eventReceived.Value.WaitOne(TimeSpan.FromSeconds(5)), $"Event not received for {eventReceived.Key}");
            }

            foreach (var eventReceivedCount in channelEventReceivedCount)
            {
                Assert.AreEqual(1, eventReceivedCount.Value, $"#Events received for {eventReceivedCount.Key}");
            }
        }

        #endregion

        #region Test helper functions

        private static PusherEvent CreatePusherEvent(ChannelTypes channelType, string eventName)
        {
            EventTestData data = new EventTestData
            {
                TextField = ExpectedTextField,
                IntegerField = ExpectedIntegerField,
            };
            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                { "channel", ChannelNameFactory.CreateUniqueChannelName(channelType: channelType) },
                { "event", eventName },
                { "data", data },
            };

            PusherEvent result = new PusherEvent(properties, JsonConvert.SerializeObject(properties));
            return result;
        }

        private static void AssertPusherEventData(string expectedData, string actualData)
        {
            Assert.IsNotNull(expectedData);
            Assert.AreEqual(expectedData, actualData);
            EventTestData actual = JsonConvert.DeserializeObject<EventTestData>(actualData);
            Assert.AreEqual(ExpectedIntegerField, actual.IntegerField, nameof(actual.IntegerField));
            Assert.IsNull(actual.NothingField, nameof(actual.NothingField));
            Assert.AreEqual(ExpectedTextField, actual.TextField, nameof(actual.TextField));
        }

        private static void AssertPusherEventsAreEqual(ChannelTypes channelType, PusherEvent expected, PusherEvent actual)
        {
            Assert.IsNotNull(expected, nameof(expected));
            Assert.IsNotNull(actual, nameof(actual));

            if (channelType == ChannelTypes.Presence)
            {
                Assert.IsNull(expected.UserId);
                Assert.IsNotNull(actual.UserId);
            }
            else
            {
                Assert.IsNull(expected.UserId);
                Assert.IsNull(actual.UserId);
            }

            Assert.IsNotNull(actual.ChannelName);
            Assert.AreEqual(expected.ChannelName, actual.ChannelName);

            Assert.IsNotNull(actual.EventName);
            Assert.AreEqual(expected.EventName, actual.EventName);

            AssertPusherEventData(expected.Data, actual.Data);
        }

        private async Task PusherEventEmitterTestAsync(ChannelTypes channelType, bool raiseEmitterActionError = false)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-pusher-event-test";
            AutoResetEvent globalEventReceived = new AutoResetEvent(false);
            AutoResetEvent channelEventReceived = new AutoResetEvent(false);
            AutoResetEvent globalActionErrorReceived = raiseEmitterActionError ? new AutoResetEvent(false) : null;
            AutoResetEvent channelActionErrorReceived = raiseEmitterActionError ? new AutoResetEvent(false) : null;
            string globalActionErrorMsg = "Simulate error in BindAll action.";
            string channelActionErrorMsg = "Simulate error in Bind action.";
            PusherException globalActionError = null;
            EventEmitterActionException<PusherEvent> channelActionError = null;
            PusherEvent globalEvent = null;
            PusherEvent channelEvent = null;
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            if (raiseEmitterActionError)
            {
                void HandleError(object sender, PusherException error)
                {
                    if (error.Message == globalActionErrorMsg)
                    {
                        globalActionError = error;
                        globalActionErrorReceived?.Set();
                    }
                    else if (error.Message.Contains(channelActionErrorMsg))
                    {
                        channelActionError = error as EventEmitterActionException<PusherEvent>;
                        channelActionErrorReceived?.Set();
                    }

                    if (raiseEmitterActionError)
                    {
                        throw new InvalidOperationException("Simulated error from error handler.");
                    }
                }

                localPusher.Error += HandleError;
            }

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel remoteChannel = await _remoteClient.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);

            void GeneralListener(string eventName, PusherEvent eventData)
            {
                if (eventName == testEventName)
                {
                    globalEvent = eventData;
                    globalEventReceived.Set();
                    if (raiseEmitterActionError)
                    {
                        throw new PusherException(globalActionErrorMsg, ErrorCodes.Unknown);
                    }
                }
            }

            void Listener(PusherEvent eventData)
            {
                channelEvent = eventData;
                channelEventReceived.Set();
                if (raiseEmitterActionError)
                {
                    throw new InvalidOperationException(channelActionErrorMsg);
                }
            }

            // Act
            localPusher.BindAll(GeneralListener);
            localChannel.Bind(testEventName, Listener);
            remoteChannel.Trigger(testEventName, pusherEvent.Data);

            // Assert
            Assert.IsTrue(globalEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(channelEventReceived.WaitOne(TimeSpan.FromSeconds(5)));
            if (raiseEmitterActionError)
            {
                Assert.IsTrue(globalActionErrorReceived.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.IsTrue(channelActionErrorReceived.WaitOne(TimeSpan.FromSeconds(5)));
                Assert.IsNotNull(globalActionError);
                Assert.IsNotNull(channelActionError);
                Assert.AreEqual(ErrorCodes.EventEmitterActionError, channelActionError.PusherCode);
                Assert.IsNotNull(channelActionError.EventData);
                Assert.AreEqual(testEventName, channelActionError.EventName);
            }

            AssertPusherEventsAreEqual(channelType, pusherEvent, globalEvent);
            AssertPusherEventsAreEqual(channelType, pusherEvent, channelEvent);
        }

        private async Task PusherEventEmitterUnbindTestAsync(ChannelTypes channelType, IList<int> listenersToUnbind)
        {
            // Arrange
            Pusher localPusher = PusherFactory.GetPusher(channelType: ChannelTypes.Presence, saveTo: _clients);
            string testEventName = "client-pusher-event-test";
            PusherEvent pusherEvent = CreatePusherEvent(channelType, testEventName);
            string[] testEventNames = new string[] { testEventName, testEventName, testEventName, testEventName, };
            AutoResetEvent[] receivedEvents = new AutoResetEvent[testEventNames.Length];
            int[] numberEventsReceived = new int[testEventNames.Length];
            int[] totalEventsExpected = new int[testEventNames.Length];
            bool[] eventExpected = new bool[testEventNames.Length];
            for (int i = 0; i < testEventNames.Length; i++)
            {
                receivedEvents[i] = new AutoResetEvent(false);
                numberEventsReceived[i] = 0;
                eventExpected[i] = true;
                if (listenersToUnbind.Contains(i)) totalEventsExpected[i] = 1;
                else totalEventsExpected[i] = 2;
            }

            await localPusher.ConnectAsync().ConfigureAwait(false);
            Channel remoteChannel = await _remoteClient.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);
            Channel localChannel = await localPusher.SubscribeAsync(pusherEvent.ChannelName).ConfigureAwait(false);

            void Listener(int index, string eventName)
            {
                if (eventName == testEventNames[index])
                {
                    numberEventsReceived[index]++;
                    if (eventExpected[index])
                    {
                        receivedEvents[index].Set();
                    }
                }
            }

            void GeneralListener0(string eventName, PusherEvent eventData)
            {
                Listener(0, eventName);
            }

            void GeneralListener1(string eventName, PusherEvent eventData)
            {
                Listener(1, eventName);
            }

            void Listener2(PusherEvent eventData)
            {
                Listener(2, eventData.EventName);
            }

            void Listener3(PusherEvent eventData)
            {
                Listener(3, eventData.EventName);
            }

            localPusher.BindAll(GeneralListener0);
            localPusher.BindAll(GeneralListener1);
            localChannel.Bind(testEventName, Listener2);
            localChannel.Bind(testEventName, Listener3);
            await remoteChannel.TriggerAsync(testEventName, pusherEvent.Data).ConfigureAwait(false);
            for (int i = 0; i < testEventNames.Length; i++)
            {
                Assert.IsTrue(receivedEvents[i].WaitOne(TimeSpan.FromSeconds(5)), $"receivedEvents[{i}]");
                receivedEvents[i].Reset();
            }

            TimeSpan delayAfterTrigger = TimeSpan.FromMilliseconds(0);
            foreach (int index in listenersToUnbind)
            {
                eventExpected[index] = false;
            }

            // Act
            if (listenersToUnbind.Count == testEventNames.Length)
            {
                // Not expecting any events, so wait a bit and ensure that none come in.
                delayAfterTrigger = TimeSpan.FromMilliseconds(500);
                localPusher.UnbindAll();
                localChannel.UnbindAll();
            }
            else
            {
                if (listenersToUnbind.Contains(0)) localPusher.Unbind(GeneralListener0);
                if (listenersToUnbind.Contains(1)) localPusher.Unbind(GeneralListener1);
                if (listenersToUnbind.Contains(2) && listenersToUnbind.Contains(3)) localChannel.Unbind(testEventName);
                else
                {
                    if (listenersToUnbind.Contains(2)) localChannel.Unbind(testEventName, Listener2);
                    if (listenersToUnbind.Contains(3)) localChannel.Unbind(testEventName, Listener3);
                }
            }

            await remoteChannel.TriggerAsync(testEventName, pusherEvent.Data).ConfigureAwait(false);
            await Task.Delay(delayAfterTrigger).ConfigureAwait(false);

            // Assert
            for (int i = 0; i < testEventNames.Length; i++)
            {
                if (eventExpected[i])
                {
                    Assert.IsTrue(receivedEvents[i].WaitOne(TimeSpan.FromSeconds(5)), $"receivedEvents[{i}]");
                }

                Assert.AreEqual(totalEventsExpected[i], numberEventsReceived[i], $"# Event[{i}]");
            }
        }

        #endregion
    }
}
