# azure-iot-edge-redis-timeseries
This sample [Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/?view=iotedge-2020-11) solution demonstrates how to store timeseries data from IoT sensors to [RedisTimeSeries](https://oss.redislabs.com/redistimeseries/). The [Simulated Temperature Sensor](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/azure-iot.simulated-temperature-sensor?tab=overview) Edge module is used to generate temperature, pressure and humidity measurements. These measurements are then [routed](https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2020-11) to our custom [C# Edge module](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module?view=iotedge-2020-11) which stores them in RedisTimeSeries.

## Features
- Connects to Redis with [Unix Sockets](https://redis.io/topics/clients) instead of the TCP port.
- Makes .NET runtime metrics available in Prometheus format.
- Scrapes Redis usage metrics in Prometheus format.
- Exposes an [Azure IoT Hub Direct Method](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods) named `GetTimeSeriesInfo`. This method can be used to retrieve statistics about the stored data in RedisTimeSeries.
- Provides Grafana dashboards for visualizing timeseries and above Prometheus data sources, [see below](#grafana-dashboards).

## Architecture
<div style=""><img src="images/data_flow.png"/></center></div>

## Prerequisites
- Azure IoT Hub with an Edge device provisioned
- [Docker Community Edition](https://docs.docker.com/get-docker/)
- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet/3.1)
- [Python 3.8](https://www.python.org/downloads/release/python-389/)
- [Azure IoT EdgeHub Dev Tool](https://github.com/Azure/iotedgehubdev)

### Visual Studio Code
The following extensions are required
- [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [Docker](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker)
- [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)

### Visual Studio 2019
In order to support VS2019, the solution abstracts [ModuleClient class](https://github.com/Azure/azure-iot-sdk-csharp/blob/master/iothub/device/src/ModuleClient.cs) from the C# IoT SDK and provides a [docker-compose project](https://docs.microsoft.com/en-us/visualstudio/containers/overview?view=vs-2019#adding-docker-support) to spin up the custom module container and its dependencies without simulating the IoT Edge runtime. This however means that capabilities such as IoT Edge routing and Direct Methods won't work in VS2019.
>Note: Please make sure to set `docker-compose` as your start project.

## Configuration
The `.env` file can be modified for container version, container host names and to use an [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/) instead of a local docker registry.

## Grafana Dashboards
Simulated timeseries dashboard was created using the [Redis Data Source for Grafana](https://github.com/RedisGrafana/grafana-redis-datasource).

<div style=""><img src="images/timeseries_dashboard.png"/></center></div>

Redis usage dashboard was created using the [Prometheus Redis Metrics Exporter](https://github.com/oliver006/redis_exporter).

<div style=""><img src="images/redis_dashboard.png"/></center></div>

.NET runtime metrics dashboard was created using [prometheus-net.DotNetMetrics](https://github.com/djluck/prometheus-net.DotNetRuntime).

<div style=""><img src="images/dotnet_dashboard.png"/></center></div>
