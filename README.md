# EF Core 6 Audit Logging

A small project used to learn: 
<li>Audit logging
<li>Audit logging value objects (owned types) 
<li>Using JSON value converters with EF Core
<li>Persisting the new DateOnly & TimeOnly types using EF  
<li>EF Core 6 Migrations
  
NOTE: This project uses SQLLite for the database but could easily be changed to a SQL Server by changing the connection string.  
# To Dos
Need to update audit logging to handle a property that is a list of owned types currently it will only handle as a property that is a singular owned type
  
# Kudos  
While researching how to get EF Core 6 to work with the new .NET 6 DateOnly & TimeOnly types. I 
came across this great .NET Utility [TinyHelpers](https://github.com/marcominerva/TinyHelpers/tree/master/src/TinyHelpers.EntityFrameworkCore).
Thank you [Marco Minerva](https://github.com/marcominerva) for sharing this great library!
  
  



  
  
  
