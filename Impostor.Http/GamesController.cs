namespace Impostor.Http;

using System.Net;
using System.Text.Json.Serialization;
using Impostor.Api.Config;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/**
 * <summary>
 * This controller has method to get a list of public games, join by game and create new games.
 * </summary>
 */
[Route("/api/games")]
[ApiController]
public class GamesController : ControllerBase
{
    private readonly IGameManager gameManager;
    private readonly ListingManager listingManager;
    private readonly HostServer hostServer;

    /**
     * <summary>
     * Initializes a new instance of the <see cref="GamesController"/> class.
     * </summary>
     * <param name="gameManager">GameManager containing a list of games.</param>
     * <param name="listingManager">ListingManager responsible for filtering.</param>
     * <param name="serverConfig">Impostor configuration section containing the public ip address of this server.</param>
     */
    public GamesController(IGameManager gameManager, ListingManager listingManager, IOptions<ServerConfig> serverConfig)
    {
        this.gameManager = gameManager;
        this.listingManager = listingManager;
        var config = serverConfig.Value;
        this.hostServer = new(new(IPAddress.Parse(config.PublicIp), config.PublicPort));
    }

    /**
     * <summary>
     * Get a list of active games.
     * </summary>
     * <param name="mapId">Maps that are requested.</param>
     * <param name="lang">Preferred chat language.</param>
     * <param name="numImpostors">Amount of impostors. 0 is any.</param>
     * <returns>An array of game listings.</returns>
     */
    [HttpGet]
    public IActionResult Index(MapFlags mapId, GameKeywords lang, int numImpostors)
    {
        IEnumerable<IGame> listings = this.listingManager.FindListings(this.HttpContext, mapId, numImpostors, lang);
        return this.Ok(listings.Select(l => new GameListing(l)));
    }

    /**
     * <summary>
     * Get the address a certain game is hosted at.
     * </summary>
     * <param name="gameId">The id of the game that should be retrieved.</param>
     * <returns>The server this game is hosted on.</returns>
     */
    [HttpPost]
    public IActionResult Post(int gameId)
    {
        GameCode code = new(gameId);
        IGame? game = this.gameManager.Find(code);

        // If the game was not found, print an error message.
        if (game == null)
        {
            return this.NotFound(new MatchmakerResult(new MatchmakerError(DisconnectReason.GameMissing)));
        }

        return this.Ok(new HostServer(game.PublicIp));
    }

    /**
     * <summary>
     * Get the address to host a new game on.
     * </summary>
     * <returns>The address of this server.</returns>
     */
    [HttpPut]
    public IActionResult Put()
    {
        return this.Ok(this.hostServer);
    }

    private static uint IpToInt(IPEndPoint ip)
    {
        byte[] ipBytes = ip.Address.GetAddressBytes();
        return (uint)((ipBytes[3] << 24) | (ipBytes[2] << 16) | (ipBytes[1] << 8) | ipBytes[0]);
    }

    private struct HostServer
    {
        public HostServer(IPEndPoint endpoint)
        {
            this.Ip = IpToInt(endpoint);
            this.Port = endpoint.Port;
        }

        [JsonPropertyName("Ip")]
        public readonly long Ip { get; }

        [JsonPropertyName("Port")]
        public readonly int Port { get; }
    }

    private struct MatchmakerResult
    {
        public MatchmakerResult(MatchmakerError error)
        {
            this.Errors = new[] { error };
        }

        [JsonPropertyName("Errors")]
        public MatchmakerError[] Errors { get; }
    }

    private struct MatchmakerError
    {
        public MatchmakerError(DisconnectReason reason)
        {
            this.Reason = reason;
        }

        [JsonPropertyName("Reason")]
        public DisconnectReason Reason { get; }
    }

    private struct GameListing
    {
        public GameListing(IGame game)
        {
            this.Ip = IpToInt(game.PublicIp);
            this.Port = (ushort)game.PublicIp.Port;
            this.GameId = game.Code.Value;
            this.PlayerCount = game.PlayerCount;
            this.HostName = game.Host?.Client.Name ?? "Unknown host";
            this.HostPlatformName = "test";
            this.Platform = Platforms.StandaloneSteamPC;
            this.MaxPlayers = game.Options.MaxPlayers;
            this.NumImpostors = game.Options.NumImpostors;
            this.MapId = game.Options.Map;
            this.Language = game.Options.Keywords;
        }

        [JsonPropertyName("IP")]
        public uint Ip { get; }

        [JsonPropertyName("Port")]
        public ushort Port { get; }

        [JsonPropertyName("GameId")]
        public int GameId { get; }

        [JsonPropertyName("PlayerCount")]
        public int PlayerCount { get; }

        [JsonPropertyName("HostName")]
        public string HostName { get; }

        [JsonPropertyName("HostPlatformName")]
        public string HostPlatformName { get; }

        [JsonPropertyName("Platform")]
        public Platforms Platform { get; }

        [JsonPropertyName("Age")]
        public int Age { get; } = 0;

        [JsonPropertyName("MaxPlayers")]
        public int MaxPlayers { get; }

        [JsonPropertyName("NumImpostors")]
        public int NumImpostors { get; }

        [JsonPropertyName("MapId")]
        public MapTypes MapId { get; }

        [JsonPropertyName("Language")]
        public GameKeywords Language { get; }
    }
}
