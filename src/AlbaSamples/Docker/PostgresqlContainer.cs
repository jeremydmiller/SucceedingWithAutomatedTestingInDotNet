using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Npgsql;

namespace DockerTestHelpers
{
    public class PostgresqlContainer : DockerServer
    {
        private readonly int _port;

        public PostgresqlContainer(string name, int port = 5433) : base("clkao/postgres-plv8:latest", name)
        {
            ConnectionString = $"Host=localhost;Port={port};Database=postgres;Username=postgres;password=postgres";
            _port = port;
        }

        public string ConnectionString { get; }
            

        protected override async Task<bool> isReady()
        {
            try
            {
                using (var conn =
                    new NpgsqlConnection(ConnectionString))
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
                        "5432/tcp",
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
                Env = new List<string> {"POSTGRES_PASSWORD=postgres"}
            };
        }
    }
}