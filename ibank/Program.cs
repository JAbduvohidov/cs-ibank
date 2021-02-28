using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ibank.Extra;
using Npgsql;
using NpgsqlTypes;
using static System.Enum;

namespace ibank
{
    internal static class Program
    {
        private static bool isRunning = true;

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
                "List users        ", // 2
                "Find user         ", // 3
                "Edit user         ", // 4
                "Exit              ", // 5
            };

            while (isRunning)
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
                    case 5:
                    {
                        isRunning = false;
                        break;
                    }
                }
            }
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
                        new NpgsqlCommand("select id, login, password, role from users where login = @login;",
                            connection);
                    cmd.Parameters.AddWithValue("login", NpgsqlDbType.Varchar, login);

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

    class LoginResponseModel
    {
        public string Error { get; set; }
        public User User { get; set; }

        public LoginResponseModel(string error, User user)
        {
            Error = error;
            User = user;
        }
    }

    class User
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
    }

    class Profile
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
        public List<Credit>? Credits { get; set; }
    }

    class Credit
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