# 1. Overview

There are multiple options available for publishing and subscribing messages with AWS IOT Core. The message broker supports the use of the MQTT protocol to publish and subscribe and the HTTPS protocol to publish. Both protocols are supported through IP version 4 and IP version 6. The message broker also supports MQTT over the WebSocket protocol.

Here is a simple table that shows various protocol and port options available for handshake with AWS IOT Core.

|No |Protocol        |Authentication     |Port    |
|---|----------------|-------------------|------- |
| 1 |MQTT            |ClientCertificate  |8883,443|
| 2 |HTTP            |ClientCertificate  |8443    |
| 3 |HTTP            |SigV4              |443     |
| 4 |MQTT+WebSocket  |SigV4              |443     |

More details are available here https://docs.aws.amazon.com/iot/latest/developerguide/protocols.html

In this post, we'll cover the option #2 of leveraging ClientCertificate and HTTP protocol for ingesting message into AWS IOT core. Very specifically, we'll leverage Microsoft .NET and .NET core for achieving the same. Creating this sample implementation will help IOT applications running in 'App Plane' to handshake with AWS IOT Core. We mean, the applications such as .NET Console app, ASP.NET MVC app, .NET Core console app, ASP.NET core mvc app and Xamarain cross platform mobile applications. 


# 2. Create an AWS IOT Device

![](/images/pic1.JPG)



Let's create an AWS IOT device with any arbitrary name and associate the following IAM policy to the X509 certificate of the device.


``` json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Action": [
        "iot:Publish",
        "iot:Subscribe",
        "iot:Connect",
        "iot:Receive"
      ],
      "Effect": "Allow",
      "Resource": [
        "*"
      ]
    }
  ]
}
``` 

The above IAM policy grants Publish, Subscribe, Connect and Receive action on all the AWS IOT core resource (topics). In the real world implemenation, this policy needs to be restricted to the required topics and actions.

During the Thing creation process you should get the following four security artifacts.

a)**Device certificate** - This file usually ends with ".pem.crt". When you download this it will save as .txt file extension in windows. Save it as 'dotnet-devicecertificate.pem' for convenience and make sure that it is of file type '.pem', not 'txt' or '.crt'

b)**Device public key** - This file usually ends with ".pem" and is of file type ".key".  Rename this file as 'dotnet-public.pem' for convenience. 

c)**Device private key** -  This file usually ends with ".pem" and is of file type ".key".  Rename this file as 'dotnet-private.pem' for convenience. Make sure that this file is referred with suffix ".key" in the code while making MQTT connection to AWS IOT.

d)**Root certificate** - The default name for this file is VeriSign-Class 3-Public-Primary-Certification-Authority-G5.pem and rename it as "root-CA.crt" for convenience.


# 3. Converting device certificate from .pem to .pfx

In order to establish an MQTT connection with the AWS IOT platform, root CA certificate, private key of the Thing and the Certificate of the Thing are needed. The .NET Cryptographic APIs can understand root CA (.crt) and device private key (.key) out-of-box. It expects the device certificate to be in the .pfx format, not in the .pem format. Hence we need to convert the device certificate from .pem to .pfx.

We'll leverage the openssl for converting .pem to .pfx. Navigate to the folder where all the security artifacts are present and launch bash for Windows 10.

The syntax for converting .pem to .pfx is below :-

openssl pkcs12 -export -in **iotdevicecertificateinpemformat** -inkey **iotdevivceprivatekey** -out **devicecertificateinpfxformat** -certfile **rootcertificatefile**

If you replace with actual file names the syntax will look like below.

openssl pkcs12 -export -in dotnet-devicecertificate.pem -inkey dotnet-private.pem.key -out dotnet_devicecertificate.pfx -certfile root-CA.crt

<p align="center">
<img src="/images/pic3.JPG">
</p>


# 4. AWS IOT Device Publisher using HTTPS & X509 certificates on Windows

## 4a. Development environment
- Windows 10 with latest updates
- Visual Studio 2017 with latest updates
- Windows Subsystem for Linux 

## 4b. Visual Studio Solution & Project

Create a visual studio 2017 console project with name 'Awsiotdevicepubhttp'.

