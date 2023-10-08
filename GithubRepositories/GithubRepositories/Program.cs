// See https://aka.ms/new-console-template for more information
using GithubRepositories;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Octokit;
using Spectre.Console;
using System.Security.Cryptography;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration config = builder.Build();

string workingDirectory = Environment.CurrentDirectory;
var currentDate = DateTime.Now;
using Aes myAes = Aes.Create();
GithubConnection githubConnection;
try
{
    githubConnection = new GithubConnection(config);
}
catch (Exception)
{
    Console.WriteLine("[ERROR] Check configuration");
    return 0;
}
var (issueList, repository) = (new List<GithubRepositories.Issue>(), string.Empty);
var database = new Database(config);


while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Choose a step:")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more functions)[/]")
            .AddChoices(new[] {
            "1. List Private Repositories",
            "2. Select a Repository to Download Issues",
            "3. Encrypt and Save Backup",
            "4. Record Backups",
            "5. Display Latest Backups",
            "6. Select a Backup to Restore",
            "7. Exit"})
    );

    switch (choice)
    {
        case "1. List Private Repositories":
            await githubConnection.ShowPrivateRepositories();
            break;
        case "2. Select a Repository to Download Issues":
            (issueList, repository) = await githubConnection.ShowIssues();
            break;
        case "3. Encrypt and Save Backup":
            {
                if (issueList.Count > 0)
                {
                    byte[] encrypted = EncryptDecrypt.EncryptIssueToBytes_Aes(issueList, myAes.Key, myAes.IV);

                    //Write to file
                    var fileName = $"{repository}-{currentDate.ToString("yyyy-dd-M--HH-mm-ss")}.bytes";

                    File.WriteAllBytes(Path.Combine(workingDirectory, fileName), encrypted);
                    Console.WriteLine("Write to file completed successfully");
                }
                else
                {
                    Console.WriteLine("\nYou should first list repositories and select one of them");
                }

                break;
            }
        case "4. Record Backups":
            if (repository != string.Empty)
                database.SaveBackup(repository, currentDate);
            else
                Console.WriteLine("\nYou should first list repositories and select one of them");
            break;
        case "5. Display Latest Backups":
            database.ShowBackups();
            break;
        case "6. Select a Backup to Restore":
            {
                var selectedBackup = string.Empty;
                do
                {
                    Console.WriteLine("Select a backup:");
                    selectedBackup = Console.ReadLine();
                }
                while (string.IsNullOrEmpty(selectedBackup));

                var date = database.GetMaxDateFromRepository(selectedBackup);

                var backupPath = Path.Combine(workingDirectory, $"{selectedBackup}-{DateTime.Parse(date).ToString("yyyy-dd-M--HH-mm-ss")}.bytes");

                await githubConnection.CreateRepositoryFromBackup(backupPath, myAes, date, selectedBackup);
                break;
            }

        case "7. Exit":
            AnsiConsole.Markup("Goodbye!");
            return 0;
        default:
            AnsiConsole.MarkupLine("[red]Invalid choice[/]");
            break;
    }
}






