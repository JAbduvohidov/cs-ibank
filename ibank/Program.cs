using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ibank.Extra;
using Microsoft.VisualBasic;
using Npgsql;
using NpgsqlTypes;
using static System.Enum;

namespace ibank
{
    internal static class Program
    {
        private static bool _isRunning = true;

        private static async Task Main(string[] args)
        {
            Shred();
            await Database.Init();

            var login = Ui.InputText("Phone/Login", 20, 3, 3, 1);
            Shred();

            var password = Ui.InputText("Password", 25, 3, 3, 1, "*");
            Shred();

            var response = await Login(login, password);
            if (response.Error != string.Empty)
            {
                Console.WriteLine(response.Error);
                return;
            }

            switch (response.User.Role)
            {
                case User.Roles.User:
                {
                    //TODO: user zone here
                    Console.WriteLine("user zone");
                    //while (isRunning) userFunctionsLoop();
                    break;
                }
                case User.Roles.Admin:
                {
                    await AdminFunctionsLoop();
                    break;
                }
                default:
                {
                    Console.WriteLine("you don't have any permissions :(");
                    break;
                }
            }
        }

        private static async Task AdminFunctionsLoop()
        {
            var items = new List<string>
            {
                "Register new user ", // 0
                "Fill user profile ", // 1
                "Filter            ", // 2
                "Edit user         ", // 3
                "Exit              ", // 4
            };

            while (_isRunning)
            {
                Shred();
                Console.SetCursorPosition(2, 2);
                var selectedIndex = Ui.ComboBox(items);
                switch (selectedIndex)
                {
                    case 0:
                    {
                        Shred();
                        var infoText = await RegisterNewUser();
                        if (infoText != string.Empty)
                        {
                            Console.WriteLine(infoText);
                            Thread.Sleep(1200);
                        }

                        break;
                    }
                    case 1:
                    {
                        Shred();
                        var infoText = await FillUserProfile();
                        if (infoText != string.Empty)
                        {
                            Shred();
                            Console.WriteLine(infoText);
                            Thread.Sleep(1200);
                        }

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

        private static async Task<string> FillUserProfile()
        {
            var login = Ui.InputText("Search", 20, 3, 3, 1);
            Shred();

            await using var connection = Database.GetConnection();
            await connection.OpenAsync();
            var users = new List<User>();
            try
            {
                await using var cmd = new NpgsqlCommand(
                    @"select u.id, u.login, u.firstname, u.lastname, u.middlename, u.role, u.passport
from users u
         left join profiles p on u.id = p.user_id
where p.id is null
  and u.removed = false
  and u.login like @login
order by u.id;", connection);
                cmd.Parameters.AddWithValue("login", $"%{login}%");

                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        Id = reader.GetInt32(0),
                        Login = reader.GetString(1),
                        FirstName = reader.GetString(2),
                        LastName = reader.GetString(3),
                        MiddleName = reader.GetString(4),
                        Passport = reader.GetString(6)
                    };
                    _ = TryParse(reader.GetString(3), out User.Roles role);
                    user.Role = role;

                    users.Add(user);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }

            var usersString = new List<string>();
            users.ForEach(user => usersString.Add($"{user} "));
            usersString.Add("Go back ");

            Console.SetCursorPosition(2, 2);
            var index = Ui.ComboBox(usersString);
            if (index == usersString.Count - 1)
                return string.Empty;

            var genders = new List<string> {"Male   ", "Female "};

            var profile = new Profile();

            Shred();
            Console.SetCursorPosition(4, 1);
            Console.Write("Gender:");
            Console.SetCursorPosition(3, 2);
            profile.Gender = (Profile.Genders) (Ui.ComboBox(genders) + 1);

            Shred();
            _ = int.TryParse(Ui.InputText("Age:", 20, 3, 3, 1), out var age);
            if (age < 18)
                return "invalid value for field age";

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
                    return "nationality is too short";
            }
            else
            {
                profile.Nationality = Strings.Trim(nationalities[nationalitiesIndex]);
            }

            await connection.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(
                    @"insert into profiles(user_id, gender, marital_status, age, nationality)
values (@user_id, @gender, @marital_status, @age, @nationality);", connection);
                cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, users[index].Id);
                cmd.Parameters.AddWithValue("gender", profile.Gender.ToString());
                cmd.Parameters.AddWithValue("marital_status", profile.MaritalStatus.ToString());
                cmd.Parameters.AddWithValue("age", NpgsqlDbType.Integer, profile.Age);
                cmd.Parameters.AddWithValue("nationality", profile.Nationality);

                if (await cmd.ExecuteNonQueryAsync() < 1)
                {
                    return "unable to fill user profile";
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }

            return string.Empty;
        }

        private static async Task<string> RegisterNewUser()
        {
            var roles = new List<string> {"User  ", "Admin "};

            var user = new User();

            user.Login = Ui.InputText("*Phone number(ex: 992900111222)", 30, 3, 3, 1);
            Shred();

            if (user.Login.Length < 10 && !int.TryParse(user.Login, out _))
                return "invalid phone number";

            user.FirstName = Ui.InputText("*Name", 20, 3, 3, 1);
            Shred();

            if (user.FirstName.Length < 1)
                return "invalid first name";

            user.LastName = Ui.InputText("*Surname", 20, 3, 3, 1);
            Shred();

            if (user.LastName.Length < 1)
                return "invalid last name";

            user.MiddleName = Ui.InputText("MiddleName", 20, 3, 3, 1);
            Shred();

            Console.SetCursorPosition(3, 1);
            user.Role = (User.Roles) (Ui.ComboBox(roles) + 1);
            Shred();


            user.Passport = Ui.InputText("*Passport data", 20, 3, 3, 1);
            Shred();

            if (user.Passport.Length < 8)
                return "invalid passport data";

            user.Password = Ui.InputText("*Create password", 30, 3, 3, 1, "*");
            Shred();

            if (user.Password.Length < 8)
                return "password is too short";

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await using var connection = Database.GetConnection();
            await connection.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(
                    @"insert into users (login, firstname, lastname, middlename, password, role, passport)
                    values (@login, @firstname, @lastname, @middlename, @password, @role, @passport);", connection);
                cmd.Parameters.AddWithValue("login", user.Login);
                cmd.Parameters.AddWithValue("firstname", user.FirstName);
                cmd.Parameters.AddWithValue("lastname", user.LastName);
                cmd.Parameters.AddWithValue("middlename", user.MiddleName);
                cmd.Parameters.AddWithValue("password", user.Password);
                cmd.Parameters.AddWithValue("role", user.Role.ToString());
                cmd.Parameters.AddWithValue("passport", user.Passport);

                if (await cmd.ExecuteNonQueryAsync() < 1)
                {
                    return "unable to add new user";
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }

            return string.Empty;
        }

        private static async Task<LoginResponseModel> Login(string login, string password)
        {
            var response = new LoginResponseModel(string.Empty, null);
            var user = new User();

            await using (var connection = Database.GetConnection())
            {
                await connection.OpenAsync();
                try
                {
                    await using var cmd =
                        new NpgsqlCommand(
                            "select id, login, password, role from users where login = @login and removed = false;",
                            connection);
                    cmd.Parameters.AddWithValue("login", NpgsqlDbType.Varchar, login);

                    //TODO: simplify this reader
                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        user.Id = reader.GetInt32(0);
                        user.Login = reader.GetString(1);
                        user.Password = reader.GetString(2);

                        TryParse(reader.GetString(3), out User.Roles role);
                        user.Role = role;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            if (user.Id == 0)
            {
                response.Error = "user not found";
                return response;
            }

            var ok = BCrypt.Net.BCrypt.Verify(password, user.Password);


            if (!ok)
            {
                response.Error = "invalid password";
                return response;
            }

            user.Password = string.Empty;
            response.User = user;

            return response;
        }

        private static void Shred() => Console.Clear();
    }

    internal record LoginResponseModel
    {
        public string Error { get; set; }
        public User User { get; set; }

        public LoginResponseModel(string error, User user)
        {
            Error = error;
            User = user;
        }
    }

    internal record User
    {
        public enum Roles
        {
            User = 1,
            Admin = 2
        }

        public int Id { get; set; }
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Password { get; set; }
        public Roles Role { get; set; }
        public string Passport { get; set; }
        public DateTime CreatedAt { get; set; }
        public Profile Profile { get; set; }

        public override string ToString()
        {
            return $"{Login} {FirstName} {LastName}";
        }
    }

    internal record Profile
    {
        public enum Genders
        {
            Male = 1,
            Female = 2
        }

        public enum MaritalStatuses
        {
            Single = 1,
            Married = 2,
            Divorced = 3,
            WidowWidower = 4,
        }

        public int Id { get; set; }
        public Genders Gender { get; set; }
        public MaritalStatuses MaritalStatus { get; set; }
        public int Age { get; set; }
        public string Nationality { get; set; }
        public List<Credit> Credits { get; set; }
    }

    internal record Credit
    {
        public enum Terms
        {
            Appliances = 1,
            Repair = 2,
            Telephone = 3,
            Other = 4
        }

        public int Id { get; set; }
        public int SumOfTotalIncome { get; set; }
        public int TotalIncome { get; set; }
        public int History { get; set; }
        public int Delinquencies { get; set; }
        public string Purpose { get; set; }
        public Terms Term { get; set; }
        public bool Accepted { get; set; }
    }
}