using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ibank.Extra;
using Npgsql;

namespace ibank
{
    internal record Repayment
    {
        public long Id { get; set; }
        public DateTime RepaymentDate { get; set; }
        public double Amount { get; set; }
        public bool Repaid { get; set; }

        public override string ToString() =>
            $"Date: {RepaymentDate}, Amount: {Amount}, Repaid: {(Repaid ? "✅" : "❌")}";

        public static async Task<int> InsertRepaymentAsync(long creditId, IEnumerable<Repayment> repayments)
        {
            await using var connection = Database.GetConnection();
            var inserted = 0;
            await connection.OpenAsync();
            try
            {
                foreach (var repayment in repayments)
                {
                    await using var cmd = new NpgsqlCommand(
                        @"insert into repayments (credit_id, date, amount)
values (@credit_id, @date, @amount);", connection);
                    cmd.Parameters.AddWithValue("credit_id", creditId);
                    cmd.Parameters.AddWithValue("date", repayment.RepaymentDate);
                    cmd.Parameters.AddWithValue("amount", repayment.Amount);

                    inserted += await cmd.ExecuteNonQueryAsync();
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

            return inserted;
        }

        public static async Task<List<Repayment>> GetCreditRepaymentsAsync(long creditId)
        {
            var repayments = new List<Repayment>();
            await using var connection = Database.GetConnection();
            await connection.OpenAsync();
            try
            {
                await using var cmd = new NpgsqlCommand(
                    @"select r.id, r.amount, r.repaid, r.date
from repayments r
         left join credits c on c.id = r.credit_id
where r.removed = false and r.repaid = false and r.credit_id = @credit_id;", connection);
                cmd.Parameters.AddWithValue("credit_id", creditId);

                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var repayment = new Repayment
                    {
                        Id = reader.GetInt32(0),
                        Amount = reader.GetDouble(1),
                        Repaid = reader.GetBoolean(2),
                        RepaymentDate = reader.GetDateTime(3)
                    };

                    repayments.Add(repayment);
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

            return repayments;
        }
    }
}