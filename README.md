# ostrun-crawler-service

Crawler service for [Ostrun](https://github.com/ostrun-labs/ostrun), provider `Ostrun`, service `crawler`.

## Status

Early development. The service builds, runs in Docker on port 8082 and answers `GET /health`. The fetch API and the manifest come next.

## Independence

Like every Ostrun service, the crawler has its own database, its own `Dockerfile` and its own repo. It talks to other services only through its HTTP API or through events on the bus.

## License

Apache-2.0, see [LICENSE](./LICENSE).
