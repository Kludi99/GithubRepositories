using Microsoft.Extensions.Configuration;
using Octokit;
using System.Security.Cryptography;

namespace GithubRepositories
{
    public class GithubConnection
    {
        private GitHubClient Github = new GitHubClient(new ProductHeaderValue("GithubRepositories"));
        private User User { get; set; }
        private IReadOnlyList<Repository> Repositories { get; set; }
        public GithubConnection(IConfiguration config)
        {
            User = Github.User.Get(config["User"]).Result;
            Github.Credentials = new Credentials(config["Credentials"]);
            Repositories = new List<Repository>();
        }


        public async Task ShowPrivateRepositories()
        {
            try
            {
                Repositories = await Github.Repository.GetAllForCurrent();
                Console.WriteLine("\nPrivate Repositories:");
                foreach (var repo in Repositories.Where(x => x.Visibility == RepositoryVisibility.Private))
                {
                    Console.WriteLine(repo.Name);
                }
                await Console.Out.WriteLineAsync();
            }
            catch (Exception) { Console.WriteLine("Show repositories from GitHub failed"); }

        }
        private Repository GetRepository()
        {
            Console.WriteLine("\n\nType repository name");
            var repositoryName = Console.ReadLine();
            return Repositories.Where(x => x.Name.Equals(repositoryName)).FirstOrDefault();

        }

        public async Task<(List<Issue>, string)> ShowIssues()
        {
            try
            {
                var repository = GetRepository();
                if (repository is not null)
                {
                    var issues = await Github.Issue.GetAllForRepository(repository.Owner.Login, repository.Name);
                    var issueList = new List<Issue>();
                    foreach (var item in issues)
                    {
                        Console.WriteLine($"Issue: {item.Title} \n {item.Body} \n {item.Url} {item.State}");
                        var issue = new Issue { Number = item.Number, Description = item.Body, Title = item.Title, Url = item.Url, Status = item.State.StringValue };
                        issueList.Add(issue);
                    }
                    return (issueList, repository.Name);
                }
                else
                {
                    await Console.Out.WriteLineAsync("Selected repository does not exist");
                    return (null, string.Empty);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Show issues from repository failed");
                return (null, string.Empty);
            }
        }

        public async Task CreateRepositoryFromBackup(string backupPath, Aes myAes, string date, string selectedBackup)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    var fileRead = File.ReadAllBytes(backupPath);
                    var decrypted = EncryptDecrypt.DecryptIssueFromBytes_Aes(fileRead, myAes.Key, myAes.IV);

                    var backupRepoName = $"{selectedBackup}-{DateTime.Parse(date).ToString("yyyy-dd-M--HH-mm-ss")}";
                    await Github.Repository.Create(new NewRepository(backupRepoName));
                    var repo = await Github.Repository.Get(User.Login, backupRepoName);
                    await Console.Out.WriteLineAsync("\n\nDecrypted issues:");
                    foreach (var item in decrypted)
                    {
                        Console.WriteLine("Decrypted: Title: {0} \n Body: {1} \n Url: {2} \n Number: {3} \n Status: {4}", item.Title, item.Description, item.Url, item.Number, item.Status);
                        var newIssue = new NewIssue(item.Title);
                        newIssue.Body = item.Description;
                        await Github.Issue.Create(repo.Id, newIssue);
                    }
                }
            }
            catch(Exception ex) { Console.WriteLine(ex); }
        }
    }
}
