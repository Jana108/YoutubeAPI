
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeAPI.Utils;
using YoutubeAPI.AppLogic;
using System.Net.Http;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;

namespace YoutubeAPI.Services
{
    public static class YoutubeClient
    {
        private static YouTubeService? _youtubeService = WebAPI._youtubeService;

        public static async Task<string[]> GetVideoCommentsAsync(string videoID)
        {
            var request = _youtubeService!.CommentThreads.List("snippet");
            request.VideoId = videoID;
            request.MaxResults = 25;

            var response = await request.ExecuteAsync();
            return response.Items.Select(item => item.Snippet.TopLevelComment.Snippet.TextDisplay).ToArray();
        }
    }
}
