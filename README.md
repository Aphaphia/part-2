\# RaceDay API (PROG6212 – Part 2)



RESTful ASP.NET Core Web API for the RaceDay event management system. It powers organisers and participants with secure authentication, event management, category management, enrolments, and race results.



\---



\## Tech Stack



\- ASP.NET Core 8 Web API

\- Entity Framework Core (Code-First)

\- SQL Server / LocalDB

\- JWT Authentication + Role-Based Authorization

\- Swagger / Swashbuckle (OpenAPI 3.0)

\- xUnit + Moq for unit testing

\- GitHub Actions for CI/CD



\---



\## Roles



| Role | Capabilities |

|------|--------------|

| \*\*Organiser\*\* | Create, update, and delete events. Manage categories. View enrolments for own events. Capture and correct race results. |

| \*\*Participant\*\* | Browse events. Enrol into events by selecting a category. Cancel own enrolment. View personal results. |



\---



\## Setup Instructions



\### 1. Clone the repository



```bash

git clone https://github.com/Aphaphia/part-2.git

cd part-2










