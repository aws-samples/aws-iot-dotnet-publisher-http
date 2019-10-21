
using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Newtonsoft.Json;
using dotnetcore.utils;
using System.Collections.Generic;

namespace dotnetcore
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientCert = new X509Certificate2(Path.Join(AppContext.BaseDirectory, "certificate.cert.pfx"), "MyPassword1");

            string requestUri = @"https://youriotendpoint.iot.us-east-1.amazonaws.com/topics/iotbutton/virtualButton?qos=1";

            while (true)
            {
                Thermostat thermostat = new Thermostat();
                Random random = new Random();
                thermostat.ThermostatID = random.Next(10000);
                thermostat.SetPoint = random.Next(32, 100);
                thermostat.CurrentTemperature = random.Next(32, 100);

                InvokeHttpPost(requestUri, clientCert, thermostat);

                Thread.Sleep(5000);
            }
        }

        private static void InvokeHttpPost<T>(string requestUri, X509Certificate2 clientCert, T postData)
        {
            string serializedPostData = JsonConvert.SerializeObject(postData);
            Logger.LogInfo($"Publishing {serializedPostData}");
            byte[] byteArray = Encoding.UTF8.GetBytes(serializedPostData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.Method = "POST";
            request.ContentLength = byteArray.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            request.KeepAlive = true;
            request.ClientCertificates.Add(clientCert);

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            HttpHelpers.GetResponse(request);
        }
    }
}