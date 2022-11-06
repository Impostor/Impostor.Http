namespace Impostor.Http;

using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Innersloth;

/**
 * <summary>
 * Perform game listing filtering.
 * </summary>
 */
public class ListingManager
{
    private readonly IGameManager gameManager;

    private readonly IServiceProvider serviceProvider;

    /**
     * <summary>
     * Initializes a new instance of the <see cref="ListingManager"/> class.
     * </summary>
     * <param name="gameManager">GameManager containing a list of games.</param>
     * <param name="serviceProvider">Used to retrieve a list of IListingFilter's.</param>
     */
    public ListingManager(IGameManager gameManager, IServiceProvider serviceProvider)
    {
        this.gameManager = gameManager;
        this.serviceProvider = serviceProvider;
    }

    /**
     * <summary>
     * Find listings that match the requested settings.
     * </summary>
     * <param name="ctx">The context of this http request.</param>
     * <param name="map">The selected maps.</param>
     * <param name="impostorCount">The amount of impostors. 0 is any.</param>
     * <param name="language">Chat language of the game.</param>
     * <param name="maxListings">Maximum amount of games to return.</param>
     * <returns>Listings that match the required criteria.</returns>
     */
    public IEnumerable<IGame> FindListings(HttpContext ctx, int map, int impostorCount, GameKeywords language, int maxListings = 10)
    {
        int resultCount = 0;

        Func<IGame, bool>[] filters = this.serviceProvider.GetServices<IListingFilter>().Select(f => f.GetFilter(ctx)).ToArray();

        List<IGame> compatibleGames = new List<IGame>();

        // We want to add 2 types of games
        // 1. Desireable games that the player wants to play (right language, right map, desired impostor amount)
        // 2. Failing that, display compatible games the player could join (public games with spots available)

        // .Where filters out games that can't be joined.
        foreach (var game in this.gameManager.Games.Where(x =>
            x.IsPublic &&
            x.GameState == GameStates.NotStarted &&
            x.PlayerCount < x.Options.MaxPlayers))
        {
            bool addGame = true;
            foreach (var filter in filters)
            {
                if (!filter(game))
                {
                    addGame = false;
                    break;
                }
            }

            if (addGame)
            {
                if (IsGameDesired(game, map, impostorCount, language))
                {
                    // Add to result immediately.
                    yield return game;

                    // Break out if we have enough.
                    if (++resultCount == maxListings)
                    {
                        yield break;
                    }
                }
                else
                {
                    // Add to result to add afterwards. Adding is pointless if we already have enough compatible games to fill the list
                    if (compatibleGames.Count < (maxListings - resultCount))
                    {
                        compatibleGames.Add(game);
                    }
                }
            }
        }

        foreach (var game in compatibleGames)
        {
            yield return game;

            if (++resultCount == maxListings)
            {
                yield break;
            }
        }
    }

    private bool IsGameDesired(IGame game, int map, int impostorCount, GameKeywords language)
    {
        if ((map & (1 << (int)game.Options.Map)) == 0)
        {
            return false;
        }

        if (language != game.Options.Keywords)
        {
            return false;
        }

        if (impostorCount != 0 && game.Options.NumImpostors != impostorCount)
        {
            return false;
        }

        return true;
    }
}
