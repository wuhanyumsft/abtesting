using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace abtesting
{
    class Program
    {
        static void Main(string[] args)
        {
            int treatmentCount = 0, controlCount = 0;
            int totalCount = 10000;
            object lockObject = new object();

            var requests = Enumerable.Range(0, totalCount).ToArray();
            int processedCount = 0;
            var uri = new Uri("https://docs.microsoft.com/ja-jp/microsoft-edge/devtools-guide");
            Parallel.ForEach(requests, new ParallelOptions { MaxDegreeOfParallelism = 32 }, (request) =>
            {
                CookieContainer cookies = new CookieContainer();
                HttpClient client = new HttpClient(new HttpClientHandler
                {
                    CookieContainer = cookies
                });
                using (HttpResponseMessage response = client.GetAsync(uri).Result)
                {
                    response.EnsureSuccessStatusCode();
                    var resultStr = response.Content.ReadAsStringAsync().Result;
                    lock (lockObject)
                    {
                        if (resultStr.Contains(@"<meta name=""experimental"" content=""false"" />"))
                        {
                            treatmentCount++;
                        }
                        if (resultStr.Contains(@"<meta name=""experimental"" content=""true"" />"))
                        {
                            controlCount++;
                        }

                        var responseCookies = cookies.GetCookies(uri);
                        var assignmentTextBase64 = HttpUtility.UrlDecode(responseCookies["TasTreatmentAssignment"].Value);
                        var assignmentText = Encoding.UTF8.GetString(Convert.FromBase64String(assignmentTextBase64));
                        processedCount++;
                        Console.WriteLine($"[{processedCount}] - control {controlCount} - treatment {treatmentCount} - {assignmentText}");
                    }
                }
            });

            Console.WriteLine($"{treatmentCount} / {totalCount} = {(double)treatmentCount / totalCount}.");
            Console.ReadKey();
        }
    }

    public class TreatmentResult
    {
        public string[] features;
    }
}
