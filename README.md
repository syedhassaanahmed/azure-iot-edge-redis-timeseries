# azure-iot-edge-redis-timeseries
This sample Azure IoT Edge module demonstrates how to store sensor data in RedisTimeSeries. The [Simulated Temperature Sensor](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/azure-iot.simulated-temperature-sensor?tab=overview) edge module is used to generate temperature readings. These readings are then routed to our custom C# module that stores them in [RedisTimeSeries](https://oss.redislabs.com/redistimeseries/).

<div style=""><img src="images/dataflow.png"/></center></div>
