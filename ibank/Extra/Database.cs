using System.Threading.Tasks;
using Npgsql;

namespace ibank.Extra
{
    public static class Database
    {
        //TODO: get database connection info from environment variables or application arguments
        private const string ConnectionString =
            "Server=localhost;Port=5432;User Id=user;Password=pass;Database=ibankdb;";

        public static async Task Init()
        {
            await using var connection = GetConnection();
            await connection.OpenAsync();

            await using var tableUsers = new NpgsqlCommand(@"create table if not exists users
(
    id         bigserial primary key,
    login      varchar(255) not null unique,
    firstname  varchar(255) not null,
    lastname   varchar(255) not null,
    middlename varchar(255) default '',
    password   varchar(255) not null,
    role       varchar(20)  default 'User',
    passport   varchar(20)  not null,
    created_at timestamp    default now(),
--     updated_at timestamp   default now(),
    removed    boolean      default false
);", connection);
            await tableUsers.ExecuteNonQueryAsync();

            await using var tableProfiles = new NpgsqlCommand(@"create table if not exists profiles
(
    id             bigserial primary key,
    user_id        bigint      not null references users unique,
    gender         varchar(20) not null,
    marital_status varchar(50) not null,
    age            integer     not null check ( age > 0 ),
    nationality    varchar(100) default 'Tajikistan'
);", connection);
            await tableProfiles.ExecuteNonQueryAsync();

            await using var tableCredits = new NpgsqlCommand(@"create table if not exists credits
(
    id                    bigserial primary key,
    --user_id bigint not null references users,
    profile_id            bigint       not null references profiles,
    sum_from_total_income integer      not null,
    total_income          integer      not null,
    history               integer      not null, --closed credits
    delinquencies         integer      not null, --delayed credits
    purpose               varchar(20)  not null,
    term                  varchar(100) not null,
    accepted              boolean default false,
    removed               boolean default false
);", connection);
            await tableCredits.ExecuteNonQueryAsync();

            await using var tableRepayments = new NpgsqlCommand(@"create table if not exists repayments
(
    id        bigserial primary key,
    credit_id bigint    not null references credits,
    date      timestamp not null,
    amount    integer   not null,
    repaid    boolean default false,
    removed   boolean default false
);", connection);
            await tableRepayments.ExecuteNonQueryAsync();

            //TODO: get first user data from environment variables or application arguments

            await using var checkIfModeratorExists =
                new NpgsqlCommand("select exists(select 1 from users where login = 'moderator' and removed = false);",
                    connection);

            var exists = await checkIfModeratorExists.ExecuteScalarAsync();
            if (exists != null && (bool) exists)
                return;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("password");
            await using var insertFirstAdmin = new NpgsqlCommand(
                @"insert into users (login, firstname, lastname, password, role, passport)
values ('moderator', 'Moderator', 'Moderator', @password, @role, 'A00000001') on conflict do nothing;", connection);
            insertFirstAdmin.Parameters.AddWithValue("password", passwordHash);
            insertFirstAdmin.Parameters.AddWithValue("role", User.Roles.Admin.ToString());
            await insertFirstAdmin.ExecuteNonQueryAsync();
        }

        public static NpgsqlConnection GetConnection()
        {
            return new(ConnectionString);
        }
    }
}