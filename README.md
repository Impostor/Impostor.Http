# Impostor.Http

Adds HTTP matchmaking to [Impostor](https://github.com/Impostor/Impostor)

## Installation

1. [Install Impostor first](https://github.com/Impostor/Impostor/blob/master/docs/Running-the-server.md). You need version 1.8.1 or newer.
2. Install [ASP.NET Core Runtime 7](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) as well
3. Download [Impostor.Http.dll from the GitHub Releases](https://github.com/Impostor/Impostor.Http/releases) and put it in Impostor's `plugin` folder
4. Finally, if you want to change the default configuration, you need to create a configuration file for this plugin. See the next section for this.

## Configuration

Configuration is read from the `config_http.json` file or from environment variables prefixed with `IMPOSTOR_HTTP_`. You can copy over [this file](https://github.com/Impostor/Impostor.Http/blob/main/config_http.json) for the default settings. These are the possible keys:

| Key                 | Default   | Description                                                                                                                                     |
| ------------------- | --------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **ListenIp**        | `0.0.0.0` | IP address to listen on. Use 127.0.0.1 if using a reverse proxy like nginx (recommended), use 0.0.0.0 if exposed directly (not recommended)     |
| **ListenPort**      | 22023     | Port the HTTP matchmaking server is running on.                                                                                                 |
| **UseHttps**        | `false`   | Set to true if using encrypted communication to your reverse proxy or if you're exposing this server directly to the internet (not recommended) |
| **CertificatePath** | _not set_ | If UseHttps is enable, set this property to the path of your SSL certificate in PFX format.                                                     |

## HTTPS configuration

To enable support for Android/iOS devices, you need to enable HTTPS. If you don't need to support phones, you can skip this section. Enabling HTTPS can be done in one of two ways:

### 1. Use a Reverse Proxy like nginx, apache or caddy

The reverse proxy can terminate HTTPS for you. To configure this:

1. Set ListenIp to `127.0.0.1` so that Impostor.Http can only be reached using the Reverse Proxy.
2. Set UseHttps to `false`.
3. Use Let's Encrypt or a similar service to get an SSL certificate. We recommend [Certbot](https://certbot.eff.org/). Self-signed certificates are not enough.
4. Configure your reverse proxy. For nginx, we have a sample config available. For the other servers, please refer to your server's documentation.

<details>
<summary>Nginx server configuration</summary>
Replace YOUR_SERVER_NAME_HERE with the hostname of your server
```nginx
server {
    listen 443 ssl http2;
    server_name YOUR_SERVER_NAME_HERE;

    ssl_certificate /etc/letsencrypt/live/YOUR_SERVER_NAME_HERE/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/YOUR_SERVER_NAME_HERE/privkey.pem;
    ssl_trusted_certificate /etc/letsencrypt/live/YOUR_SERVER_NAME_HERE/fullchain.pem;

    include /etc/nginx/ssl_ciphers; # https://ssl-config.mozilla.org/#server=nginx&version=1.16.1&config=intermediate&openssl=1.1.1d&guideline=5.4

    # generated 2023-03-19, Mozilla Guideline v5.6, nginx 1.17.7, OpenSSL 1.1.1d, intermediate configuration, no HSTS
    # https://ssl-config.mozilla.org/#server=nginx&version=1.17.7&config=intermediate&openssl=1.1.1d&hsts=false&guideline=5.6
    ssl_session_timeout 1d;
    ssl_session_cache shared:MozSSL:10m;  # about 40000 sessions
    ssl_session_tickets off;

    # curl https://ssl-config.mozilla.org/ffdhe2048.txt > /path/to/dhparam
    ssl_dhparam /path/to/dhparam;

    # intermediate configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:DHE-RSA-AES128-GCM-SHA256:DHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    # OCSP stapling
    ssl_stapling on;
    ssl_stapling_verify on;

    location / {
        proxy_pass http://localhost:22000;
        proxy_pass_header Server;
        proxy_buffering off;
        proxy_redirect off;
        proxy_set_header X-Real-IP $remote_addr;  # http://wiki.nginx.org/HttpProxyModule
        proxy_set_header X-Forwarded-For $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Host $host;
        proxy_http_version 1.1;  # recommended with keepalive connections
    }

}

```
</details>

### 2. Use the Https feature of Impostor.Http

We don't recommend using this option for a couple reasons: it is not very flexible as it requires PFX certificates and it needs to be restarted to reload the certificate. But in case you really don't want to use a reverse proxy, here's how to do it:

1. Set ListenIp to `0.0.0.0` so that your server can be reached externally.
2. Set UseHttps to `true`.
3. Use Let's Encrypt or a similar service to get an SSL certificate. We recommend [Certbot](https://certbot.eff.org/). Self-signed certificates are not enough.
4. Convert your certificate to PFX format, for example using OpenSSL: `openssl pkcs12 -export -out certificate_fullchain.pfx -inkey privkey.pem -in fullchain.pem`.
5. Set CertificatePath to the path to this `certificate_fullchain.pfx` file.

```
