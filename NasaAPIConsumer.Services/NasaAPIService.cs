﻿using Microsoft.Extensions.DependencyInjection;
using NasaAPIConsumer.Domain;
using NasaAPIConsumer.Utils;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace NasaAPIConsumer.Services
{
    public class NasaAPIService : INasaAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly NasaAPIConfigurations _configs;
        public NasaAPIService(HttpClient httpClient)
        {
            _configuration = ServiceTool.ServiceProvider.GetService<IConfiguration>();
            _configs = _configuration.GetSection("NasaAPIConfigurations")
                                     .Get<NasaAPIConfigurations>()
                                     ?? throw new Exception("This property can't be null");
            _httpClient = httpClient;
        }
        public async Task<List<Asteroid>> Get(int days)
        {
            DateTime currentDate = DateTime.Now;
            DateTime date = DateTime.Now;
            DateTime endDate = currentDate.AddDays(days);
            List<Asteroid> list = new();

            string url = $"{_configs.BaseUrl}?start_date={currentDate.ToString("yyyy-MM-dd")}&end_date={endDate.ToString("yyyy-MM-dd")}&API_KEY={_configs.ApiKey}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if(response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var jsonParse = JsonDocument.Parse(json);
                var jsonFilter = jsonParse.RootElement.GetProperty("near_earth_objects").ToString();
                var asteroids = JsonSerializer.Deserialize<JsonElement>(jsonFilter);                
                
                while(date < endDate)
                {
                    var prop = asteroids.GetProperty(date.ToString("yyyy-MM-dd"));
                    if(prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
                    {
                        for (int i = 0; i < prop.GetArrayLength(); i++)
                        {
                            var kilometers = prop[i].GetProperty("estimated_diameter").GetProperty("kilometers");
                            var close_approach_data = prop[i].GetProperty("close_approach_data");

                            var name = prop[i].GetProperty("name").ToString();
                            var estimated_diameter_min = kilometers.GetProperty("estimated_diameter_min").ToString();
                            var estimated_diameter_max = kilometers.GetProperty("estimated_diameter_max").ToString();
                            var kilometers_per_hour = close_approach_data[0].GetProperty("relative_velocity").GetProperty("kilometers_per_hour").ToString();
                            var close_approach_date = close_approach_data[0].GetProperty("close_approach_date").ToString();
                            var orbiting_body = close_approach_data[0].GetProperty("orbiting_body").ToString();
                            var diameterAvg = (float.Parse(estimated_diameter_min) + float.Parse(estimated_diameter_max)) / 2;

                            list.Add(new()
                            {
                                Name = name,
                                Diameter = diameterAvg,
                                Date = close_approach_date,
                                Planet = orbiting_body,
                                Velocity = kilometers_per_hour
                            });

                            //if (bool.Parse(prop[i].GetProperty("is_potentially_hazardous_asteroid").ToString().ToLower()))
                            //{

                            //}
                        }
                    }                    

                    date = date.AddDays(1);
                }

            }

            return list;
        }
    }
}
