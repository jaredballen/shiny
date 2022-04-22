﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Foundation;
using UserNotifications;
using Microsoft.Extensions.Logging;

namespace Shiny.Notifications
{
    public class ChannelManager : IChannelManager, IShinyStartupTask
    {
        readonly IRepository repository;
        readonly ILogger<ChannelManager> logger;


        public ChannelManager(IRepository repository, ILogger<ChannelManager> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }


        public void Start()
        {
            this.logger.LogInformation("Starting iOS channel manager");
            this.RebuildNativeCategories()
                .ContinueWith(x =>
                {
                    if (x.IsFaulted)
                    {
                        this.logger.LogError("Error rebuilding category catalog", x.Exception);
                    }
                    else
                    {
                        this.logger.LogInformation("iOS channel manager started");
                    }
                });
        }


        public async Task Add(Channel channel)
        {
            channel.AssertValid();
            await this.repository.Set(channel.Identifier, channel).ConfigureAwait(false);
            await this.RebuildNativeCategories().ConfigureAwait(false);
        }


        public async Task Clear()
        {
            await this.repository.Clear<Channel>().ConfigureAwait(false);
            await this.RebuildNativeCategories().ConfigureAwait(false);
        }


        public Task<Channel?> Get(string channelId) => this.repository.Get<Channel>(channelId);
        public Task<IList<Channel>> GetAll() => this.repository.GetList<Channel>();


        public async Task Remove(string channelId)
        {
            await this.repository.Remove<Channel>(channelId).ConfigureAwait(false);
            await this.RebuildNativeCategories().ConfigureAwait(false);
        }


        protected async Task RebuildNativeCategories()
        {
            var channels = await this.GetAll().ConfigureAwait(false);
            var list = channels.ToList();
            list.Add(Channel.Default);

            var categories = new List<UNNotificationCategory>();
            foreach (var channel in list)
            {
                var actions = new List<UNNotificationAction>();
                foreach (var action in channel.Actions)
                {
                    var nativeAction = this.CreateAction(action);
                    actions.Add(nativeAction);
                }

                var native = UNNotificationCategory.FromIdentifier(
                    channel.Identifier,
                    actions.ToArray(),
                    new string[] { "" },
                    UNNotificationCategoryOptions.None
                );
                categories.Add(native);
            }
            var set = new NSSet<UNNotificationCategory>(categories.ToArray());
            UNUserNotificationCenter.Current.SetNotificationCategories(set);
        }


        protected virtual UNNotificationAction CreateAction(ChannelAction action) => action.ActionType switch
        {
            ChannelActionType.TextReply => UNTextInputNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.None,
                action.Title,
                String.Empty
            ),

            ChannelActionType.Destructive => UNNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.Destructive
            ),

            ChannelActionType.OpenApp => UNNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.Foreground
            ),

            ChannelActionType.None => UNNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.None
            ),

            _ => throw new InvalidOperationException("Invalid action type")
        };
    }
}
