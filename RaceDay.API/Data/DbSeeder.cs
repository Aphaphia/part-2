using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Data;

public static class DbSeeder
{
    public static void Seed(RaceDayDbContext db, IPasswordHasher hasher)
    {
        if (db.Users.Any()) return;

        var org1 = new User { FirstName = "Thabo", LastName = "Nkosi", Email = "thabo.nkosi@raceday.co.za", Password = hasher.Hash("HashedPassword123"), Role = "Organiser" };
        var org2 = new User { FirstName = "Lindiwe", LastName = "Dube", Email = "lindiwe.dube@raceday.co.za", Password = hasher.Hash("HashedPassword123"), Role = "Organiser" };
        var p1 = new User { FirstName = "Sipho", LastName = "Mokoena", Email = "sipho.mokoena@gmail.com", Password = hasher.Hash("HashedPassword123"), Role = "Participant" };
        var p2 = new User { FirstName = "Anele", LastName = "Botha", Email = "anele.botha@gmail.com", Password = hasher.Hash("HashedPassword123"), Role = "Participant" };

        db.Users.AddRange(org1, org2, p1, p2);
        db.SaveChanges();

        db.UserProfiles.AddRange(
            new UserProfile { UserID = org1.UserID, DateOfBirth = new DateTime(1985, 3, 14), Phone = "0821234567", City = "Johannesburg" },
            new UserProfile { UserID = org2.UserID, DateOfBirth = new DateTime(1990, 7, 22), Phone = "0839876543", City = "Cape Town" },
            new UserProfile { UserID = p1.UserID, DateOfBirth = new DateTime(1998, 11, 5), Phone = "0731122334", City = "Soweto" },
            new UserProfile { UserID = p2.UserID, DateOfBirth = new DateTime(2001, 2, 18), Phone = "0844455667", City = "Pretoria" });

        var e1 = new Event { EventName = "Soweto 10km Fun Run", Description = "A community fun run through the streets of Soweto.", EventDate = new DateTime(2026, 10, 10), Location = "Soweto, Johannesburg", Distance = 10.00m, EventType = "Run", OrganiserID = org1.UserID };
        var e2 = new Event { EventName = "Cape Town Cycle Tour", Description = "One of the world's largest timed cycling events.", EventDate = new DateTime(2026, 11, 8), Location = "Cape Town", Distance = 109.00m, EventType = "Cycle", OrganiserID = org2.UserID };
        var e3 = new Event { EventName = "Joburg City Walk", Description = "A relaxed charity walk through Johannesburg CBD.", EventDate = new DateTime(2026, 9, 20), Location = "Johannesburg CBD", Distance = 5.00m, EventType = "Walk", OrganiserID = org1.UserID };

        db.Events.AddRange(e1, e2, e3);
        db.SaveChanges();

        var c1 = new Category { EventID = e1.EventID, CategoryName = "Under 20", Description = "Runners under the age of 20." };
        var c2 = new Category { EventID = e1.EventID, CategoryName = "Senior", Description = "Runners aged 20 and above." };
        var c3 = new Category { EventID = e2.EventID, CategoryName = "Individual", Description = "Individual cyclist entry." };
        var c4 = new Category { EventID = e2.EventID, CategoryName = "Team", Description = "Team entry of up to 4 riders." };
        var c5 = new Category { EventID = e3.EventID, CategoryName = "Open", Description = "Open category for all ages." };

        db.Categories.AddRange(c1, c2, c3, c4, c5);
        db.SaveChanges();

        var en1 = new Enrollment { UserID = p1.UserID, EventID = e1.EventID, CategoryID = c1.CategoryID, Status = "Confirmed" };
        var en2 = new Enrollment { UserID = p2.UserID, EventID = e2.EventID, CategoryID = c3.CategoryID, Status = "Confirmed" };
        var en3 = new Enrollment { UserID = p1.UserID, EventID = e3.EventID, CategoryID = c5.CategoryID, Status = "Confirmed" };

        db.Enrollments.AddRange(en1, en2, en3);
        db.SaveChanges();

        db.Results.Add(new Result { EnrollmentID = en1.EnrollmentID, FinishTime = new TimeSpan(0, 42, 15), Position = 3 });
        db.SaveChanges();
    }
}