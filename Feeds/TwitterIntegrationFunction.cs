using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using Feeds.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using TweetSharp;
using YamlDotNet.Serialization;

namespace Feeds
{
    public static class TwitterIntegrationFunction
    {
        [FunctionName("TwitterIntegration")]
        public static void Run([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            var consumerKey = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_KEY"];
            var consumerSecret = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_SECRET"];
            var consumerAccessToken = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN"];
            var consumerAccessTokenSecret = ConfigurationManager.AppSettings["TWITTER_YORKDEVELOPERS_CONSUMER_ACCESS_TOKEN_SECRET"];

            const string SOURCEFILENAME = "_data/Events.yml";

            var gitHub = new GitHub(log);
            var deserializer = new Deserializer();

            var yaml = gitHub.ReadFileFromGitHub(SOURCEFILENAME);
            var Events = deserializer.Deserialize<List<Common>>(yaml);

            var twitterApp = new TwitterService(consumerKey, consumerSecret);
            twitterApp.AuthenticateWith(consumerAccessToken, consumerAccessTokenSecret);

            foreach (var evt in Events)
            {
                var daysUntilEvent = (evt.Starts - DateTime.Now).Days;
                if (evt.Endorsed)
                {
                    log.Info($"{daysUntilEvent} days until {evt.Name}");
                }
                if ((daysUntilEvent == 6) && evt.Endorsed)
                {
                    log.Info($"Tweeting about {evt.Name}");
                    SendTweet(twitterApp, evt.Name + " " + evt.Starts.ToString("d MMM yyyy") + " " + evt.URL + " #CodeYork #TechForYork");
                }
            }

        }

        public static TwitterStatus SendTweet(TwitterService twitterApp, string tweetText)
        {
            var options = new TweetSharp.SendTweetOptions() { Status = tweetText };
            return twitterApp.SendTweet(options);
        }


        public static string RemoveSpacesAndTitleCase(string strToConvert)
        {
            // Creates a TextInfo based on the "en-US" culture.
            var myTI = new CultureInfo("en-US", false).TextInfo;

            // Changes a string to titlecase, then replace the spaces with empty
            return myTI.ToTitleCase(strToConvert).Replace(" ", "");
        }


    }
}