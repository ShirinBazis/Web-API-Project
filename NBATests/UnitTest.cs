using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using NBAGame;
using System.Net.Http;
using NBAGame.Controllers;
using NBAGame.Services;

namespace NBATests
{
    public class NBAControllerTests
    {
        private readonly NBAController _controller;

        public NBAControllerTests()
        {
            var mockApiClient = new MockNBAApiClient();
            var mockService = new MockNBAService(mockApiClient);
            _controller = new NBAController(mockService);
        }

        /**
         * Test for GetAllPlayersNames with a valid game id.
         **/
        [Test]
        public async Task GetAllPlayersNames_ValidId_ReturnsOk()
        {
            // Valid
            var gameId = "0022000180";
            var result = await _controller.GetAllPlayersNames(gameId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        /**
        * Test for GetAllPlayersNames with a null game id.
        **/
        [Test]
        public async Task GetAllPlayersNames_NullId_ReturnsBadRequest()
        {
            // Invalid
            string? gameId = null;
            var result = await _controller.GetAllPlayersNames(gameId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }


        /**
        * Test for GetAllActionsByPlayerName with both valid game id and player name.
        **/
        [Test]
        public async Task GetAllActionsByPlayerName_BothInvalid_ReturnsOk()
        {
            // Both valid
            var gameId = "0022000180";
            var playerName = "Gordon";
            var result = await _controller.GetAllActionsByPlayerName(gameId, playerName);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        /**
        * Test for GetAllActionsByPlayerName with invalid game id and valid player name.
        **/
        [Test]
        public async Task GetAllActionsByPlayerName_InvalidId_ReturnsBadRequest()
        {
            // Invalid gameId
            string? gameId = "0";
            var playerName = "Brown";
            var result = await _controller.GetAllActionsByPlayerName(gameId, playerName);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }


        /**
        * Test for GetResultsByPlayerName with both valid game id and player name.
        **/
        [Test]
        public async Task GetResultsByPlayerName_BothValid_ReturnsOk()
        {
            // Both valid
            var gameId = "0022000180";
            var playerName = "Brown";
            var result = await _controller.GetResultsByPlayerName(gameId, playerName);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        /**
        * Test for GetResultsByPlayerName with both invalid game id and player name.
        **/
        [Test]
        public async Task GetResultsByPlayerName_BothInvalid_ReturnsBadRequest()
        {
            // Both invalid 
            string? gameId = "1";
            var playerName = ".";
            var result = await _controller.GetResultsByPlayerName(gameId, playerName);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }


        /**
        * Test for GetGameResults with valid game id.
        **/
        [Test]
        public async Task GetGameResults_ValidGameId_ReturnsBadRequest()
        {
            // Valid 
            string? gameId = "0022000180";
            var result = await _controller.GetGameResults(gameId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        /**
        * Test for GetGameResults with invalid game id.
        **/
        [Test]
        public async Task GetGameResults_InvalidGameId_ReturnsBadRequest()
        {
            // Invalid 
            string? gameId = "1";
            var result = await _controller.GetGameResults(gameId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }


        // Mock NBAService for testing
        public class MockNBAService(INBAApiClient nbaApiClient) : NBAService(nbaApiClient)
        {
        }

        // Mock INBAApiClient for testing
        public class MockNBAApiClient : INBAApiClient
        {
            private readonly NBAApiClient _nbaApiClient;
            private readonly HttpClient _httpClient;
            public MockNBAApiClient()
            {
                _httpClient = new HttpClient();
                _nbaApiClient = new NBAApiClient(_httpClient);
            }
            public async Task<string> GetPlayByPlayDataAsync(string gameId)
            {
               return await _nbaApiClient.GetPlayByPlayDataAsync(gameId);
            }
        }
    }
}