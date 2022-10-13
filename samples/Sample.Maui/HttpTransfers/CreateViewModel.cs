﻿using Shiny;
using Shiny.Net.Http;

namespace Sample.HttpTransfers;


public class CreateViewModel : ViewModel
{
    const string RANDOM_FILE_NAME = "upload.random";
    IDisposable? sub;


    public CreateViewModel(
        BaseServices services,
        IFilePicker filePicker,
        IHttpTransferManager manager
    ) : base(services)
    {

        this.SelectUpload = new Command(async () =>
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select a file to upload"
            });
            if (result != null)
                this.FilePath = result.FullPath;
        });

        this.Save = this.LoadingCommand(async () =>
        {
            this.ErrorMessage = "";
            if (this.FilePath.IsEmpty())
            {
                this.ErrorMessage = "Enter a filename";
                return;
            }
            if (!Uri.TryCreate(this.Url, UriKind.Absolute, out var uri))
            {
                this.ErrorMessage = "Please enter a valid URI";
                return;
            }
            if (this.IsUpload && !File.Exists(this.FilePath))
            {
                await this.Alert("This file does not exist");
                return;
            }

            var verb = this.HttpVerb.ToLower() switch
            {
                "post" => HttpMethod.Post,
                "get" => HttpMethod.Get,
                "put" => HttpMethod.Put,
                _ => null
            };
            if (verb == null)
            {
                await this.Alert("Invalid HTTP Verb - " + this.HttpVerb);
                return;
            }
            var request = new HttpTransferRequest(this.Url, this.FilePath, this.IsUpload)
            {
                UseMeteredConnection = this.UseMeteredConnection,
                PostData = this.PostData,
                HttpMethod = verb
            };
            //await httpTransfers.Enqueue(request);

            await this.Navigation.GoBack();
        });

        this.CreateRandom = this.LoadingCommand(async () =>
        {
            if (this.SizeInMegabytes <= 0)
            {
                await this.Alert("Invalid File Size");
                return;
            }
            await this.GenerateRandom();
            this.FilePath = this.GetRandomFilePath();
        });
    }


    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        this.sub = this.WhenAnyProperty(x => x.IsUpload).Subscribe(upload =>
        {
            if (upload)
            {
                this.Title = "New Upload";
                this.HttpVerb = "POST";
                var path = this.GetRandomFilePath();
                if (File.Exists(path))
                    this.FilePath = path;
            }
            else
            {
                this.Title = "New Download";
                this.HttpVerb = "GET";
                this.FilePath = Path.Combine(this.Platform.AppData.FullName, Guid.NewGuid().ToString() + ".download");
            }
        });
    }


    public ICommand Save { get; }
    public ICommand SelectUpload { get; }
    public ICommand Delete { get; }
    public ICommand CreateRandom { get; }


    [Reactive] public string PostData { get; set; }
    [Reactive] public string HttpVerb { get; set; }
    [Reactive] public string Title { get; set; }
    [Reactive] public string ErrorMessage { get; private set; }
    [Reactive] public string Url { get; set; }
    [Reactive] public bool UseMeteredConnection { get; set; }
    [Reactive] public bool IsUpload { get; set; }
    [Reactive] public string FilePath { get; set; }
    [Reactive] public int SizeInMegabytes { get; set; }


    string GetRandomFilePath() => Path.Combine(this.Platform.AppData.FullName, RANDOM_FILE_NAME);
    Task GenerateRandom() => Task.Run(() =>
    {
        var path = this.GetRandomFilePath();
        if (File.Exists(path))
            File.Delete(path); // delete previous random file

        var byteSize = this.SizeInMegabytes * 1024 * 1024;
        var data = new byte[8192];
        var rng = new Random();

        using var fs = new FileStream(path, FileMode.Create);
        while (fs.Length < byteSize)
        {
            rng.NextBytes(data);
            fs.Write(data, 0, data.Length);
            fs.Flush();
        }
    });
}