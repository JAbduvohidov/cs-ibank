using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ibank.Extra;
using Npgsql;

namespace ibank
{
    internal record Credit
    {
        public enum Purposes
        {
            Appliances = 1,
            Repair = 2,
            Telephone = 3,
            Other = 4
        }

        public long Id { get; set; }
        public double LoanAmount { get; set; }
        public double TotalIncome { get; set; }
        public int History { get; set; }
        public int Delinquencies { get; set; }
        public Purposes Purpose { get; set; }
        public int Term { get; set; }
        public bool Accepted { get; set; }
        public List<Repayment> Repayments { get; set; }

        public override string ToString() =>
            $"Amount: {LoanAmount}, Purpose: {Purpose}, Term: {Term}MM, Status:{(Accepted ? "✅" : "❌")}";

        public static async Task<long> InsertCreditAsync(long userId, Credit credit)
        {
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(
                    @"insert into credits(user_id, loan_amount, total_income, history, delinquencies, purpose, term, accepted)
values (@user_id, @loan_amount, @total_income, @history, @delinquencies, @purpose, @term, @accepted) returning id;",
                    connection);
                cmd.Parameters.AddWithValue("user_id", userId);
                cmd.Parameters.AddWithValue("loan_amount", credit.LoanAmount);
                cmd.Parameters.AddWithValue("total_income", credit.TotalIncome);
                cmd.Parameters.AddWithValue("history", credit.History);
                cmd.Parameters.AddWithValue("delinquencies", credit.Delinquencies);
                cmd.Parameters.AddWithValue("purpose", credit.Purpose.ToString());
                cmd.Parameters.AddWithValue("term", credit.Term);
                cmd.Parameters.AddWithValue("accepted", credit.Accepted);

                var rawCreditId = await cmd.ExecuteScalarAsync();
                if (rawCreditId != null && (long) rawCreditId != 0)
                    return (long) rawCreditId;
                else
                    return 0;
            }
            catch (Exception exception)
            {
                await connection.CloseAsync();
                Console.WriteLine(exception);
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public static async Task<List<Credit>> GetCreditHistoryAsync(string phoneNumber)
        {
            var credits = new List<Credit>();
            await using var connection = Database.GetConnection();
            using (connection.OpenAsync())
            {
                try
                {
                    await using var cmd = new NpgsqlCommand(
                        @"select loan_amount, purpose, term, accepted
from credits
         left join users u on credits.user_id = u.id
where u.login = @login;", connection);
                    cmd.Parameters.AddWithValue("login", phoneNumber);

                    var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var credit = new Credit
                        {
                            LoanAmount = reader.GetDouble(0),
                            Term = reader.GetInt32(2),
                            Accepted = reader.GetBoolean(3),
                        };
                        _ = Enum.TryParse(reader.GetString(1), out Purposes purpose);
                        credit.Purpose = purpose;

                        credits.Add(credit);
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

            return credits;
        }

        public static async Task<List<Credit>> GetRemainingLoanAsync(string phoneNumber)
        {
            var credits = new List<Credit>();
            await using var connection = Database.GetConnection();
            using (connection.OpenAsync())
            {
                try
                {
                    await using var cmd = new NpgsqlCommand(
                        @"select c.id, c.purpose, c.term, c.accepted, coalesce(sum(r.amount), 0)
from credits c
         left join users u on c.user_id = u.id
         left join repayments r on c.id = r.credit_id
where c.removed = false and c.accepted = true
  and u.login = @login
group by c.id;", connection);
                    cmd.Parameters.AddWithValue("login", phoneNumber);

                    var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var credit = new Credit
                        {
                            Id = reader.GetInt32(0),
                            Term = reader.GetInt32(2),
                            Accepted = reader.GetBoolean(3),
                            LoanAmount = reader.GetDouble(4)
                        };
                        _ = Enum.TryParse(reader.GetString(1), out Purposes purpose);
                        credit.Purpose = purpose;

                        credits.Add(credit);
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

            return credits;
        }
    }
}