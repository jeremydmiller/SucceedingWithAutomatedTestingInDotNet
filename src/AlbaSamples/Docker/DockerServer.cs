using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerTestHelpers
{
	public class DockerServers
	{
		public static IDockerClient BuildDockerClient()
		{
			var uriString = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? "npipe://./pipe/docker_engine"
				: "unix:///var/run/docker.sock";

			Console.WriteLine($"Connecting to the Docker daemon at '{uriString}'");

			var config = new DockerClientConfiguration(new Uri(uriString));

			return config.CreateClient();
		}
	}
	
	public enum StartAction
	{
		started,
		external,
		none
	}
	
    public abstract class DockerServer
    {
        public string ImageName { get; }
        public string ContainerName { get; }

        protected DockerServer(string imageName, string containerName)
        {
            ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
            ContainerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        }

	    private static async Task<bool> ImageExists(IDockerClient client, string imageName)
	    {
		    var images = await client.Images.ListImagesAsync(new ImagesListParameters {MatchName = imageName});
		    return images.Count != 0;
	    }

		public async Task Start(IDockerClient client = null)
        {
            if (StartAction != StartAction.none)
	            return;

	        client = client ?? DockerServers.BuildDockerClient();
	        
	        
	        if (await ImageExists(client, ImageName) == false)
            {
                Console.WriteLine($"Fetching Docker image '{ImageName}'");

	            var status = string.Empty;
                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = ImageName, Tag = "latest" }, 
                    null,
                    new Progress<JSONMessage>(msg => status = msg.Status));

				//If there was an error, then the call above may just return successfully without having downloaded the image
	            if (!status.Contains("Downloaded"))
	            {
					Console.WriteLine($"WARNING the image may have NOT been dowloaded. Last recorded status: {status}");
	            }
			}

            var list = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var container = list.FirstOrDefault(x => x.Names.Contains("/" + ContainerName));
            if (container == null)
            {
				await createContainer(client);
	            await JoinNetwork(client);
			}
            else
            {
                if (container.State == "running")
                {
                    Console.WriteLine($"Container '{ContainerName}' is already running.");
                    StartAction = StartAction.external;
                    return;
                }
            }

            var started = await client.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters()
            {
				
            });
            if (!started)
            {
                throw new InvalidOperationException($"Container '{ContainerName}' did not start!!!!");
            }

            var i = 0;
            while (!await isReady())
            {
                i++;

                if (i > 20)
                {
                    throw new TimeoutException($"Container {ContainerName} does not seem to be responding in a timely manner");
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Console.WriteLine($"Container '{ContainerName}' is ready.");

            StartAction = StartAction.started;
        }

	    private async Task JoinNetwork(IDockerClient client)
	    {
			//If there is a 'Tests' network, then the container will try and join it.
			//Please note, that after this, ALL containers that need to interact with this one, must be on the same network
		    var allNetworks = await client.Networks.ListNetworksAsync();
		    var testNetwork = allNetworks.FirstOrDefault(n => n.Name == "Tests");
		    if (testNetwork != null)
		    {
			    await client.Networks.ConnectNetworkAsync(testNetwork.ID, new NetworkConnectParameters {Container = ContainerName});
		    }
	    }

	    public StartAction StartAction { get; private set; } = StartAction.none;

        private async Task createContainer(IDockerClient client)
        {
            Console.WriteLine($"Creating container '{ContainerName}' using image '{ImageName}'");

            var hostConfig = ToHostConfig();
            var config = ToConfig();

	        await client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
            {
                Image = ImageName,
                Name = ContainerName,
                Tty = true,
                HostConfig = hostConfig,
            });
        }

        public async Task Stop(IDockerClient client)
        {
            await client.Containers.StopContainerAsync(ContainerName, new ContainerStopParameters());
        }

        public Task Remove(IDockerClient client)
        {
            return client.Containers.RemoveContainerAsync(ContainerName,
                new ContainerRemoveParameters { Force = true });
        }

        protected abstract Task<bool> isReady();

        public abstract HostConfig ToHostConfig();

        public abstract Config ToConfig();

        public override string ToString()
        {
            return $"{nameof(ImageName)}: {ImageName}, {nameof(ContainerName)}: {ContainerName}";
        }
    }
}
