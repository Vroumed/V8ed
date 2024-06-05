using MySqlConnector;
using Vroumed.V8ed.Models.Configuration;

namespace Vroumed.V8ed.Managers;

public class DatabaseManager
{
  public string Server { get; init; }
  public string Database { get; init; }
  public string Username { get; init; }
  public string Password { get; init; }
  public string ConnectionString => $"server={Server};database={Database};uid={Username};pwd={Password};";
  public MySqlConnection Connection { get; init; }
  public MySqlDataReader? Reader { get; set; }
  public MySqlTransaction? Transaction { get; set; }

  public DatabaseManager(ServerConfiguration configuration)
  {
    Server = configuration.Server;
    Database = configuration.Database;
    Username = configuration.Username;
    Password = configuration.Password;
    Connection = new(ConnectionString);
  }

  public DatabaseManager(string server, string database, string username, string password)
  {
    Server = server;
    Database = database;
    Username = username;
    Password = password;
    Connection = new(ConnectionString);
  }

  /// <summary>
  /// Open a connection, fetch one row, then close the connection, return return a dictionnary of <see cref="object"/>
  /// </summary>
  /// <param name="query">the SQL query</param>
  /// <param name="parameters">parameters to sanitize your request</param>
  /// <param name="user"></param>
  /// <returns>the row returned</returns>
  public async Task<Dictionary<string, object>?> FetchOne(string query, IDictionary<string, object>? parameters = null)
  {
    return await FetchOne<object>(query, parameters);
  }

  public async Task<List<Dictionary<string, object>>> FetchAll(string query, IDictionary<string, object>? parameters = null)
  {
    return await FetchAll<object>(query, parameters);
  }

  /// <summary>
  /// Open a connection, fetch one row, then close the connection. return a dictionnary of <typeparamref name="T"/> 
  /// </summary>
  /// <typeparam name="T">If your fetch is sure to contain a single type, it will cast the whole dictionnary to this type, if multiple types are neede, must be <see cref="object"/>, or <seealso cref="FetchOne(string, IEnumerable{object})"/></typeparam>
  /// <param name="query">the SQL query</param>
  /// <param name="parameters">parameters to sanitize your request</param>
  /// <returns>the row returned</returns>
  public async Task<Dictionary<string, T>?> FetchOne<T>(string query, IDictionary<string, object>? parameters = null)
  {
    await OpenReader(query, parameters);
    Dictionary<string, T> result = new();
    if (!Reader!.HasRows)
    {
      await Reader.CloseAsync();
      await Connection.CloseAsync();
      Reader = null;
      return null;
    }

    await Reader.ReadAsync();

    for (int i = 0; i < Reader.FieldCount; i++)
      result[Reader.GetName(i)] = (T) Reader.GetValue(i);

    await Reader.CloseAsync();
    await Connection.CloseAsync();
    Reader = null;
    return result;
  }

  /// <summary>
  /// Fetch a row of an already Openned Reader
  /// </summary>
  /// <returns>the row returned</returns>
  public async Task<Dictionary<string, object>?> FetchOne()
  {
    return await FetchOne<object>();
  }

  /// <summary>
  /// Fetch a row of an already Openned Reader
  /// </summary>
  /// <typeparam name="T">If your fetch is sure to contain a single type, it will cast the whole dictionnary to this type, if multiple types are neede, must be <see cref="object"/>, or <seealso cref="FetchOne()"/></typeparam>
  /// <returns></returns>
  public async Task<Dictionary<string, T>?> FetchOne<T>()
  {
    EnsureReaderOpenned();
    bool t = await Reader!.ReadAsync();
    if (!t)
      return null;
    Dictionary<string, T> result = new();
    for (int i = 0; i < Reader!.FieldCount; i++)
      result[Reader.GetName(i)] = (T) Reader.GetValue(i);

    return result;
  }

  /// <summary>
  /// Open a connection, fetch every row, then close the connection. This can be really Memory Heavy for big database, consider 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="query"></param>
  /// <param name="parameters"></param>
  /// <returns></returns>
  public async Task<List<Dictionary<string, T>>> FetchAll<T>(string query, IDictionary<string, object>? parameters = null)
  {
    await OpenReader(query, parameters);
    List<Dictionary<string, T>> result = new();
    if (!Reader!.HasRows)
    {
      await CloseReader();
      return result;
    }

    while (await Reader.ReadAsync())
    {
      Dictionary<string, T> row = new();

      result.Add(row);
    }

    await CloseReader();
    return result;
  }

  public async Task Execute(string query, IDictionary<string, object>? parameters = null)
  {
    await Connection.OpenAsync();
    MySqlCommand command = new(query, Connection, Transaction);
    if (parameters != null)
      command.Parameters.AddRange(parameters.ToArray());
    await command.ExecuteNonQueryAsync();
    await Connection.CloseAsync();
  }

  public async Task OpenReader(string query, IDictionary<string, object>? parameters = null)
  {
    AssertReaderOpenned();
    await Connection.OpenAsync();
    MySqlCommand command = new(query, Connection, Transaction);
    if (parameters != null)
      foreach (KeyValuePair<string, object> parameter in parameters)
        command.Parameters.AddWithValue(parameter.Key, parameter.Value);

    Reader = await command.ExecuteReaderAsync();
  }

  public async Task CloseReader()
  {
    EnsureReaderOpenned();
    await Reader!.CloseAsync();
    await Connection.CloseAsync();
  }

  public async Task OpenTransaction()
  {
    AssertTransactionOpenned();
    Transaction = await Connection.BeginTransactionAsync();
  }

  public async Task CommitTransaction()
  {
    EnsureTransactionOpenned();
    await Transaction!.CommitAsync();
  }

  public async Task RollbackTransaction()
  {
    EnsureTransactionOpenned();
    await Transaction!.RollbackAsync();
  }

  #region Security Checks
  private void AssertReaderOpenned()
  {
    if (Connection.State != System.Data.ConnectionState.Closed)
      throw new InvalidOperationException($"To start a new query, connection must be closed, current state is is {Connection.State}");
  }
  private void EnsureReaderOpenned()
  {
    if (Reader == null || Reader.IsClosed)
      throw new InvalidOperationException($"Tried to use a closed Reader");
  }
  private void EnsureTransactionOpenned()
  {
    if (Transaction == null)
      throw new InvalidOperationException($"Tried to close an unexisting Transaction");
  }
  private void AssertTransactionOpenned()
  {
    if (Transaction != null)
      throw new InvalidOperationException($"Tried to open an already openned Transaction");
  }
  #endregion
}