# MedSync

MedSync is a role-based healthcare management web application built to digitize hospital operations — appointment booking, digital prescriptions, medicine inventory, and doctor-patient communication — for Admins, Doctors, and Patients.

## Features

### 👤 Patient Portal
- Smart appointment booking with doctor-specific schedules and time slots
- Automatic patient account creation for seamless onboarding
- Real-time appointment notifications (approved/rejected status)
- Browser push notifications for upcoming appointments
- Complete appointment history with status tracking
- Digital prescription viewer with print & download support
- Live doctor-patient chat with real-time updates

### 🩺 Doctor Portal
- Personalized dashboard with appointment insights and patient queue
- One-click appointment approval, rejection, and completion management
- Access to complete patient medical history
- Digital prescription generation including diagnosis, medicines, tests, and notes
- Smart medicine search using both brand names and salt names
- FIFO-based medicine stock deduction during prescriptions
- Real-time patient chat with unread message indicators

### ⚙️ Admin Panel
- Doctor management with configurable schedules and availability
- Medicine inventory system with batch tracking
- 30-day expiry alert dashboard with urgency indicators
- FIFO batch handling to reduce medicine wastage
- Medicine substitute suggestions using salt-name matching
- Smart medicine search functionality
- Centralized patient and appointment management

## Technical Highlights

- Role-based authentication (Admin / Doctor / Patient) using ASP.NET Identity
- FIFO inventory management for optimized medicine handling
- Real-time chat implementation using AJAX polling (without SignalR)
- Automatic patient account generation with secure credential handling
- Anti-forgery token protection across forms
- Fully responsive, role-specific professional UI/UX

## Tech Stack

- **Framework:** ASP.NET Core MVC
- **ORM:** Entity Framework Core
- **Database:** Microsoft SQL Server
- **Authentication:** ASP.NET Identity
- **Frontend:** Razor Views, JavaScript, AJAX, Bootstrap

## Project Structure

```
MedSync/
├── Controllers/       # MVC controllers (Admin, Doctor, Patient, Auth)
├── Models/            # Entity Framework models
├── Views/             # Razor views, organized by role
├── Data/              # DbContext and migrations
├── Services/          # Business logic (inventory, prescriptions, chat)
├── wwwroot/           # Static assets (CSS, JS, images)
└── MedSync.sln
```

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (matching the project's target version)
- Microsoft SQL Server (local instance or SQL Server Express)
- Visual Studio 2022 or later (recommended) or the `dotnet` CLI

### Setup

1. Clone the repository
   ```bash
   git clone https://github.com/<your-username>/medsync.git
   cd medsync
   ```

2. Update the connection string in `appsettings.json` to point to your SQL Server instance.

3. Apply database migrations
   ```bash
   dotnet ef database update
   ```

4. Run the application
   ```bash
   dotnet run
   ```

5. Open the app at `https://localhost:<port>` and register/log in based on role.

## Roles & Access

| Role | Access |
|---|---|
| **Admin** | Manage doctors, medicine inventory, appointments, and patients |
| **Doctor** | Manage appointments, patient history, and prescriptions |
| **Patient** | Book appointments, view prescriptions, chat with doctors |

## License

This project is available for educational and academic use.
