To create the AppWebApi, AppMvc and AppRazor
All applications are using the same software stack

1. Create the database. With Terminal in folder _scripts 
   E.g. database name: sql-music, database type: sqlserver, server: docker, default user: dbo, application: ../AppWebApi
   use application as ../AppWebApi, ../AppRazor or ../AppMvc depending on application you want to build database for.
   
   macOs
   ./database-rebuild-all.sh sql-music sqlserver docker dbo ../AppRazor
   ./database-rebuild-all.sh sql-music mysql docker dbo ../AppRazor
   ./database-rebuild-all.sh sql-music postgresql docker dbo ../AppRazor
   
   Windows
   ./database-rebuild-all.ps1 sql-music sqlserver docker dbo ../AppRazor
   ./database-rebuild-all.ps1 sql-music mysql docker dbo ../AppRazor
   ./database-rebuild-all.ps1 sql-music postgresql docker dbo ../AppRazor

   Ensure no errors from build, migration or database update

2. With SQL Server (mssql) extension you can now connect to the database
   Use connection string from user secrets:
   connection string corresponding to Tag
   "sql-friends.<db_type>.docker.root"

3. Use SQL Server (mssql) extension to execute SQL script DbContext/SqlScripts/<db_type>/initDatabase.sql

4. Run AppWebApi with or without debugger

   Without debugger:   
   Open a Terminal in folder AppWebApi run: 
   dotnet run -lp https 
   open url: https://localhost:7066/swagger

   Verify your can execute endpoint Admin/Environment and Guest/Info

5. Use endpoint Admin/Seed to seed the database, Admin/RemoveSeed to remove the seed
   Verify database seed with endpoint Guest/Info
   Play around with all endpoints


Running AppRazor using WebApi or Database as source:
----------------------------------------------------

6. Ensure that in appsettings.json 
   "DefaultDataUser": "dbo"

7. Run AppRazor or AppMvc with debugger

   Use menu alternative to seed and play around with data from the database

8. Use menu Select datasource to WebApi and see that you can access same data but through the WebApi
   
NOTE: From AppRazor and AppMvc perspective, the ONLY change is the DataAccess services injected to the DI 
      This is one of the strength of a well made software stack with loosly couple objects and alyers