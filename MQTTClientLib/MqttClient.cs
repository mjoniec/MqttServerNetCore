﻿using System.Collections.Generic;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MQTTClientLib
{
    public class MqttClient
    {
        private IMqttClient _client = new MqttFactory().CreateMqttClient();
        private List<string> _messages = new List<string>();

        public async void Connect(string ip, int port)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(ip, port)
                .Build();

            await _client.ConnectAsync(options);
        }

        public async void Subscribe(string topic)
        {
            _client.ApplicationMessageReceived += (s, e) =>
            {
                _messages.Add(Encoding.UTF8.GetString(e.ApplicationMessage.Payload) + " | " + e.ApplicationMessage.Topic);
            };

            await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topic).Build());
        }

        public async void Publish(string topic, string message)
        {
            var mqttApplicationMessageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _client.PublishAsync(mqttApplicationMessageBuilder);
        }
    }
}
