# 1. Overview

There are multiple options available for publishing and subscribing messages with AWS IoT Core. The message broker supports the use of the MQTT protocol to publish and subscribe and the HTTPS protocol to publish. Both protocols are supported through IP version 4 and IP version 6. The message broker also supports MQTT over the WebSocket protocol.

Here is a simple table that shows various protocol and port options available for handshake with AWS IoT Core.

|No |Protocol        |Authentication     |Port    |
|---|----------------|-------------------|------- |
| 1 |MQTT            |ClientCertificate  |8883,443|
| 2 |HTTP            |ClientCertificate  |8443    |
| 3 |HTTP            |SigV4              |443     |
| 4 |MQTT+WebSocket  |SigV4              |443     |

More details are available here https://docs.aws.amazon.com/iot/latest/developerguide/protocols.html

In this post, we'll cover the option #2 of leveraging ClientCertificate and HTTP protocol for ingesting message into AWS IoT Core. Very specifically, we'll leverage Microsoft .NET and .NET Core for achieving the same. Creating this sample implementation will help IoT applications running in 'App Plane' to handshake with AWS IoT Core. We mean, the applications such as .NET Console app, ASP.NET MVC app, .NET Core console app, ASP.NET Core mvc app, and Xamarain cross platform mobile applications. 

## 2 Create an AWS IoT Thing

You can run the automated provisioning script to create an AWS IoT thing, or choose to walk through the provisioning actions manually in the console.

## Running the provisioning script

Navigate to the 'dotnet' folder and execute the provision_thing.ps1 PowerShell script.  This script handles the setup for the .NET Framework examples in this repository including:

- Downloading the Amazon Root CA certificate.
- Generating a new certificate in AWS IoT.
- Converting the private key to .PFX format.
- Registering an AWS IoT thing with the created certificate.
- Configuring the sample code to use your account's AWS IoT custom endpoint URL.

You can skip to section 3 if you chose to execute the script.

## Manually Creating an AWS IoT Thing

Alternatively, you can manually create an IoT thing using the AWS IoT console.  To start, let's navigate to the console and create an IoT thing called 'dotnetdevice'.

![](/images/pic1.JPG)

Let's associate the following policy with the thing.

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

The above IAM policy grants Publish, Subscribe, Connect, and Receive action on all the AWS IoT Core resource (topics). In the real world implemenation, this policy needs to be restricted to the required topics and actions.

During the Thing creation process you should get the following four security artifacts. Start by creating a 'certificates' folder at 'dotnet\certificates'.

- **Device certificate** - This file usually ends with ".pem.crt". When you download this it will save as .txt file extension in windows. Save it in your certificates directory as 'certificates\certificate.cert.pem' and make sure that it is of file type '.pem', not 'txt' or '.crt'

- **Device public key** - This file usually ends with ".pem" and is of file type ".key".  Save this file as 'certificates\certificate.public.key'. 

- **Device private key** -  This file usually ends with ".pem" and is of file type ".key".  Save this file as 'certificates\certificate.private.key'. Make sure that this file is referred with suffix ".key" in the code while making MQTT connection to AWS IoT.

- **Root certificate** - Download from https://www.amazontrust.com/repository/AmazonRootCA1.pem.  Save this file to 'certificates\AmazonRootCA1.crt'


### Converting device certificate from .pem to .pfx

In order to establish an MQTT connection with the AWS IoT platform, the root CA certificate, private key of the thing and the certificate of the thing are needed. The .NET Cryptographic APIs can understand root CA (.crt) and device private key (.key) out-of-the-box. It expects the device certificate to be in the .pfx format, not in the .pem format. Hence we need to convert the device certificate from .pem to .pfx.

We'll leverage the openssl for converting .pem to .pfx. Navigate to the folder where all the security artifacts are present and launch bash for Windows 10.

The syntax for converting .pem to .pfx is below :-

openssl pkcs12 -export -in **iotdevicecertificateinpemformat** -inkey **iotdevivceprivatekey** -out **devicecertificateinpfxformat** -certfile **rootcertificatefile**

If you replace with actual file names the syntax will look like below.

openssl pkcs12 -export -in certificates\certificate.cert.pem -inkey certificates\certificate.private.key -out certificates\certificate.cert.pfx -certfile certificates\AmazonRootCA1.crt

![](/images/pic3.JPG)

# 3. AWS IoT Device Publisher using HTTPS & X509 certificates with .NET Framework

## 3a. Development environment
- Windows 10 with latest updates
- Visual Studio 2017 with latest updates
- Windows Subsystem for Linux 

## 3b. Visual Studio Solution & Project

Open the solution file located at 'dotnet\Awsiotdevicepubhttp\Awsiotdevicepubhttp.sln' and navigate to the Program.cs class.

This sample application demonstrates the use of client certificates with an HTTP request to communicate with AWS IoT Core.  It creates a random object and publishes that object to the AWS IoT Core custom endpoint using the client certificate created in step 2.  It repeats this process every 5 seconds to emulate an actual IoT device sending device updates to AWS IoT Core.

The InvokeHttpPost method takes an object of any type, serializes it to JSON, and then publishes that object as the payload of the HTTP request.

## 3c. Compile, Run and Verify messages sent to AWS IoT Core
Compile and run the above code. You should see the following output in the console.

![](/images/pic4.jpg)

You should also see the JSON messages succesfully ingested in AWS IoT Core as well.

![](/images/pic5.JPG)

# 4. AWS IoT Device Publisher using HTTPS & X509 certificates with .NET Core

## 4a. Create an AWS IoT Thing 

Navigate to the 'dotnetcore' folder and execute the provision_thing.sh shell script.  This script handles the setup for the .NET Core examples, following the same steps as the PowerShell script in the .NET Framework examples.

Alternatively, you can copy the certificates created in the .NET Framework example to a 'dotnetcore\certificates' folder or follow the same steps to create a new Thing and certificate.

## 4b. Development environment
- Mac OS with latest updates (or) Supported Linux distros for .NET Core with latest updates
- .NET Core 2.1 or higher
- Visual Studio Code

## 4c. Visual Studio Project

Open the project file located at 'dotnetcore\dotnetcore.csproj' and navigate to the Program.cs class.

This sample application demonstrates the use of client certificates with an HTTP request to communicate with AWS IoT Core.  It creates a random object and publishes that object to the AWS IoT Core custom endpoint using the client certificate created in step 2.  It repeats this process every 5 seconds to emulate an actual IoT device sending device updates to AWS IoT Core.

The InvokeHttpPost method takes an object of any type, serializes it to JSON, and then publishes that object as the payload of the HTTP request.

## 4d. Compile and run the code

Invoke the following commands in the terminal to compile and run the code.

```
dotnet clean
dotnet restore
dotnet build
dotnet run
```

## 4e. Verify the messages

You should see messages getting successfully ingested into AWS IoT Core from the console output.

![](/images/pic6.jpg)

Subscribe to the topic 'iotbutton/virtualButton' in  AWS IoT Core.

![](/images/pic7.jpg)

You should see JSON Thermostat messages getting ingested sucessfully into the topic.

![](/images/pic5.jpg)

The complete code sample for this .NET Core implemenation is available in the folder named 'dotnetcore' in this Github repository.

# 5. Conclusion

In this post, we created a .NET sample running on Windows, posting messages to AWS IoT Core. This example leveraged HTTPS protocol and Device Cerrtificate (X509Certificate) for interacting with AWS IoT Core. We also have created another sample in .NET Core publishing messages to AWS IoT Core, achieving the same purpose. This completes the post of creating an AWS IoT Device publisher using HTTPs and Device Certificate, in .NET and .NET Core.