Make sure that the following namespaces are imported in the program.cs file.
``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Newtonsoft.Json;
```

Add a new class called 'Thermostat.cs' to the project. It should look like below.
``` c#
class Thermostat
    {
        public int ThermostatID { get; set; }

        public int SetPoint { get; set; }

        public int CurrentTemperature { get; set; }

       
    }
  ``` 

In the static main method add the following lines of code to load the X509certificate2 of the device and X509Certificate of the root into objects namely 'ClientCert' and 'CaCert'. Also invoke the static method 'InvokeHttpPost' every 5 seconds for ingesting JSON data into AWS IOT topic.

``` c#
            var CaCert = X509Certificate.CreateFromCertFile(@"C:\PythonIotsamples\root-CA.crt");
            var ClientCert = new X509Certificate2(@"C:\PythonIotsamples\WinIOTDevice.pfx", "password1");
            Thermostat thermostat = new Thermostat();
           

            while (true)
            {

               InvokeHttpPost(CaCert, ClientCert,thermostat).Wait();

                Thread.Sleep(5000);
            }
``` 


Add a static method called InvokeHttpPost with the following implementation.


``` c#
	
	
        private static async Task<string> InvokeHttpPost(X509Certificate root, X509Certificate2 device, Thermostat th)
        {




            Random r = new Random();


            string requesturi = @"https://youriotendpoint.iot.us-east-1.amazonaws.com:8443/topics/iotbutton/virtualButton?qos=1";

            th.ThermostatID = r.Next(10000);
            th.SetPoint = r.Next(32, 100);

            th.CurrentTemperature = r.Next(32, 100);

            string postData = JsonConvert.SerializeObject(th);

            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturi);


            request.Method = "POST";
            request.ContentLength = byteArray.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            request.KeepAlive = false;
            request.ClientCertificates.Add(root);
            request.ClientCertificates.Add(device);


            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string responseString;
            StreamReader responseReader = new StreamReader(response.GetResponseStream());

            responseString = responseReader.ReadToEnd();

            if (responseString.Contains("OK"))
            {
                Console.WriteLine(responseString);
            }

            else
            {
                Console.WriteLine("Not successful" + responseString);

            }

            return responseString;
        }  
      
``` 

Instead of hard coding the AWS IOT Http endpoint url, you can also ead it from app.config file. 

## 4c. App.Config file changes

``` XML
Add the following snippet in the web.config file.
 <system.net>
    <settings>
      <httpWebRequest useUnsafeHeaderParsing="true" />
    </settings>
  </system.net>
  ``` 
If the above app.config file entires are not added,  executing the above piece of code will throw an exception of type "The server committed a protocol violation". More information about this app.config flag is available in this documentation https://docs.microsoft.com/en-us/dotnet/api/system.net.configuration.httpwebrequestelement.useunsafeheaderparsing?view=netframework-4.7.2

## 4d. Compile, Run and Verify messages sent to AWS IOT Core
Compile and run the above code. You should see the following output in the console.


<p align="center">
<img src="/images/pic4.JPG">
</p>

You should also see the JSON messages succesfully ingested in AWS IOT core as well.

<p align="center">
<img src="/images/pic7.JPG">
</p>


# 5. AWS IOT Device Publisher using HTTPS & X509 certificates on Mac OS or Linux

## 5a. Development environment
- Mac OS with latest updates (or) Supported Linux distros for .NET core with latest updates
- .NET Core 2.1 or higher
- Visual Studio Code

## 5b. Create Console application project in Dotnetcore

Invoke the following commands in the terminal to create a sample console application project in .NET core.

``` shell
$dotnet new console
$dotnet restore
$dotnet run
``` 


Make sure that the following namespaces are imported in the program.cs file.
``` c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Newtonsoft.Json;
```

Add a reference to the package 'Newtonsoft.Json' package by issuing the below command.

``` shell
$dotnet add package 'Netwonsoft.Json'
``` 

Add a new class called 'Thermostat.cs' to the project. It should look like below.
``` c#
class Thermostat
    {
        public int ThermostatID { get; set; }

        public int SetPoint { get; set; }

        public int CurrentTemperature { get; set; }

       
    }
  ``` 

 In the static void main method implement the following.

``` c#

 var CaCert = X509Certificate.CreateFromCertFile(@"root-CA.crt");

            // new changes
            var CaCertNew = new X509Certificate2(CaCert);
           
            var ClientCert = new X509Certificate2(@"yourdevicecert.pfx", "passphrase");
            Thermostat thermostat = new Thermostat();


            while (true)
            {

              
                InvokeHttpPost(CaCertNew, ClientCert, thermostat).Wait();

                Thread.Sleep(5000);
            }
