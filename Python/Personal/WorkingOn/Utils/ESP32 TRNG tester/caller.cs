using System.Diagnostics;

string result;
var psi = new ProcessStartInfo
{
    FileName = "python",
    Arguments = "trng_pipeline.py",
    RedirectStandardOutput = true,
    UseShellExecute = false,
    CreateNoWindow = true
};
using (var process = Process.Start(psi))
{
    result = process.StandardOutput.ReadToEnd();
}
Console.WriteLine(result);  // contiene anche il Base64 da decodificare
