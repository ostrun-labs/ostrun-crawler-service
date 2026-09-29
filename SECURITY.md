# Security Policy

This service makes HTTP requests to URLs that other services send it. That makes server-side request forgery the main risk, and we take reports seriously.

## Supported versions

The project is pre-1.0. Only the latest commit on `main` receives fixes.

## Reporting a vulnerability

Please do not open a public issue, pull request or discussion for a security problem.

Report it privately through GitHub instead: open the repository's **Security** tab and click **Report a vulnerability**. Include:

- what is affected (endpoint, component, configuration)
- steps to reproduce, or a proof of concept
- the impact you expect

You should get a first response within a week. Once a fix is ready, we release it and publish the advisory, crediting you unless you ask otherwise.

## Scope

In scope: the code in this repository, its Dockerfile, and the default configuration it ships with. That covers any way to make the crawler reach a host outside `Crawler__AllowedHosts`, or reach a loopback, private, link-local or reserved address, for example through DNS rebinding, redirects, proxies or IPv6 address forms.

Out of scope: deployments that override the documented configuration, such as allowlisting a host that serves internal content or exposing the API to untrusted callers with no auth in front. Publicly known vulnerabilities in third-party dependencies are out of scope too. Please report those upstream.
