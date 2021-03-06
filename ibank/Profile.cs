using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ibank.Extra;
using Npgsql;
using NpgsqlTypes;

namespace ibank
{
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

        public long Id { get; set; }
        public Genders Gender { get; set; }
        public MaritalStatuses MaritalStatus { get; set; }
        public int Age { get; set; }
        public string Nationality { get; set; }
        public List<Credit> Credits { get; set; }


        public static async Task<int> InsertProfileAsync(long userId, Profile profile)
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(
                    @"insert into profiles(user_id, gender, marital_status, age, nationality)
values (@user_id, @gender, @marital_status, @age, @nationality);", connection);
                cmd.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
                cmd.Parameters.AddWithValue("gender", profile.Gender.ToString());
                cmd.Parameters.AddWithValue("marital_status", profile.MaritalStatus.ToString());
                cmd.Parameters.AddWithValue("age", NpgsqlDbType.Integer, profile.Age);
                cmd.Parameters.AddWithValue("nationality", profile.Nationality);

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
}