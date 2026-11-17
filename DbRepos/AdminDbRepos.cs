using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

using Seido.Utilities.SeedGenerator;
using Models.DTO;
using DbModels;
using DbContext;
using Encryption;


namespace DbRepos;

public class AdminDbRepos
{
    private const string _seedSource = "./app-seeds.json";
    private readonly ILogger<AdminDbRepos> _logger;
    private Encryptions _encryptions;
    private readonly MainDbContext _dbContext;

    public AdminDbRepos(ILogger<AdminDbRepos> logger, Encryptions encryptions, MainDbContext context)
    {
        _logger = logger;
        _encryptions = encryptions;
        _dbContext = context;
    }

    public async Task<ResponseItemDto<GstUsrInfoAllDto>> InfoAsync() => await DbInfo();


    private async Task<ResponseItemDto<GstUsrInfoAllDto>> DbInfo()
    {
        var info = new GstUsrInfoAllDto();
        info.Db = await _dbContext.InfoDbView.FirstAsync();
        return new ResponseItemDto<GstUsrInfoAllDto>
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif

            Item = info
        };
    }

    public async Task<ResponseItemDto<GstUsrInfoAllDto>> SeedAsync(int nrOfItems)
    {
        var fn = Path.GetFullPath(_seedSource);
        var seeder = new SeedGenerator(fn);

        //get a list of music groups
        var musicGroups = seeder.ItemsToList<MusicGroupDbM>(nrOfItems);

        //Set between 5 and 50 albums for each music groups
        musicGroups.ForEach(mg => mg.AlbumsDbM = seeder.ItemsToList<AlbumDbM>(seeder.Next(2, 5)));

        //get a list of artists
        var artists = seeder.ItemsToList<ArtistDbM>(100);

        //Assign artists to Music groups
        musicGroups.ForEach(mg => mg.ArtistsDbM = seeder.UniqueIndexPickedFromList<ArtistDbM>(seeder.Next(2, 5), artists));

        //Note that all other tables are automatically set through csFriendDbM Navigation properties
        _dbContext.MusicGroups.AddRange(musicGroups);

        await _dbContext.SaveChangesAsync();
        return await DbInfo();
    }
      
    public async Task<ResponseItemDto<GstUsrInfoAllDto>> RemoveSeedAsync(bool seeded)
    {
        // Create parameters based on database provider
        var connection = _dbContext.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;

        List<DbParameter> parameters;
        if (connection is MySqlConnection)
        {
            command.CommandText = "supusr_spDeleteAll";
            parameters = new List<DbParameter>
            {
                new MySqlParameter("seededParam", seeded),
                new MySqlParameter("nrMusicGroupsAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output },
                new MySqlParameter("nrAlbumsAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output },
                new MySqlParameter("nrArtistsAffected", MySqlDbType.Int32) { Direction = ParameterDirection.Output }
            };
        }
        else if (connection is NpgsqlConnection)
        {
            // PostgreSQL parameters - call as function returning table
            command.CommandText = "SELECT nrMusicGroupsAffected, nrAlbumsAffected, nrArtistsAffected FROM supusr.\"spDeleteAll\"(@seededParam)";
            command.CommandType = CommandType.Text;
            parameters =
            [
                new NpgsqlParameter("seededParam", seeded),
                new NpgsqlParameter("nrMusicGroupsAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output },
                new NpgsqlParameter("nrAlbumsAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output },
                new NpgsqlParameter("nrArtistsAffected", NpgsqlTypes.NpgsqlDbType.Integer) { Direction = ParameterDirection.Output }
            ];
        }
        else
        {
            // SQL Server parameters (default)
            command.CommandText = "supusr.spDeleteAll";
            parameters = new List<DbParameter>
            {
                new SqlParameter("seededParam", seeded),
                new SqlParameter("nrMusicGroupsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("nrAlbumsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("nrArtistsAffected", SqlDbType.Int) { Direction = ParameterDirection.Output }
            };
        }

        command.Parameters.AddRange(parameters.ToArray());

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        if (connection is NpgsqlConnection)
        {
            // in postgresql, execute a procedure (a function) cannot return a dataset and have output parameters
            // therefore I execute the command without expecting a result set
            await command.ExecuteScalarAsync();
        }
        else
        {
            // Execute the stored procedure and get the result set
            using var reader = await command.ExecuteReaderAsync();

            // map reader result into GstUsrInfoDbDto result_set
            GstUsrInfoDbDto result_set = null;
            if (reader.HasRows)
            {
                // Read the first result set which should be InfoDbView
                await reader.ReadAsync();

                result_set = new GstUsrInfoDbDto
                {
                    // Populate properties from the reader
                    NrSeededMusicGroups = Convert.ToInt32(reader["NrSeededMusicGroups"]),
                    NrUnseededMusicGroups = Convert.ToInt32(reader["NrUnseededMusicGroups"]),
                    NrSeededAlbums = Convert.ToInt32(reader["NrSeededAlbums"]),
                    NrUnseededAlbums = Convert.ToInt32(reader["NrUnseededAlbums"]),
                    NrSeededArtists = Convert.ToInt32(reader["NrSeededArtists"]),
                    NrUnseededArtists = Convert.ToInt32(reader["NrUnseededArtists"]),
                };
            }
            await reader.CloseAsync();
            // result_set can now be accessed - not used in this example
        }


        // Output parameters can now be accessed - not used in this example
        int nrMusicGroupsAffected = (int)parameters.First(p => p.ParameterName == "nrMusicGroupsAffected").Value;
        int nrAlbumsAffected = (int)parameters.First(p => p.ParameterName == "nrAlbumsAffected").Value;
        int nrArtistsAffected = (int)parameters.First(p => p.ParameterName == "nrArtistsAffected").Value;

        return await DbInfo();
    }
    
    public async Task<ResponseItemDto<UsrInfoDto>> SeedUsersAsync(int nrOfUsers, int nrOfSuperUsers, int nrOfDbOwners)
    {
        _logger.LogInformation($"Seeding {nrOfUsers} users and {nrOfSuperUsers} superusers");

        //First delete all existing users
        foreach (var u in _dbContext.Users)
            _dbContext.Users.Remove(u);

        //add users
        for (int i = 1; i <= nrOfUsers; i++)
        {
            _dbContext.Users.Add(new UserDbM
            {
                UserId = Guid.NewGuid(),
                UserName = $"user{i}",
                Email = $"user{i}@gmail.com",
                PasswordHash = _encryptions.EncryptPasswordToBase64($"user{i}"),
                UserRole = "usr"
            });
        }

        //add super user
        for (int i = 1; i <= nrOfSuperUsers; i++)
        {
            _dbContext.Users.Add(new UserDbM
            {
                UserId = Guid.NewGuid(),
                UserName = $"superuser{i}",
                Email = $"superuser{i}@gmail.com",
                PasswordHash = _encryptions.EncryptPasswordToBase64($"superuser{i}"),
                UserRole = "supusr"
            });
        }

        //add system adminitrators
        for (int i = 1; i <= nrOfDbOwners; i++)
        {
            _dbContext.Users.Add(new UserDbM
            {
                UserId = Guid.NewGuid(),
                UserName = $"dbo{i}",
                Email = $"dbo{i}@gmail.com",
                PasswordHash = _encryptions.EncryptPasswordToBase64($"dbo{i}"),
                UserRole = "dbo"
            });
        }
        await _dbContext.SaveChangesAsync();

        var _info = new UsrInfoDto
        {
            NrUsers = await _dbContext.Users.CountAsync(i => i.UserRole == "usr"),
            NrSuperUsers = await _dbContext.Users.CountAsync(i => i.UserRole == "supusr"),
            NrDbOwners = await _dbContext.Users.CountAsync(i => i.UserRole == "dbo")
        };

        return new ResponseItemDto<UsrInfoDto>()
        {
#if DEBUG
            ConnectionString = _dbContext.dbConnection,
#endif
            Item = _info
        };
    }
}
