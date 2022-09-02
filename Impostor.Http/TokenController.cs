namespace Impostor.Http;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

/**
 * <summary>
 * This controller has a method to get an auth token.
 * </summary>
 */
[Route("/api/user")]
[ApiController]
public class TokenController : ControllerBase
{
    /**
     * <summary>
     * Get an authentication token.
     * </summary>
     * Because we can't validate it anyway, give some garbage back
     * <param name="request">Token parameters that need to be put into the token.</param>
     * <returns>A bare minimum authentication token that the client will accept.</returns>
     */
    [HttpPost]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        Token token = new(request.Puid, request.ClientVersion);

        // Wrap into a Base64 sandwich
        byte[] serialized = JsonSerializer.SerializeToUtf8Bytes(token);
        return this.Ok(Convert.ToBase64String(serialized));
    }

    /**
     * <summary>
     * Body of the token request endpoint.
     * </summary>
     */
    public struct TokenRequest
    {
        /**
         * <summary>
         * Gets or sets the puid of the requester.
         * </summary>
         */
        [JsonPropertyName("Puid")]
        public string Puid { get; set; }

        /**
         * <summary>
         * Gets or sets the client version of the requester.
         * </summary>
         */
        [JsonPropertyName("ClientVersion")]
        public int ClientVersion { get; set; }
    }

    /**
     * <summary>
     * Token that is returned to the user with a "signature".
     * </summary>
     */
    public struct Token
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> struct.
        /// </summary>
        /// <param name="puid">Puid of the token owner.</param>
        /// <param name="clientVersion">Client version of the token owner.</param>
        internal Token(string puid, int clientVersion)
        {
            this.Content = new TokenPayload(puid, clientVersion);
            this.Hash = "impostor_http_was_here";
        }

        /**
         * <summary>
         * Gets the content of the matchmaking token.
         * </summary>
         */
        [JsonPropertyName("Content")]
        public readonly TokenPayload Content { get; }

        /**
         * <summary>
         * Gets the hash of the matchmaking token. Ignored by the client.
         * </summary>
         */
        [JsonPropertyName("Hash")]
        public readonly string Hash { get; }
    }

    /**
     * <summary>
     * Actual token contents.
     * </summary>
     */
    public struct TokenPayload
    {
        private static readonly DateTime DefaultExpiryDate = new(2012, 12, 21);

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenPayload"/> struct.
        /// </summary>
        /// <param name="puid">Puid of the token owner.</param>
        /// <param name="clientVersion">Client version of the token owner.</param>
        internal TokenPayload(string puid, int clientVersion)
        {
            this.Puid = puid;
            this.ClientVersion = clientVersion;
            this.ExpiresAt = DefaultExpiryDate;
        }

        /**
         * <summary>
         * Gets the puid of the matchmaking token.
         * </summary>
         */
        [JsonPropertyName("Puid")]
        public readonly string Puid { get; }

        /**
         * <summary>
         * Gets the client version of the matchmaking token.
         * </summary>
         */
        [JsonPropertyName("ClientVersion")]
        public readonly int ClientVersion { get; }

        /**
         * <summary>
         * Gets the expiry time of the matchmaking token.
         * </summary>
         */
        [JsonPropertyName("ExpiresAt")]
        public readonly DateTime ExpiresAt { get; }
    }
}
