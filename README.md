# IB Gateway Health Checker

Continously monitors IB gateway to ensure the connection is active and healthy.

Simply start IB gateway then run the program then start IQFeed. Run
`ibgateway-health-checker.exe --help` for more options.

## Development

Building can be conducted via Visual Studio or via a container. To build the
container, run:

    docker build -t okinta/ibgateway-health-checker .
