using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Eventing.Reader;
using static System.Math;
using static NBAGame.Services.NBAService;

namespace NBAGame.Services
{
    public class NBAService(INBAApiClient nbaApiClient)
    {
        private readonly INBAApiClient _nbaApiClient = nbaApiClient;

        public enum TeamType
        {
            Home,
            Away
        }

        /**
        * Function that returns the actions token (happends many times in the project,
        * therefore created to avoid repeating code).
        **/
        public async Task<JToken?> GetActionsToken(string gameId)
        {
            try
            {
                var playByPlayData = await _nbaApiClient.GetPlayByPlayDataAsync(gameId);
                var parsedData = JObject.Parse(playByPlayData);
                return parsedData?["game"]?["actions"];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /**
         * Function that defines which team is the "Home" and which is the "Away",
         * by the tricode names of the teams in the game.
         **/
        public Dictionary<string, TeamType> ConvertTricodeToTeamType(JToken actionsToken)
        {
            try
            {
                Dictionary<string, TeamType> convertedTypes = [];
                bool first = true;
                string? firstTricode = "", currentTricode = "";
                foreach (var action in actionsToken)
                {
                    currentTricode = (string?)action["teamTricode"];
                    if (currentTricode != null)
                    {
                        // After the first goal in the game, the scores would certainly be: 0 vs. X goals to the group did the goal
                        if (first && (string?)(action["shotResult"] ?? null) == "Made")
                        {
                            if ((int?)action["scoreHome"] > (int?)action["scoreAway"])
                            {
                                firstTricode = currentTricode;
                                convertedTypes[firstTricode] = TeamType.Home;
                                first = false;
                            }
                        }
                        else if (!first && currentTricode != firstTricode)
                        {
                            convertedTypes[currentTricode] = TeamType.Away;
                            return convertedTypes;
                        }
                    }
                }
                return [];
            }
            catch (Exception)
            {
                return [];
            }
        }

        /**
         * Functionality of GetAllPlayersNames method in the controller.
         **/
        public async Task<Dictionary<string, HashSet<string>>> GetAllPlayersNames(string gameId)
        {
            try
            {
                var actionsToken = await GetActionsToken(gameId) ?? throw new Exception("There are no actions");
                var convertedTeamTypes = ConvertTricodeToTeamType(actionsToken);
                Dictionary<string, HashSet<string>> playersByTeam = [];
                foreach (var action in actionsToken)
                {
                    string? team = (string?)action["teamTricode"];
                    if (team == null)
                    {
                        continue;
                    }
                    string? teamType = convertedTeamTypes.Count == 0 ? team : convertedTeamTypes[team].ToString();
                    string? playerName = (string?)action["playerName"];
                    if (!string.IsNullOrWhiteSpace(team) && !string.IsNullOrWhiteSpace(playerName))
                    {
                        if (!playersByTeam.TryGetValue(teamType, out HashSet<string>? value))
                        {
                            value = ([]);
                            playersByTeam[teamType] = value;
                        }
                        if (!string.IsNullOrWhiteSpace(playerName))
                        {
                            value.Add(playerName);
                        }
                    }
                }
                return playersByTeam;
            }
            catch (Exception)
            {
                return [];
            }
        }


        /**
         * Functionality of GetAllPlayersNames method in the controller.
         **/
        public async Task<HashSet<string?>> GetAllActionsByPlayerName(string gameId, string playerName)
        {
            try
            {
                var actionsToken = await GetActionsToken(gameId) ?? throw new Exception("There are no actions");
                var actions = new HashSet<string?>();
                foreach (var action in actionsToken)
                {
                    string? currentPlayerName = (string?)action["playerName"];
                    if (currentPlayerName != null && currentPlayerName == playerName)
                    {
                        string? actionType = (string?)action["actionType"];
                        actions.Add(actionType);
                    }
                }
                return actions;
            }
            catch (Exception)
            {
                return [];
            }
        }


        /**
         * Function that returns the final score of each team in the game.
         **/
        public int?[] GetFinalScores(JToken actionsToken)
        {
            try
            {
                int actionCount = actionsToken.Count();
                // Iterate through the actions in reverse order
                for (int i = actionCount - 1; i >= 0; i--)
                {
                    var action = actionsToken[i];
                    if (action == null) { break; }
                    string? actionType = (string?)action["actionType"];
                    string? subType = (string?)action["subType"];
                    if (actionType == "game" && subType == "end")
                    {
                        int? scoreHome = (int?)action["scoreHome"];
                        int? scoreAway = (int?)action["scoreAway"];
                        return [scoreHome, scoreAway];
                    }
                }
                return [];
            }
            catch (Exception)
            {
                return [];
            }
        }


        /**
         * Function that calculates the 2 special ratios I chose (contribution and points).
         **/
        private double[] CalculateRatios(int points, int steals, int blocks, int rebounds, int turnovers, int fouls, JToken actionsToken, string? teamTricode)
        {
            try
            {
                // Calculate the player contribution ratio
                double positiveGains = points + steals + blocks + rebounds;
                double negativeActions = turnovers + fouls;
                double relevantActionsCount = points + steals + blocks + rebounds + turnovers + fouls;
                double contributionRatio = (positiveGains - negativeActions) / relevantActionsCount;
                contributionRatio = Max(contributionRatio, 0);
                contributionRatio = Round(contributionRatio, 2);

                // Calculate the player points ratio
                var results = GetFinalScores(actionsToken);
                int? scoreHome = results[0];
                int? scoreAway = results[1];
                if (scoreHome != null && scoreAway != null && teamTricode != null)
                {
                    var convertedTeamTypes = ConvertTricodeToTeamType(actionsToken);

                    int maxEfficiency = convertedTeamTypes[teamTricode] == TeamType.Home ? (int)scoreHome : (int)scoreAway;
                    double pointsRatio = Round(points / (double)maxEfficiency, 2);
                    return [contributionRatio, pointsRatio];
                }
                return [];
            }
            catch (Exception)
            {
                return [];
            }
        }


        /**
         * Functionality of GetResultsByPlayerName method in the controller.
         **/
        public async Task<string> GetResultsByPlayerName(string gameId, string playerName)
        {
            try
            {
                var actionsToken = await GetActionsToken(gameId) ?? throw new Exception("There are no actions");
                int goals = 0, points = 0, steals = 0, blocks = 0, rebounds = 0, turnovers = 0, fouls = 0;
                bool playerExists = false;
                string? teamTricode = null;

                foreach (var action in actionsToken)
                {
                    string? currentPlayerName = (string?)action["playerName"];
                    string? shotResult = action["shotResult"]?.ToString();
                    string? actionType = (string?)action["actionType"];
                    int? pointsTotal = (int?)action["pointsTotal"];
                    if (currentPlayerName != null && currentPlayerName == playerName)
                    {
                        playerExists = true;
                        teamTricode = (string?)action["teamTricode"];
                        if (shotResult == "Made")
                        {
                            goals++;
                            points = pointsTotal == null ? 0 : (int)pointsTotal;
                        }
                        switch (actionType)
                        {
                            case "steal":
                                steals++;
                                break;
                            case "block":
                                blocks++;
                                break;
                            case "rebound":
                                rebounds++;
                                break;
                            case "turnover":
                                turnovers++;
                                break;
                            case "foul":
                                fouls++;
                                break;
                        }
                    }
                }
                double[] ratios = CalculateRatios(points, steals, blocks, rebounds, turnovers, fouls, actionsToken, teamTricode);
                return !playerExists ? "" : $"**{playerName} results in this game:**\n" +
                $" Total Points: {points}\n Goals: {goals}\n Steals: {steals}\n" +
                $" Blocks: {blocks}\n Rebounds: {rebounds}\n\n" +
                $" Turn Overs: {turnovers}\n Fouls: {fouls}\n\n" +
                $" *Contribution Ratio:* {ratios[0]}\n *Player Points From Team Points:* {ratios[1]}";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
