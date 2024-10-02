# CurrencyExchangeAPI

## What is this repository for? 

- This is a web api project that exposes endpoints to fetch current and historical currency exchange rates, and converts amount between currencies 
- It internally uses frankfurter open source apis to get data https://www.frankfurter.app/docs/

## How do I get set up / Run the project? 

### Using Visual Studio
- Open CurrencyExchangeAPI.sln present in root directory using visual studio 
- This should load up both main project and unit test project
- You can run/debug the main project using https icon (Play button) at the top center. This opens the swagger page in development mode which can be used to execute/debug the endpoints
	- Optional : In case you are using any other port to run your app, please update "API:BaseURL" appsettings.Development.json with appropriate port
- To run units tests, Go to Test > Run All Tests. Alternatively open Test > Test Explorer to run or debug tests.

### Using VSCode
- Open CurrencyExchangeAPI folder using vscode
- In the terminal
	- From root folder Change path using command :  cd CurrencyExchangeAPI
	- Run project using command :  dotnet run CurrencyExchangeAPI.csproj --urls=https://localhost:7037/  (Use another port if you get error)
	- Optional : In case you are using any other port to run your app, please update "API:BaseURL" appsettings.Development.json with appropriate port
- Go to any browser and open "BaseUrl": "https://localhost:7037/swagger/index.html". This opens the swagger page in development mode which can be used to execute/debug the endpoints
- To run units tests
	- From root folder Change path using command :  cd CurrencyExchangeAPI.UnitTests
	- Run Tests uing command :  dotnet test


## Assumptions / Business Rules
 - Some of the business rules/best practices are inferred based on https://www.frankfurter.app/docs/
	- Added extra date validations based on documentation. for eg: oldest date supported = January 4, 1999
	- For Endpoint 2 (Conversion): Using Rates API for getting exchange rates and then doing conversions on our end instead of directly using convert api from frankfurter. This is what docs also recommend and this way we can use caching efficiently.
	- For Endpoint 3 (HistoricalRates): Appropriately handling requests based on the fact that frankfurter samples requests if date range is above 90 days.

- For handling trainsient API failure, added retry policy which uses exponential backoffs and jitter to gracefully retry apis.
	- Max No of retries can be modified from appsettings.json using "API:MaxRetryCount" property

- For handling large no of requests, project uses in-memory cache setup, that caches responses for all the apis.
	- Caching Perid = 30 min sliding and 1hour of absolute expiry. This should fine as the rates are refreshed 16:00 CET every working day as per docs https://www.frankfurter.app/docs/ 

- Project uses Serilog setup to logs info/errors. This currently only logs to standard output i.e console

## Future Improvements/Features
 - Given more time and context would like to add these features
	- Authentication
		- This is must have feature, will probably use jwt based authentication
		- The authentication logic can be part of this project or can be decentralized and re-used by adding it to api-gateway
    - Authorization
		- This can be implemented if needed. Can inspect jwt token claims for the authenticated entity and take authorization decisions
    - Update Cors Policies
		- Will update cors policies based on client dns for our apis
	- Distributed Caching 
		- Current caching setup is basic and uses in-memory caching. Other solutions using distributed caches can be explored in future if needed.
		- Also caching ttl can be increased and mode more dynamic.
	- Enhance overall Logging and Monitoring
		- Can enhance logging to log to more sinks.