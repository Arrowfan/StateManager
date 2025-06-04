namespace WebApplication1.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

[ApiController]
[Route("state/{id}")]
public class StateController : ControllerBase
{
    private readonly ILogger<StateController> logger;
    private readonly IConfiguration configuration;

    public StateController(ILogger<StateController> logger, IConfiguration configuration)
    {
        this.logger = logger;
        this.configuration = configuration;
    }

    [HttpGet(Name = "GetState")]
    public ActionResult<State> Get([FromRoute] string id)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        using var connection = new MySqlConnection(connectionString);
        try
        {
            connection.Open();

            using var command = new MySqlCommand("SELECT recording FROM status WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                bool recording = reader.GetBoolean(0);
                return Ok(new State { Recording = recording });
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching state for ID: {Id}", id);
            return StatusCode(500, "Internal server error");
        }

        return Ok(new State { Recording = false }); // Or NotFound(), if preferred
    }

    [HttpPost(Name = "SetState")]
    public ActionResult<State> Post([FromRoute] string id, [FromBody] State state)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        using var connection = new MySqlConnection(connectionString);
        try
        {
            connection.Open();

            using var command = new MySqlCommand(@"
                INSERT INTO status (id, recording) 
                VALUES (@id, @recording) 
                ON DUPLICATE KEY UPDATE recording = VALUES(recording)", connection);

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@recording", state.Recording ? 1 : 0);

            command.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating state for ID: {Id}", id);
            return StatusCode(500, "Internal server error");
        }

        return Get(id);
    }
}
