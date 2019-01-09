using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Xunit;

namespace DockerTestHelpers
{

    public class SqlServerFixture : IDisposable
    {
        public readonly SqlServerContainer TheContainer = new SqlServerContainer("MySqlServer");

        public SqlServerFixture()
        {
            TheContainer.Start().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            // Nothing
        }
    }

    public class test_class_that_needs_the_sql_server : IClassFixture<SqlServerFixture>
    {
        private readonly SqlServerFixture _fixture;

        public test_class_that_needs_the_sql_server(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void test_that_uses_sql_server()
        {
            var conn = new SqlConnection(_fixture.TheContainer.ConnectionString);
        }
    }
    
    
    public class SqlServerContainer : DockerServer
    {
        private readonly int _port;
        private string _password;

        public SqlServerContainer(string containerName, int port = 1434, string password = "P@55w0rd") : base("microsoft/mssql-server-linux:latest", containerName)
        {
            _port = port;
            ConnectionString = $"Server=localhost,{port};User Id=sa;Password={password};Timeout=5";
            _password = password;
        }
        
        public string ConnectionString { get; }

        protected override async Task<bool> isReady()
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override HostConfig ToHostConfig()
        {
            return new HostConfig
            {

                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        "1433/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostPort = _port.ToString(),
                                HostIP = "127.0.0.1"
                            }
                        }
                    },
                },
            };
        }

        public override Config ToConfig()
        {
            return new Config
            {
                Env = new List<string> { "ACCEPT_EULA=Y", $"SA_PASSWORD={_password}", "MSSQL_PID=Developer" }
            };
        }
    }
}
