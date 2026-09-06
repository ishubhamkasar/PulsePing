using System.Net;
using System.Security.Cryptography;
using PulsePingNative;

internal static class Program
{
 static async Task Main(string[] args)
 {
  string root = Path.Combine(Path.GetTempPath(), "PulsePing-download-tests-" + Guid.NewGuid().ToString("N"));
  byte[] bytes = new byte[200000]; Random.Shared.NextBytes(bytes);
  string digest = "sha256:" + Convert.ToHexString(SHA256.HashData(bytes));
  GitHubReleaseAsset Asset(string? hash = null, long? length = null, string? url = null) => new()
  { Name = "PulsePing-v9.0.0-win-x64.exe", Digest = hash ?? digest, Size = length ?? bytes.Length,
    DownloadUrl = url ?? UpdateChecker.ReleasePrefix + "download/v9.0.0/PulsePing-v9.0.0-win-x64.exe" };
  UpdateCheckResult Release(GitHubReleaseAsset asset) => new(new Version(1,0,0,0), new Version(9,0,0,0), "9.0.0", new Uri(UpdateChecker.ReleasePrefix+"tag/v9.0.0"), asset);
  using var client = new HttpClient(new ResponseHandler(bytes));
  var progress = new InlineProgress();
  var downloaded = await UpdateChecker.DownloadAsync(Release(Asset()), progress, CancellationToken.None, client, root);
  if (!File.ReadAllBytes(downloaded.Path).SequenceEqual(bytes) || progress.Last?.Percent != 100) throw new Exception("Successful download/progress failed");
  Console.WriteLine("PASS successful download and 100% progress");
  await Fails("wrong checksum", Release(Asset("sha256:" + new string('0',64))), CancellationToken.None);
  await Fails("missing checksum", Release(Asset("")), CancellationToken.None);
  await Fails("size mismatch", Release(Asset(length:bytes.Length+1)), CancellationToken.None);
  await Fails("untrusted URL", Release(Asset(url:"https://example.com/update.exe")), CancellationToken.None);
  using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
  await Fails("cancelled download", Release(Asset()), cancellation.Token);
  if (Directory.GetFiles(root,"*.partial",SearchOption.AllDirectories).Length != 0) throw new Exception("Partial downloads not cleaned up");
  if(UpdateChecker.IsOfficialUrl("https://github.com.evil.example/ishubhamkasar/PulsePing/releases/download/a", "download/")) throw new Exception("Spoofed host accepted");
  var parsed = UpdateChecker.ParseRelease(new() {TagName="v9.0.0",HtmlUrl=UpdateChecker.ReleasePrefix+"tag/v9.0.0",Assets=[Asset()]});
  if(parsed.Asset is null || !parsed.IsUpdateAvailable) throw new Exception("Release parsing failed");
  var duplicate = UpdateChecker.ParseRelease(new() {TagName="v9.0.0",HtmlUrl=UpdateChecker.ReleasePrefix+"tag/v9.0.0",Assets=[Asset(),Asset()]});
  if(duplicate.Asset is not null) throw new Exception("Ambiguous release accepted");
  Console.WriteLine("PASS official URL validation, asset selection, version parsing and cleanup");
  if(args.Contains("--live"))
  {
   var live=await UpdateChecker.CheckAsync(CancellationToken.None);
   using var liveClient=UpdateChecker.CreateClient();
   var actual=await UpdateChecker.DownloadAsync(live,new InlineProgress(),CancellationToken.None,liveClient,root);
   Console.WriteLine($"PASS actual GitHub release {live.LatestVersionText} checksum: {actual.Sha256}");
  }
  Console.WriteLine("Test files: "+root);
  async Task Fails(string label, UpdateCheckResult release, CancellationToken token)
  {
   try { await UpdateChecker.DownloadAsync(release,new InlineProgress(),token,client,root); }
   catch(Exception ex) when(ex is InvalidDataException or OperationCanceledException) {Console.WriteLine("PASS "+label);return;}
   throw new Exception("Expected rejection: "+label);
  }
 }
 sealed class ResponseHandler(byte[] bytes) : HttpMessageHandler
 {
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
  {token.ThrowIfCancellationRequested();return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)});}
 }
 sealed class InlineProgress : IProgress<DownloadProgress>
 { public DownloadProgress? Last; public void Report(DownloadProgress value)=>Last=value; }
}
