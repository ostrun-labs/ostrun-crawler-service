# ostrun-crawler-service

Crawler service for [Ostrun](https://github.com/ostrun-labs/ostrun), provider `Ostrun`, service `crawler`.

Other services send it a list of URLs, each with the HTTP status they expect. The crawler requests them in parallel and reports, for each one, the status it got and whether it matched. It knows nothing about the sites it calls, so the caller decides what a match means. A name checker, for example, sends `https://registry.npmjs.org/<name>` with an expected status of 404 and reads `matched: true` as "the name is free on npm".

## API

### `POST /crawler/fetch`

```json
{
  "targets": [
    { "id": "npm", "url": "https://registry.npmjs.org/my-app", "match": { "status": 404 } },
    { "id": "nuget", "url": "https://api.nuget.org/v3-flatcontainer/my-app/index.json", "match": { "status": 404 } }
  ]
}
```

Response `200`, in request order:

```json
{
  "results": [
    { "id": "npm", "url": "https://registry.npmjs.org/my-app", "status": 404, "matched": true, "error": null },
    { "id": "nuget", "url": "https://api.nuget.org/v3-flatcontainer/my-app/index.json", "status": 200, "matched": false, "error": null }
  ]
}
```

- `id` is yours. It must be unique within the request and comes back as is.
- `status` is `null` when no response came back. `error` then says why: `timeout`, `network_error` or `host_not_allowed`. One failing URL doesn't fail the others.
- Redirects are not followed, so a 301 is reported as 301. Response bodies are not read.
- An invalid request (no targets, too many, duplicate `id`, a URL that isn't http or https, a missing or out-of-range `match.status`) gets a `400` with the errors listed per target.

### `GET /health`

Returns `200` when the service is up.

## Configuration

| Variable | Default | Description |
|---|---|---|
| `Crawler__AllowedHosts` | empty | Comma-separated host names the crawler may request, matched exactly. Empty blocks every host. |
| `Crawler__TimeoutSeconds` | `5` | Timeout per URL. |
| `Crawler__MaxTargets` | `20` | Maximum URLs per request. |
| `Crawler__UserAgent` | `OstrunCrawler/0.1 (+repo URL)` | Sent with every request. Some APIs, GitHub and crates.io among them, reject requests without one. |

Even for an allowlisted host, the crawler refuses to connect when the name resolves to a loopback, private, link-local or other reserved address. It also ignores system proxy settings.

## Run it

```sh
cp .env.example .env
docker compose up --build
curl localhost:8082/crawler/fetch -H 'content-type: application/json' \
  -d '{"targets":[{"id":"npm","url":"https://registry.npmjs.org/react","match":{"status":404}}]}'
```

## Independence

Like every Ostrun service, the crawler has its own `Dockerfile` and its own repo. It has no database and publishes no events for now. Other services reach it through its HTTP API only.

## License

Apache-2.0, see [LICENSE](./LICENSE).
