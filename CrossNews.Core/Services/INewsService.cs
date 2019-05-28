using System.Collections.Generic;
using System.Threading.Tasks;
using CrossNews.Core.Model.Api;

namespace CrossNews.Core.Services
{
    public interface INewsService
    {
        Task<List<int>> GetStoryListAsync(StoryKind kind);
        Task<IEnumerable<Item>> EnqueueItems(List<int> ids);
    }

    public enum StoryKind
    {
        Top,
        New,
        Best,
        Ask,
        Show,
        Job
    }
}
