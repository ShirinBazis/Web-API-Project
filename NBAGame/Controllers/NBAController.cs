using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NBAGame.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NBAGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NBAController(NBAService nbaService) : ControllerBase
    {
        private readonly NBAService _nbaService = nbaService ?? throw new ArgumentNullException(nameof(nbaService));

        /**
         * Get method that gets a game id and returns all player's names that played in the given
         * game, grouped by the team they belong to (the "home" or "away" teams).
         **/
        [HttpGet("allPlayersNames/{gameId}", Name = "GetAllPlayersNames")]
        public async Task<ActionResult<Dictionary<string, HashSet<string>>>> GetAllPlayersNames(string gameId)
        {
            try
            {
                var playersByTeam = await _nbaService.GetAllPlayersNames(gameId);
                return playersByTeam.Count == 0 ? BadRequest($"There are no actions associated with players in this game") : Ok(playersByTeam);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /**
        * Get method that gets game id and player id, and returns all actions (non duplicated 
        * action types) for a given player, that he made throughout the entire game.
        **/
        [HttpGet("actionsByPlayerName/{gameId}/{playerName}", Name = "GetAllActionsByPlayerName")]
        public async Task<ActionResult<List<string>>> GetAllActionsByPlayerName(string gameId, string playerName)
        {
            try
            {
                var actions = await _nbaService.GetAllActionsByPlayerName(gameId, playerName);

                return actions.Count == 0 ? BadRequest($"{playerName} doesn't have associated actions in this game") : Ok(actions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /**
        * *Bonus feature 1* that gets game id and player id, and returns the most relevant results
        * of the player in this game (Total points, goals, steals, blocks, rebounds, turnovers and
        * fouls), in addition to 2 important ratios I chose: 
        * 1. Contribution ratio- positive actions VS. negative actions.
        * 2. Player total points from the team total points in this game.
        **/
        [HttpGet("resultsByPlayerName/{gameId}/{playerName}", Name = "GetResultsByPlayerName")]
        public async Task<ActionResult<int>> GetResultsByPlayerName(string gameId, string playerName)
        {
            try
            {
                var results = await _nbaService.GetResultsByPlayerName(gameId, playerName);
                return results == "" ? BadRequest($"{playerName} doesn't have associated actions in this game") : Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /**
        * *Bonus feature 2* that gets game id and returns the results of the game:
        * The team who won and the final score of each team.
        **/
        [HttpGet("gameResults/{gameId}", Name = "GetgameResults")]
        public async Task<ActionResult<string>> GetGameResults(string gameId)
        {
            try
            {
                var actionsToken = await _nbaService.GetActionsToken(gameId) ?? throw new Exception("There are no actions");
                var results = _nbaService.GetFinalScores(actionsToken);
                int? scoreHome = results[0];
                int? scoreAway = results[1];
                if (scoreHome != null && scoreAway != null)
                {
                    return scoreHome > scoreAway ? Ok($"Home won!\n Home: {scoreHome} points\n Away: {scoreAway} points") :
                        Ok($"Away won!\n Away: {scoreAway} points\n Home: {scoreHome} points");
                }
                return BadRequest("Game didn't end yet");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}

