using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;
using BGGSlack.Models;

namespace BGGSlack.Controllers
{
    public class GameController : ApiController
    {
        
        private Game SelectMatch(IEnumerable<Game> games, string searchString)
        {
            //if all of them have ranks, return the highest ranked thing
            int dontcare;
            
            if(games.All(g => int.TryParse(g.BGRankNum, out dontcare)))
            {
                return games.OrderBy(g => long.Parse(g.BGRankNum)).First();
            }

            var nameMatch = games.Where(g => g.Name.Equals(searchString, StringComparison.CurrentCultureIgnoreCase));

            //if there is only one with an exact name match, return that one
            if (nameMatch.Count() == 1)
            {
                return nameMatch.First();
            }

            //else return the most popular one
            return games.OrderByDescending(g => g.UsersRated).First();
        }

        public IHttpActionResult GetGame(string token, string team_id, string channel_id, string channel_name, string user_id, string user_name, string command, string text)
        {
            var ids = GetGameIDs(text);

            if (ids.Count() > 0)
            {
                var games = GetGames(ids);
                var game = SelectMatch(games, text);
                PostToChannel(channel_id, game.ToString());
                return Ok();
            }

            return Ok("Not Found");
        }

        //Posts arbitrary content to a Slack channel on B.
        //Concern: Slack (move to a Slack-centric class)
        private void PostToChannel(string channel_id, string result)
        {
            using (var client = new HttpClient())
            {
                client.PostAsync("https://bpower.slack.com/services/hooks/slackbot?token=U8cuNF7CpItFhimCWe4pweMP&channel=" + channel_id, new StringContent(result)).Wait();
            }
        }

        private IEnumerable<Game> GetGames(IEnumerable<string> ids)
        {
            return ids
                .Select(GetThisGame)
                .ToList();
        }

        //For a particular game, get the details
        //TODO: there is probably a BGG API where I can get these
        //details back in one shot, rather than hitting it N times for each match.
        //If not, consider threading this. It's damn slow with lots of matches (i.e. 'Coup')
        private Game GetThisGame(string id)
        {
            string detailUrl = string.Format("http://www.boardgamegeek.com/xmlapi/boardgame/{0}?stats=1", id);
            var detailResult = XDocument.Load(detailUrl);
            var bg = detailResult.Elements().First().Elements().First();
            var year = bg.Element("yearpublished").Value;
            var nameNode = bg.Elements("name").Where(e => e.Attribute("primary") != null && e.Attribute("primary").Value == "true").FirstOrDefault();
            var name = nameNode != null ? nameNode.Value : bg.Elements("name").First().Value;
            var bgRank = bg.Descendants("rank").Where(l => l.Attribute("type").Value == "subtype").FirstOrDefault();
            var family = bg.Descendants("rank").Where(l => l.Attribute("type").Value == "family").FirstOrDefault();
            var bgRankName = bgRank == null ? null : bgRank.Attribute("friendlyname").Value;
            var bgRankNum = bgRank == null ? null : bgRank.Attribute("value").Value;
            var bgFamilyName = family == null ? null : family.Attribute("friendlyname").Value;
            var bgFamilyNum = family == null ? null : family.Attribute("value").Value;
            var usersRated = int.Parse(bg.Descendants("usersrated").First().Value);
            var url = string.Format("http://boardgamegeek.com/boardgame/{0}", id);

            return new Game
            {
                ID = id,
                URL = url,
                BGFamilyName = bgFamilyName,
                BGFamilyNum = bgFamilyNum,
                BGRankName = bgRankName,
                BGRankNum = bgRankNum,
                Year = year,
                Name = name,
                UsersRated = usersRated
            };
        }

        //gets all game IDs that come back from BGG's search API
        private IEnumerable<string> GetGameIDs(string searchCriteria)
        {
            var searchResult = XDocument.Load(string.Format(@"http://www.boardgamegeek.com/xmlapi2/search?query={0}", searchCriteria.Replace(' ', '+')));

            //return a list of results, removing any expansions via de-duplication and type
            return searchResult
                .Descendants("item")
                .GroupBy(i => i.Attribute("id").Value)
                .Where(g => g.Count() == 1 && g.First().Attribute("type").Value == "boardgame")
                .Select(e => e.Key);
        }


        //test method, not for use by Slack
        public IHttpActionResult GetGame(string id)
        {
            var ids = GetGameIDs(id);
            var games = GetGames(ids).ToList();
            var game = SelectMatch(games, id);

            return Ok(game.ToString());
        }

    }
}