```

Implement the InvokeHttpPost method like the following.

``` c#
private static async Task<string> InvokeHttpPost(X509Certificate2 root, X509Certificate2 device, Thermostat th)
        {
                     

            Random r = new Random();


            string requesturi = @"https://youriotendpoint.iot.us-east-1.amazonaws.com:8443/topics/iotbutton/virtualButton?qos=1";

            th.ThermostatID = r.Next(10000);
            th.SetPoint = r.Next(32, 100);

            th.CurrentTemperature = r.Next(32, 100);

            string postData = JsonConvert.SerializeObject(th);

            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturi);


            request.Method = "POST";
            request.ContentLength = byteArray.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            request.KeepAlive = true;
            request.ClientCertificates.Add(root);
            request.ClientCertificates.Add(device);


            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string responseString;
            StreamReader responseReader = new StreamReader(response.GetResponseStream());

            responseString = responseReader.ReadToEnd();

            if (responseString.Contains("OK"))
            {
                Console.WriteLine(responseString);
            }

            else
            {
                Console.WriteLine("Not successful" + responseString);

            }

            return responseString;
        }

```

## 5c. Code changes specific to .NET core running on Linux or Mac

If you look at the above code-snippet it is not exactly one and the same written for .NET running on windows (section 4b). The crux of the above code is all about programmatically sending HTTP Post request to AWS IOT Core by adding root certificate & device certificate and also setting the required POST parameters. HttpWebRequest object in .NET core expects both the certificates to be of the type 'X509Certificate2', unlike the .NET framework for windows. Hence we are constructing both the certificates of type 'X509Certificate2'.


``` c#

 var CaCert = X509Certificate.CreateFromCertFile(@"root-CA.crt");

            // changes specific to .NET core
            var CaCertNew = new X509Certificate2(CaCert);
           
            var ClientCert = new X509Certificate2(@"yourdevicecert.pfx", "passphrase");
``` 


If you look at the below code sample written for .NET framework running on windows, the root certificate is constructed as object of type 'X509Certificate' and the device certificate is constructed as 'X509Certificate2'.

``` c#
 var CaCert = X509Certificate.CreateFromCertFile(@"C:\PythonIotsamples\root-CA.crt");
var ClientCert = new X509Certificate2(@"Yourdevicecert", "passphrase");

```

If we follow the above approach in .NET core, it will throw a type cast exception. To fix this, we are constructing both the certificates of type 'X509Certificate2' in .NET core. 


## 5d. Compile and run the code

Invoke the following commands in the terminal to compile and run the code.

```
dotnet clean
dotnet restore
dotnet build
dotnet run
```

## 5e. Verify the messages


You should see messages getting successfully ingested into AWS IOT Core from the console output.


<p align="center">
<img src="/images/pic6.JPG">
</p>

Subscribe to the topic 'iotbutton/virtualButton' in  AWS IOT Core.


<p align="center">
<img src="/images/pic7.JPG">
</p>

You should see JSON Thermostat messages getting ingested sucessfully into the topic.


<p align="center">
<img src="/images/pic5.JPG">
</p>

The complete code sample for this .NET core implemenation is available in the folder named 'dotnetcore' in this Github repository.


# 6. Conclusion
In this post, we created a .NET sample running on windows, posting messages to AWS IOT Core. This example leveraged HTTPS protocol and Device Cerrtificate (X509Certificate) for interacting with AWS Iot Core. We also have created another sample in .NET core publishing messages to AWS IOT Core, achieving the same purpose. This completes the post of creating an AWS IOT Device publisher using HTTPs and Device Certificate, in .NET and .NET core. 




