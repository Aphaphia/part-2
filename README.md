\# RaceDay API (PROG6212 – Part 2)



A RESTful ASP.NET Core Web API for the RaceDay event management system. It supports organisers and participants with authentication, event management, category management, enrolments, and race results.



\## 🛠 Tech Stack



\- ASP.NET Core 8 Web API

\- Entity Framework Core (Code-First, SQL Server)

\- JWT Authentication + Role-Based Authorization

\- Swagger / Swashbuckle for API documentation

\- xUnit + Moq for unit tests

\- GitHub Actions for CI/CD



\## 🔐 Roles



| Role | Capabilities |

|------|--------------|

| \*\*Organiser\*\* | Create / Update / Delete events, manage categories, view enrolments for own events, capture and correct results |

| \*\*Participant\*\* | Browse events, enrol, cancel enrolment, view own results |



\## ⚙️ Setup Instructions



\### 1. Clone the repository



```bash

git clone https://github.com/Aphaphia/part-2.git

cd part-2

