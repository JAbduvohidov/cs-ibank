﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ibank.Extra;
using Microsoft.VisualBasic;

namespace ibank
{
    internal static class Program
    {
        private static bool _isRunning = true;

        private static async Task Main()
        {
            Shred();
            await Database.Init();

            Title("Login");

            var login = Ui.InputText("Phone/Login", 20, 3, 3, 1);
            Shred();

            var password = Ui.InputText("Password", 25, 3, 3, 1, "*");
            Shred();

            var response = await User.AuthenticationAsync(login, password);
            if (response.Error != string.Empty)
            {
                Ui.Notification(response.Error);
                return;
            }

            switch (response.User.Role)
            {
                case User.Roles.User:
                {
                    await UserFunctionsLoopAsync(login);
                    break;
                }
                case User.Roles.Admin:
                {
                    await AdminFunctionsLoopAsync(login);
                    break;
                }
                default:
                {
                    Shred();
                    Ui.Notification("you don't have any permissions :(", Ui.NotificationType.Warning);
                    break;
                }
            }
        }

        private static async Task UserFunctionsLoopAsync(string phoneNumber)
        {
            var items = new List<string>
            {
                "Application history ", // 0
                "Remaining loans     ", // 1
                "Exit                ", // 2
            };

            while (_isRunning)
            {
                Title($"PA - {phoneNumber}");
                Shred();
                Console.SetCursorPosition(2, 2);
                switch (Ui.ComboBox(items))
                {
                    case 0:
                    {
                        Shred();
                        await ApplicationHistoryAsync(phoneNumber);
                        break;
                    }
                    case 1:
                    {
                        Shred();
                        await RemainingLoansAsync(phoneNumber);
                        break;
                    }
                    case 2:
                    {
                        Shred();
                        _isRunning = false;
                        break;
                    }
                }
            }
        }

        private static async Task AdminFunctionsLoopAsync(string phoneNumber)
        {
            var items = new List<string>
            {
                "Register new user  ", // 0
                "Fill user profile  ", // 1
                "Apply for a loan   ", // 2
                "Personal account * ", // 3
                "Exit               ", // 4
            };

            while (_isRunning)
            {
                Title("ADMIN");
                Shred();
                Console.SetCursorPosition(2, 2);
                switch (Ui.ComboBox(items))
                {
                    case 0:
                    {
                        Shred();
                        await RegisterNewUserAsync();
                        Console.ReadKey();
                        break;
                    }
                    case 1:
                    {
                        Shred();
                        await FillUserProfileAsync(phoneNumber);
                        Console.ReadKey();
                        break;
                    }
                    case 2:
                    {
                        Shred();
                        await ApplyForLoanAsync(phoneNumber);
                        Console.ReadKey();
                        break;
                    }
                    case 3:
                    {
                        Shred();
                        await UserFunctionsLoopAsync(phoneNumber);
                        break;
                    }
                    case 4:
                    {
                        Shred();
                        _isRunning = false;
                        break;
                    }
                }
            }
        }

        private static async Task RemainingLoansAsync(string phoneNumber)
        {
            var credits = await Credit.GetRemainingLoanAsync(phoneNumber);

            var creditsList = new List<string>();

            credits.ForEach(credit => creditsList.Add($"{credit} "));
            creditsList.Add("Go back ");

            var selectedIndex = 0;
            while (true)
            {
                Shred();
                Console.SetCursorPosition(2, 2);
                selectedIndex = Ui.ComboBox(creditsList, selectedIndex);
                if (selectedIndex == creditsList.Count - 1) break;


                var repayments = await Repayment.GetCreditRepaymentsAsync(credits[selectedIndex].Id);

                var repaymentsList = new List<string>();
                repayments.ForEach(repayment => repaymentsList.Add($"{repayment} "));
                repaymentsList.Add("Back");

                Shred();
                Console.SetCursorPosition(2, 2);
                Ui.ComboBox(repaymentsList);
            }
        }

        private static async Task ApplicationHistoryAsync(string phoneNumber)
        {
            var credits = await Credit.GetCreditHistoryAsync(phoneNumber);

            var creditsList = new List<string>();

            credits.ForEach(credit => creditsList.Add($"{credit} "));
            creditsList.Add("Go back ");

            Shred();
            Console.SetCursorPosition(2, 2);
            Ui.ComboBox(creditsList);
        }

