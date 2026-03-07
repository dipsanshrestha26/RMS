# RMS Cinema Management System

\# RMS Cinema Management System



This project is a cinema management system developed using ASP.NET Core MVC and Oracle Database.



It was built to manage the main operations of a cinema in a structured way, including movie details, theatre and hall setup, show scheduling, ticket booking, payment processing, and report generation.



\## Technologies Used



\- ASP.NET Core MVC

\- C#

\- Oracle Database

\- Razor Views

\- Bootstrap

\- Git and GitHub



\## Main Modules



\### Customer Management

Stores customer records and allows add, update, delete, and search operations.



\### Movie Management

Stores movie details such as title, genre, language, duration, and release date.



\### Theatre Management

Stores theatre information including theatre name and city.



\### Hall Management

Stores hall details for each theatre, including hall name and seating capacity.



\### Show Management

Used to schedule movies in specific halls with a date, time, and base ticket price.



\### Ticket Management

Allows ticket booking for customers by selecting a show and an available seat.  

Seat availability is checked based on hall capacity and already booked seats.



\### Payment Management

Stores payment details linked to tickets.  

The payment amount is validated against the ticket price before saving the record.



\### Reports Module

Generates useful reports such as:

\- customer ticket history

\- hall show schedule

\- top occupancy report

\- revenue by movie

\- ticket status summary



\## Database Structure



The project uses a normalized relational database structure.  

The main tables are:



\- CUSTOMER\_3NF

\- MOVIE\_3NF

\- THEATRE\_3NF

\- HALL\_3NF

\- SHOW\_3NF

\- TICKET\_3NF

\- PAYMENT\_3NF



These tables are connected using primary keys and foreign keys.



\## Key Features



\- CRUD operations for all major entities

\- Duplicate record validation

\- Show scheduling validation

\- Seat availability checking

\- Payment amount validation

\- Ticket and payment status handling

\- Reporting dashboard



\## How to Run



1\. Open the project in Visual Studio.

2\. Make sure Oracle database connection is configured correctly.

3\. Run the database tables and required data.

4\. Start the application.

5\. Use the navigation menu to access each module.



\## Author



This project was developed as coursework for a web-based database management system.

