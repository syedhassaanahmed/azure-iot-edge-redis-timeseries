# azure-iot-edge-redis-timeseries
[![Build Status](https://dev.azure.com/syedhassaanahmed/azure-iot-edge-redis-timeseries/_apis/build/status/syedhassaanahmed.azure-iot-edge-redis-timeseries?branchName=main)](https://dev.azure.com/syedhassaanahmed/azure-iot-edge-redis-timeseries/_build/latest?definitionId=11&branchName=main)

This sample [Azure IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/?view=iotedge-2020-11) solution demonstrates how to store timeseries data from IoT sensors to [RedisTimeSeries](https://oss.redislabs.com/redistimeseries/). The [Simulated Temperature Sensor](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/azure-iot.simulated-temperature-sensor?tab=overview) Edge module is used to generate temperature, pressure and humidity measurements. These measurements are then [routed](https://docs.microsoft.com/en-us/azure/iot-edge/module-composition?view=iotedge-2020-11) to our custom [C# Edge module](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module?view=iotedge-2020-11) which stores them in RedisTimeSeries.

## Features
- Ability to connect to Redis with [Unix Sockets](https://redis.io/topics/clients) in addition to TCP.
- Scrapes Redis usage metrics in Prometheus format.
- Exposes an [Azure IoT Hub Direct Method](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods) named `GetTimeSeriesInfo`. This method can be used to retrieve statistics about the stored data in RedisTimeSeries.
- Provides Grafana dashboards for visualizing timeseries and above Prometheus data sources, [see below](#grafana-dashboards).

## Architecture
<div style=""><img src="images/data_flow.png"/></center></div>

## Prerequisites
- Azure IoT Hub with a [provisioned Edge device](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-provision-single-device-linux-symmetric?view=iotedge-2020-11&tabs=azure-portal)
- [Docker Community Edition](https://docs.docker.com/get-docker/)
- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Python 3.8](https://www.python.org/downloads/release/python-389/)
- [Azure IoT Edge Dev Tool](https://github.com/Azure/iotedgedev)
- [Visual Studio Code](https://code.visualstudio.com/) with the following extensions
    - [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
    - [Docker](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker)

## Configuration
The `.env` file can be modified for container version, container host names and to use an [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/) instead of a local docker registry.

## Grafana Dashboards
Simulated timeseries dashboard was created using the [Redis Data Source for Grafana](https://github.com/RedisGrafana/grafana-redis-datasource).

<div style=""><img src="images/timeseries_dashboard.png"/></center></div>

Redis usage dashboard was created using the [Prometheus Redis Metrics Exporter](https://github.com/oliver006/redis_exporter).

<div style=""><img src="images/redis_dashboard.png"/></center></div>

## Team
- [Alexander Gassmann](https://github.com/Salazander)
- [Ioana Amarande](https://github.com/Ioana37)
- [Machteld Bögels](https://github.com/machteldbogels)
- [Magda Baran](https://github.com/MagdaPaj)
- [Ville Rantala](https://github.com/vjrantal)
- [Syed Hassaan Ahmed](https://github.com/syedhassaanahmed)
