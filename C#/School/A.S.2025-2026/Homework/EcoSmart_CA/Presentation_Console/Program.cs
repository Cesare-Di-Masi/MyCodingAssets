// See https://aka.ms/new-console-template for more information
using System;
using Application.UseCases;
using Infrastructure.Repositories;

var sensorRepository = new JsonSensorRepository();
var sensorService = new SensorService(sensorRepository);

var avgTemp = await sensorService.GetAverageTemperatureLast24hAsync();
Console.WriteLine($"Average Temperature Last 24h: {avgTemp} °C");
var highCO2Sensors = await sensorService.GetHighCO2SensorsAsync();
Console.WriteLine("High CO2 Sensors (>1000 PPM):");
foreach (var sensor in highCO2Sensors)
{
    Console.WriteLine($"SensorId: {sensor.SensorId}, Value: {sensor.Value} {sensor.Type}, Timestamp: {sensor.Timestamp}");
}