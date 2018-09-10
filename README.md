# TickMe
A sample ASP.NET Core application to sell tickets that serves as a base for a Containers Workshop.
This is the third and final part of the tutorial.
You can find the the first part [here](https://github.com/etpedror/TickMe) and second part [here](https://github.com/etpedror/TickMeMicroservices)
## OVERVIEW
TickMe is a ticket selling website that is growing in popularity. While the current monolithic application still works, to cope with higher loads in an efficient manner, it was decided to go for a microservices architecture and to use containers. 

#### CONTAINERIZING
Visual Studio 2017 makes it really easy to containerise an application. We will start by following the VS2017 route and then take a look at what it created.

The first step is to add an orchestrator to make sure that all the containers start. To do that, right click one of the projects (TickMe, TickMePayments or TickMeTickets), select __Add__ > __Container Orchestrator Support__. Choose __Docker Compose__, select __Linux__ and finish the process. Visual Studio automatically creates a _docker-compose_ file for the solution and a _Dockerfile_ for the project. Repeat the process for the TickMe, TickMePayments and TickMeTickets projects.
The next step is to make sure that the TickMe container, the one containing the webapp is able to communicate with the services containers. 
This involves two things: 
- First, we need to change the URLs in the _appsettings.json_ file to the addresses of the services containers. Fortunately, Docker makes it easy for us to find out the addresses of a container: it’s the name of the container. So, we should have our _appsettings.json_ file with the following:
``` javascript
  "TicketApiUrl": "https://tickmetickets:443/api/tickets",
  "PaymentApiUrl": "https://tickmepayments:443/api/payment",
```
- Next we need to make sure our ssl certificate for each of the services is accepted by our webapp. For the sake of demonstration, we will just accept any certificate but __please keep in mind that this is NOT how you would do it production__. Our _EventController_ class in the TickMe project should have it’s methods _IssueTicket_ and _MakePayment_ updated to look like the following: 
```c#
public async Task<Ticket> IssueTicket(TicketBuyModel buyModel)
{
  using (var httpClientHandler = new HttpClientHandler())
  {
    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
    using (var client = new HttpClient(httpClientHandler))
    {
      var request = new HttpRequestMessage
      {
        RequestUri = new Uri(Configuration["TicketApiUrl"]),
        Method = HttpMethod.Post
      };
      request.Content = new StringContent(JsonConvert.SerializeObject(buyModel));
      request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

      var result = client.SendAsync(request).Result;
      result.EnsureSuccessStatusCode();
      return JsonConvert.DeserializeObject<Ticket>(await result.Content.ReadAsStringAsync());
    }
  }
}

public async Task<PaymentData> MakePayment(PaymentData paymentData)
{
  using (var httpClientHandler = new HttpClientHandler())
  {
    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
    using (var client = new HttpClient(httpClientHandler))
    {
      var request = new HttpRequestMessage
      {
        RequestUri = new Uri(Configuration["PaymentApiUrl"]),
        Method = HttpMethod.Post
      };
      request.Content = new StringContent(JsonConvert.SerializeObject(paymentData));
      request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

      var result = client.SendAsync(request).Result;
      result.EnsureSuccessStatusCode();
      return JsonConvert.DeserializeObject<PaymentData>(await result.Content.ReadAsStringAsync());
    }
  }
}
```
The application is now ready to run inside containers. Press the Start button and try to access your application on http://localhost:50080

What magic just happened? Well, Visual Studio generated a Dockerfile for each project, that, in a nutshell, has all the instructions to build a container. For the TickMe web application, it looks like this
```dockerfile
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 50080
EXPOSE 44380

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY TickMe/TickMe.csproj TickMe/
COPY TickMeHelpers/TickMeHelpers.csproj TickMeHelpers/
RUN dotnet restore TickMe/TickMe.csproj
COPY . .
WORKDIR /src/TickMe
RUN dotnet build TickMe.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish TickMe.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TickMe.dll"]
```
As you can see, we start with a basic .NET Core 2.1 image, we expose ports 50080 and 44380 to the outside world, then we copy our project into the container, we build it, publish it and finally, we tell docker to run _`dotnet TickMe.dll`_ when the container starts.
The other webapps have similar Dockerfiles