        private static async Task ApplyForLoanAsync(string phoneNumber)
        {
            Title("Apply for loan");
            const double loanPercent = 0.03;

            var login = Ui.InputText("Search", 20, 3, 3, 1);
            Shred();

            var users = await User.GetUsersWithProfileAsync(phoneNumber, login);

            var usersString = new List<string>();
            users.ForEach(user => usersString.Add($"{user} "));
            usersString.Add("Go back ");

            Shred();

            Console.SetCursorPosition(2, 2);
            var index = Ui.ComboBox(usersString);
            if (index == usersString.Count - 1)
                return;

            var credit = new Credit();

            Shred();

            if (!double.TryParse(Ui.InputText("Total income:", 20, 3, 3, 1), out var totalIncome) && totalIncome < 10)
            {
                Shred();
                Ui.Notification("invalid income value");
                return;
            }

            Shred();

            if (!int.TryParse(Ui.InputText("Number of closed credits:", 20, 3, 3, 1), out var numberOfClosedCredits) &&
                numberOfClosedCredits < 0)
            {
                Shred();
                Ui.Notification("invalid value for number of closed credits");
                return;
            }

            Shred();

            if (!int.TryParse(Ui.InputText("Number of delayed credits:", 20, 3, 3, 1),
                out var numberOfDelayedCredits) && numberOfDelayedCredits < 0)
            {
                Shred();
                Ui.Notification("invalid value for number of delayed credits");
                return;
            }

            Shred();

            if (!int.TryParse(Ui.InputText("Loan terms:", 20, 3, 3, 1), out var loanTerms) && loanTerms < 1)
            {
                Shred();
                Ui.Notification("invalid value for loan terms");
                return;
            }

            Shred();

            if (!int.TryParse(Ui.InputText("Loan amount:", 20, 3, 3, 1), out var loanAmount) && loanAmount < 1)
            {
                Shred();
                Ui.Notification("invalid value for loan amount");
                return;
            }

            Shred();

            var purposes = new List<string>
            {
                "Appliances ",
                "Repair     ",
                "Telephone  ",
                "Other      ",
            };

            Console.SetCursorPosition(4, 1);
            Console.Write("Loan purpose:");
            Console.SetCursorPosition(3, 2);
            credit.Purpose = (Credit.Purposes) (Ui.ComboBox(purposes) + 1);
            credit.TotalIncome = totalIncome;
            credit.History = numberOfClosedCredits;
            credit.Delinquencies = numberOfDelayedCredits;
            credit.Term = loanTerms;
            credit.LoanAmount = loanAmount;

            var points = CalculatePoints(users, index, credit);

            credit.Accepted = points > 11;

            credit.LoanAmount += credit.LoanAmount * loanPercent;

            credit.Id = await Credit.InsertCreditAsync(users[index].Id, credit);

            if (credit.Id == 0)
            {
                Shred();
                Ui.Notification("unable to add new credit");
                return;
            }

            if (!credit.Accepted)
            {
                Shred();
                Ui.Notification("We are sorry but your application was not accepted", Ui.NotificationType.Warning);
                return;
            }

            var repayments = new List<Repayment>();
            var amount = credit.LoanAmount / credit.Term;
            var currentTime = DateTime.Now;
            for (var i = 1; i <= credit.Term; i++)
            {
                currentTime = currentTime.AddMonths(1);
                var repayment = new Repayment
                {
                    Amount = amount,
                    RepaymentDate = currentTime,
                    Repaid = false,
                };
                repayments.Add(repayment);
            }


            if (await Repayment.InsertRepaymentAsync(credit.Id, repayments) < 1)
            {
                Shred();
                Ui.Notification("unable to add repayments");
                return;
            }

            var repaymentsList = new List<string>();
            repayments.ForEach(item => repaymentsList.Add($"{item} "));
            repaymentsList.Add("Back to main ");
            Console.SetCursorPosition(3, 2);

            var selectedIndex = 0;
            while (true)
            {
                Shred();
                selectedIndex = Ui.ComboBox(repaymentsList, selectedIndex);
                if (selectedIndex == repaymentsList.Count - 1)
                    break;
            }

            Shred();
            Ui.Notification("Application accepted", Ui.NotificationType.Success);
        }

        private static int CalculatePoints(IReadOnlyList<User> users, int index, Credit credit)
        {
            var points = 0;
            points += users[index].Profile.Gender switch
            {
                Profile.Genders.Male => 1,
                Profile.Genders.Female => 2,
                _ => 0
            };


            points += users[index].Profile.MaritalStatus switch
            {
                Profile.MaritalStatuses.Single or Profile.MaritalStatuses.Divorced => 1,
                Profile.MaritalStatuses.Married or Profile.MaritalStatuses.WidowWidower => 2,
                _ => 0
            };

            points += users[index].Profile.Age switch
            {
                (>= 26 and <= 35) or (>=63) => 1,
                >= 36 and <= 62 => 2,
                _ => 0
            };

            points += users[index].Profile.Nationality == "Tajikistan" ? 1 : 0;

            points += credit.History switch
            {
                >= 3 => 2,
                1 or 2 => 1,
                0 => -1,
                _ => 0
            };

            points += credit.Delinquencies switch
            {
                > 7 => -3,
                >= 5 and <=7 => -2,
                4 => -1,
                _ => 0
            };

            points += credit.Purpose switch
            {
                Credit.Purposes.Appliances => 2,
                Credit.Purposes.Repair => 1,
                Credit.Purposes.Other => -1,
                _ => 0
            };

            //                         really?
            // points += credit.Term > 12 ? 1 : 1;
            points++;

            points += (credit.LoanAmount * 100 / credit.TotalIncome) switch
            {
                <= 80 => 4,
                > 80 and <= 150 => 3,
                > 150 and <= 250 => 2,
                _ => 1
            };
            return points;
        }

