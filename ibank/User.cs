using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ibank.Extra;
using Npgsql;
using NpgsqlTypes;
using static System.Enum;

namespace ibank
{
    internal record User
    {
        public enum Roles
        {
            User = 1,
            Admin = 2
        }

        public long Id { get; set; }
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Password { get; set; }
        public Roles Role { get; set; }
        public string Passport { get; set; }
        public DateTime CreatedAt { get; set; }
        public Profile Profile { get; set; }

        public override string ToString() => $"{Login} {FirstName} {LastName} {Passport}";

        public static async Task<int> AddNewUserAsync(User user)
        {
            await using var connection = Database.GetConnection();
            using (connection.OpenAsync())
            {
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

                    return await cmd.ExecuteNonQueryAsync();
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
        }

        public static async Task<AuthenticationResponseModel> AuthenticationAsync(string login, string password)
        {
            var response = new AuthenticationResponseModel(string.Empty, null);
            var user = new User();

            await using (var connection = Database.GetConnection())
            {
                using (connection.OpenAsync())
                    try
                    {
                        await using var cmd =
                            new NpgsqlCommand(
                                "select id, login, password, role from users where login = @login and removed = false;",
                                connection);
                        cmd.Parameters.AddWithValue("login", NpgsqlDbType.Varchar, login);

                        var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            user.Id = reader.GetInt32(0);
                            user.Login = reader.GetString(1);
                            user.Password = reader.GetString(2);

                            _ = TryParse(reader.GetString(3), out Roles role);
                            user.Role = role;
                        }
                    }
                    catch (Exception exception)
                    {
                        await connection.CloseAsync();
                        response.Error = exception.Message;
                        return response;
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

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                response.Error = "invalid password";
                return response;
            }

            user.Password = string.Empty;
            response.User = user;

            return response;
        }

        public static async Task<List<User>> GetUsersAsync(string phoneNumber, string login)
        {
            var users = new List<User>();
            await using var connection = Database.GetConnection();
            using (connection.OpenAsync())
            {
                try
                {
                    await using var cmd = new NpgsqlCommand(
                        @"select u.id, u.login, u.firstname, u.lastname, u.middlename, u.passport
from users u
         left join profiles p on u.id = p.user_id
where p.id is null
  and u.removed = false and login != @phone_number
  and u.login like @login
order by u.id;", connection);
                    cmd.Parameters.AddWithValue("phone_number", phoneNumber);
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
                            Passport = reader.GetString(5)
                        };

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
            }

            return users;
        }

        public static async Task<List<User>> GetUsersWithProfileAsync(string phoneNumber, string login)
        {
            var users = new List<User>();
            await using var connection = Database.GetConnection();
            using (connection.OpenAsync())
            {
                try
                {
                    await using var cmd = new NpgsqlCommand(
                        @"select u.id,
       u.login,
       u.firstname,
       u.lastname,
       u.middlename,
       u.passport,
       p.gender,
       p.marital_status,
       p.age,
       p.nationality
from users u
         left join profiles p on u.id = p.user_id
where p.id is not null
  and (select count(id) from credits where user_id = u.id and accepted = false) < 5
  and u.removed = false and u.login != @phone_number
  and u.login like @login
order by u.id;", connection);
                    cmd.Parameters.AddWithValue("phone_number", phoneNumber);
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
                            Passport = reader.GetString(5),
                            Profile = new Profile(),
                        };

                        _ = TryParse(reader.GetString(6), out Profile.Genders gender);
                        user.Profile.Gender = gender;

                        _ = TryParse(reader.GetString(7), out Profile.MaritalStatuses maritalStatus);
                        user.Profile.MaritalStatus = maritalStatus;

                        user.Profile.Age = reader.GetInt32(8);
                        user.Profile.Nationality = reader.GetString(9);

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
            }

            return users;
        }
    }

    internal record AuthenticationResponseModel
    {
        public string Error { get; set; }
        public User User { get; set; }

        public AuthenticationResponseModel(string error, User user)
        {
            Error = error;
            User = user;
        }
    }
}