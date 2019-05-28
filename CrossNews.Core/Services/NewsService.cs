using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CrossNews.Core.Messages;
using CrossNews.Core.Model.Api;
using MvvmCross.Plugin.Messenger;
using Newtonsoft.Json;
using System.Threading.Tasks.Dataflow;

namespace CrossNews.Core.Services
{
    public class NewsService : INewsService
    {
        private readonly IMvxMessenger _messenger;
        private readonly ICacheService _cache;
        private readonly HttpClient _client;

        public NewsService(IMvxMessenger messenger, ICacheService cache)
        {
            _messenger = messenger;
            _cache = cache;
            _client = new HttpClient { BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/") };
        }

        public async Task<List<int>> GetStoryListAsync(StoryKind kind)
        {
            var listTag = GetStorylistTag(kind);

            var response = await _client.GetStringAsync(listTag + ".json");
            var items = JsonConvert.DeserializeObject<List<int>>(response);

            return items;
        }

        public async Task<IEnumerable<Item>> EnqueueItems(List<int> ids)
        {
            var stopwatch = Stopwatch.StartNew();

            var (items, misses) = _cache.GetCachedItems(ids);

            var newItems = new List<Item>();

            var buffer = new BufferBlock<int>();
            var downloader = new ActionBlock<int>(async id =>
            {
                var action = await _client.GetStringAsync($"item/{id}.json");
                // handle data here
                var storyItem = JsonConvert.DeserializeObject<Item>(action);
                newItems.Add(storyItem);
                var msg = new NewsItemMessage(this, storyItem);
                _messenger.Publish(msg);
            },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
            // notify TPL Dataflow to send messages from buffer to loader
            buffer.LinkTo(downloader, new DataflowLinkOptions { PropagateCompletion = true });

            foreach (var itemId in misses)
            {
                await buffer.SendAsync(itemId);
            }
            // queue is done
            buffer.Complete();

            // now it's safe to wait for completion of the downloader
            await downloader.Completion;

            var itemList = items.ToList();
            foreach (var item in itemList)
            {
                var msg = new NewsItemMessage(this, item);
                _messenger.Publish(msg);
            }

            _cache.AddItemsToCache(newItems);
            stopwatch.Stop();
            var ms = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("Queue completed in {0} ms", ms);


            return itemList;
        }

        private string GetStorylistTag(StoryKind kind)
        {
            switch (kind)
            {
                case StoryKind.Top:
                    return "topstories";
                case StoryKind.New:
                    return "newstories";
                case StoryKind.Best:
                    return "beststories";
                case StoryKind.Ask:
                    return "askstories";
                case StoryKind.Show:
                    return "showstories";
                case StoryKind.Job:
                    return "jobstories";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
