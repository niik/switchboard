# Switchboard #

Fully asynchronous C# 5 / .NET4.5 HTTP intermediary server.

Uses [HttpMachine](https://github.com/bvanderveen/httpmachine) for parsing incoming HTTP requests and a naive custom built response parser.

Supports SSL for inbound and outbound connections.

## Uses/Why? ##

I wrote it cause I needed to transparently manipulate requests going from one application to a web service. The application made Http requests to a server and I needed a quick-fix solution for tweaking the requests/responses without either end knowing about it. Since the requests could potentially be rather large I decided it would be a good time to dig into the async goodned in C# 5 and make the middle man server fully asynchronous.

The hack evolved and evolved until it had a life of it's own so I'm putting it out there in case someone has similar problems.

## Is it a web server?

The short answer: no. Longer: It certainly can be used as a web server and it's more than capable of parsing requests and generating responses. But it's primary use case is to read requests and (with or without modification) transmit them to a proper web server and deliver the response back.

### Potential uses
The lib is still really early in development and it's lacking in several aspects but here's some potential _future_ use cases.

 * Load balancing/reverse proxy
 * Reverse proxy with cache (coupled with a good cache provider)
 * In flight message logging for web services either for temporary debugging or more permanent logging when there's zero or little control over the endpoints.

### Notes/TODO ###

There are CancellationTokens sprinkled throughout but they won't do any smart cancellation as of yet.

There's currently no proper logging support, only the debug log.

No timeout support for connections which never gets around to making a request.

The original purpose of Switchboard was to run in a friendly environment. Security hardening is planned but for now it's probably not suited environments facing malicious requests/responses. This is especially true for malicious responses since we currently have our own parser for that.

Future improvment: Ability to establish outbound connection immediately after
inbound connection is established (before request is read)
thread safe openoutboundconnection

## License ##

Licensed under the MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.