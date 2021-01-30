﻿using AirTrafficInfoContracts;
using AirTrafficInfoServices;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plane
{
    public class PlaneService : IAirTrafficService
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly HttpClient _httpClient;
        private PlaneContract _planeContract;

        public PlaneService(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
            _httpClient = new HttpClient();
            _planeContract = new PlaneContract
            {
                Name = "Plane_" + _hostEnvironment.EnvironmentName + "_" + new Random().Next(1001, 9999).ToString(),
                SpeedInMetersPerSecond = 3000000
            };
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //TODO: move to app settings 
            var url = _hostEnvironment.EnvironmentName == "Docker"
                ? $"http://airtrafficinfo_1:80/api/airtrafficinfo/UpdatePlaneInfo"
                : $"https://localhost:44389/api/airtrafficinfo/UpdatePlaneInfo";

            await SetupDestinationAndDepartureAirportsForNewPlane();

            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdatePlane();

                await _httpClient.PostAsync(
                    url,
                    new StringContent(JsonConvert.SerializeObject(_planeContract),
                    Encoding.UTF8, "application/json"));

                await Task.Delay(600, stoppingToken);
            }
        }

        private async Task SetupDestinationAndDepartureAirportsForNewPlane()
        {
            var retryCount = 0;
            var airports = await GetCurrentlyAvailableAirports();

            if (airports.Count < 2)
            {
                _planeContract.DestinationAirport = null;
                _planeContract.DepartureAirport = null;

                return;
            }

            while (retryCount < 15)
            {
                var departureAirport = SelectRandomAirport(airports);
                var destinationAirport = SelectRandomAirport(airports);

                if (departureAirport != destinationAirport)
                {
                    _planeContract.DepartureAirport = departureAirport;
                    _planeContract.DestinationAirport = destinationAirport;
                    _planeContract.DepartureTime = DateTime.Now;
                    _planeContract.LastPositionUpdate = DateTime.Now;

                    break;
                }

                retryCount++;
            }
        }

        private async Task SelectNewDestinationAirport()
        {
            var retryCount = 0;
            var airports = await GetCurrentlyAvailableAirports();

            if (airports.Count < 2)
            {
                _planeContract.DestinationAirport = null;
                _planeContract.DepartureAirport = null;

                return;
            }

            while (retryCount < 15)
            {
                var newDestinationAirport = SelectRandomAirport(airports);

                if (_planeContract.DestinationAirport != newDestinationAirport)
                {
                    _planeContract.DepartureAirport = _planeContract.DestinationAirport;
                    _planeContract.DestinationAirport = newDestinationAirport;
                    _planeContract.DepartureTime = DateTime.Now;
                    _planeContract.Color = newDestinationAirport.Color;

                    break;
                }

                retryCount++;
            }
        }

        private async Task<List<AirportContract>> GetCurrentlyAvailableAirports()
        {
            var url = _hostEnvironment.EnvironmentName == "Docker"
                ? $"http://airtrafficinfo_1:80/api/airtrafficinfo/GetAirports"
                : $"https://localhost:44389/api/airtrafficinfo/GetAirports";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            var airports = JsonConvert.DeserializeObject<List<AirportContract>>(json);

            return airports;
        }

        private AirportContract SelectRandomAirport(List<AirportContract> airports)
        {
            return airports[new Random().Next(0, airports.Count)];
        }

        private async Task UpdatePlane()
        {
            var currentTime = DateTime.Now;

            Navigation.MovePlane(ref _planeContract, currentTime);

            _planeContract.LastPositionUpdate = currentTime;

            if (HasplaneReachedItsDestination())
            {
                await SelectNewDestinationAirport();
            }
        }

        private bool HasplaneReachedItsDestination()
        {
            return (Math.Abs(_planeContract.DestinationAirport.Latitude - _planeContract.Latitude) <= 1 &&
                Math.Abs(_planeContract.DestinationAirport.Longitude - _planeContract.Longitude) <= 1);
        }
    }
}