        private static async Task FillUserProfileAsync(string phoneNumber)
        {
            Title("Fill user profile");

            var login = Ui.InputText("Search", 20, 3, 3, 1);
            Shred();

            var users = await User.GetUsersAsync(phoneNumber, login);

            var usersString = new List<string>();
            users.ForEach(user => usersString.Add($"{user} "));
            usersString.Add("Go back ");

            Console.SetCursorPosition(2, 2);
            var index = Ui.ComboBox(usersString);
            if (index == usersString.Count - 1)
                return;

            var genders = new List<string> {"Male   ", "Female "};

            var profile = new Profile();

            Shred();
            Console.SetCursorPosition(4, 1);
            Console.Write("Gender:");
            Console.SetCursorPosition(3, 2);
            profile.Gender = (Profile.Genders) (Ui.ComboBox(genders) + 1);

            Shred();
            _ = int.TryParse(Ui.InputText("Age:", 20, 3, 3, 1), out var age);
            if (age < 18 || age > 200)
            {
                Shred();
                Ui.Notification("invalid value for field age");
                return;
            }

            profile.Age = age;

            var maritalStatuses = new List<string> {"Single       ", "Married      ", "Divorced     ", "WidowWidower "};
            Shred();
            Console.SetCursorPosition(4, 1);
            Console.Write("Marital status:");
            Console.SetCursorPosition(3, 2);
            profile.MaritalStatus = (Profile.MaritalStatuses) (Ui.ComboBox(maritalStatuses) + 1);

            Shred();

            Console.SetCursorPosition(4, 1);
            Console.Write("Nationality:");
            Console.SetCursorPosition(3, 2);
            var nationalities = new List<string> {"Tajikistan ", "Other      "};

            var nationalitiesIndex = Ui.ComboBox(nationalities);
            if (nationalitiesIndex == nationalities.Count - 1)
            {
                Shred();
                profile.Nationality = Ui.InputText("Nationality:", 30, 3, 3, 2);
                if (profile.Nationality.Length < 2)
                {
                    Shred();
                    Ui.Notification("nationality is too short");
                    return;
                }
            }
            else
                profile.Nationality = Strings.Trim(nationalities[nationalitiesIndex]);

            if (await Profile.InsertProfileAsync(users[index].Id, profile) < 1)
            {
                Shred();
                Ui.Notification("unable to create new profile");
                return;
            }

            Shred();
            Ui.Notification("Done", Ui.NotificationType.Success);
        }

        private static async Task RegisterNewUserAsync()
        {
            Title("Register new user");

            var roles = new List<string> {"User  ", "Admin "};

            var user = new User {Login = Ui.InputText("*Phone number(ex: 992900111222)", 30, 3, 3, 1)};

            Shred();

            if (user.Login.Length < 10 && !int.TryParse(user.Login, out _))
            {
                Shred();
                Ui.Notification("invalid phone number");
                return;
            }

            user.FirstName = Ui.InputText("*Name", 20, 3, 3, 1);
            Shred();

            if (user.FirstName.Length < 1)
            {
                Shred();
                Ui.Notification("invalid first name");
                return;
            }

            user.LastName = Ui.InputText("*Surname", 20, 3, 3, 1);
            Shred();

            if (user.LastName.Length < 1)
            {
                Shred();
                Ui.Notification("invalid last name");
                return;
            }

            user.MiddleName = Ui.InputText("MiddleName", 20, 3, 3, 1);
            Shred();

            Console.SetCursorPosition(3, 1);
            user.Role = (User.Roles) (Ui.ComboBox(roles) + 1);
            Shred();


            user.Passport = Ui.InputText("*Passport data", 20, 3, 3, 1);
            Shred();

            if (user.Passport.Length < 8)
            {
                Shred();
                Ui.Notification("invalid passport data");
                return;
            }

            user.Password = Ui.InputText("*Create password", 30, 3, 3, 1, "*");
            Shred();

            if (user.Password.Length < 8)
            {
                Shred();
                Ui.Notification("password is too short");
                return;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            if (await User.AddNewUserAsync(user) < 1)
            {
                Shred();
                Ui.Notification("unable to register new user");
                return;
            }

            Shred();
            Ui.Notification("Success", Ui.NotificationType.Success);
        }

        private static void Title(string title) => Console.Title = $"IBank - {title}";

        private static void Shred() => Console.Clear();
    }
}