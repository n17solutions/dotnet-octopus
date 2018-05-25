using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Octopus.Client;
using Octopus.Client.Model;

namespace Dotnet.Octopus
{
    public class Option
    {
        public string Template { get; set; }
        public string Description { get; set; }
    }
    
    public static class Program
    {
        public static void Main(string[] args)
        {
            const string helpTemplate = "-? | -h | --help";
            var server = new Option {Template = "-s | --server <server>", Description = "URL of the Octopus Deploy server."};
            var apiKey = new Option {Template = "-k | --api-key <api-key>", Description = "API Key to use to connect to the Octopus Deploy server."};
            var projectName = new Option {Template = "-p | --project-name <project-name>", Description = "The Project to interact with."};
            var semVer = new Option {Template = "-sv | --sem-ver <sem-ver>", Description = "The Semantic Version to interact with."};
            
            var app = new CommandLineApplication();
            
            
            app.Command("release", config =>
            {
                config.Description = "Creates an Octopus Deploy release.";
                config.HelpOption(helpTemplate);

                var serverOption = config.Option(server.Template, server.Description, CommandOptionType.SingleValue);
                var apiKeyOption = config.Option(apiKey.Template, apiKey.Description, CommandOptionType.SingleValue);
                var projectNameOption = config.Option(projectName.Template, projectName.Description, CommandOptionType.SingleValue);
                var semVerOption = config.Option(semVer.Template, semVer.Description, CommandOptionType.SingleValue);
                var releaseNotesOption = config.Option("-rn | --release-notes <release-notes>", "The Release notes", CommandOptionType.SingleValue);

                config.OnExecute(async () =>
                {
                    var validationPassed = true;
                    if (!serverOption.HasValue())
                    {
                        Console.WriteLine("Server parameter must be passed.");
                        validationPassed = false;
                    }
                
                    if (!apiKeyOption.HasValue())
                    {
                        Console.WriteLine("API Key parameter must be passed.");
                        validationPassed = false;
                    }
                    
                    if (!projectNameOption.HasValue())
                    {
                        Console.WriteLine("Project Name parameter must be passed.");
                        validationPassed = false;
                    }
                    
                    if (!semVerOption.HasValue())
                    {
                        Console.WriteLine("SemVer parameter must be passed.");
                        validationPassed = false;
                    }

                    if (!validationPassed)
                        return 1;

                    return await CreateRelease(serverOption.Value(), apiKeyOption.Value(), projectNameOption.Value(), semVerOption.Value(), releaseNotesOption.Value()).ConfigureAwait(false);                    
                });
            });

            app.Command("promote", config =>
            {
                config.Description = "Promotes an Octopus Deploy release.";
                config.HelpOption(helpTemplate);
                
                var serverOption = config.Option(server.Template, server.Description, CommandOptionType.SingleValue);
                var apiKeyOption = config.Option(apiKey.Template, apiKey.Description, CommandOptionType.SingleValue);
                var projectNameOption = config.Option(projectName.Template, projectName.Description, CommandOptionType.SingleValue);
                var semVerOption = config.Option(semVer.Template, semVer.Description, CommandOptionType.SingleValue);
                var environmentOption = config.Option("-e | --environment <environment>", "The environment to promote the release to.", CommandOptionType.SingleValue);
                
                config.OnExecute(async () =>
                {
                    var validationPassed = true;
                    if (!serverOption.HasValue())
                    {
                        Console.WriteLine("Server parameter must be passed.");
                        validationPassed = false;
                    }
                
                    if (!apiKeyOption.HasValue())
                    {
                        Console.WriteLine("API Key parameter must be passed.");
                        validationPassed = false;
                    }
                    
                    if (!projectNameOption.HasValue())
                    {
                        Console.WriteLine("Project Name parameter must be passed.");
                        validationPassed = false;
                    }
                    
                    if (!semVerOption.HasValue())
                    {
                        Console.WriteLine("SemVer parameter must be passed.");
                        validationPassed = false;
                    }
                    
                    if (!environmentOption.HasValue())
                    {
                        Console.WriteLine("Environment parameter must be passed.");
                        validationPassed = false;
                    }

                    if (!validationPassed)
                        return 1;

                    return await PromoteRelease(serverOption.Value(), apiKeyOption.Value(), environmentOption.Value(), projectNameOption.Value(), semVerOption.Value()).ConfigureAwait(false);
                });
            });
            
            app.HelpOption(helpTemplate);
            var result = app.Execute(args);
            
            Environment.Exit(result);
        }

        private static async Task<int> CreateRelease(string server, string apiKey, string projectName, string semVer, string releaseNotes)
        {
            Console.WriteLine($"Creating Octopus Deploy Release for project: {projectName} {semVer}");
            
            try
            {
                var octoEndpoint = new OctopusServerEndpoint(server, apiKey);
                using (var client = await OctopusAsyncClient.Create(octoEndpoint).ConfigureAwait(false))
                {
                    var octoRepository = client.Repository;
                    var octoProject = await octoRepository.Projects.FindOne(projectResource => projectResource.Name.Equals(projectName)).ConfigureAwait(false);
                    var octoProcess = await octoRepository.DeploymentProcesses.Get(octoProject.DeploymentProcessId).ConfigureAwait(false);
                    var octoChannel = await octoRepository.Client.Get<ChannelResource>($"/api/projects/{octoProject.Id}/channels").ConfigureAwait(false);
                    var octoTemplate = await octoRepository.DeploymentProcesses.GetTemplate(octoProcess, octoChannel).ConfigureAwait(false);
                    var octoReleaseResource = new ReleaseResource
                    {
                        Version = semVer,
                        ProjectId = octoProject.Id,
                        ReleaseNotes = releaseNotes
                    };

                    foreach (var package in octoTemplate.Packages)
                    {
                        var selectedPackage = new SelectedPackage
                        {
                            ActionName = package.ActionName,
                            Version = semVer
                        };

                        octoReleaseResource.SelectedPackages.Add(selectedPackage);
                    }

                    await octoRepository.Releases.Create(octoReleaseResource).ConfigureAwait(false);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Octopus Deploy Create Release failed with message:{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return 1;
            }
        }
        
        private static async Task<int> PromoteRelease(string server, string apiKey, string environmentName, string projectName, string semVer)
        {
            Console.WriteLine($"Promoting Octopus Deploy Release for project: {projectName} {semVer} to environment: {environmentName}");
            
            try
            {
                var octoEndpoint = new OctopusServerEndpoint(server, apiKey);
                using (var client = await OctopusAsyncClient.Create(octoEndpoint).ConfigureAwait(false))
                {
                    var octoRepository = client.Repository;
                    var octoEnv = (await octoRepository.Environments.GetAll().ConfigureAwait(false)).First(x => x.Name.Equals(environmentName));
                    var octoProject = await octoRepository.Projects.FindOne(projectResource => projectResource.Name.Equals(projectName)).ConfigureAwait(false);
                    var octoRelease = await octoRepository.Releases.FindOne(releaseResource => releaseResource.Version.Equals(semVer) && releaseResource.ProjectId.Equals(octoProject.Id)).ConfigureAwait(false);
                    var octoDeploymentResource = new DeploymentResource
                    {
                        ReleaseId = octoRelease.Id,
                        ProjectId = octoRelease.ProjectId,
                        EnvironmentId = octoEnv.Id
                    };

                    await octoRepository.Deployments.Create(octoDeploymentResource).ConfigureAwait(false);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Octopus Deploy Promote Release failed with message:{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                return 1;
            }
        }
    }
}