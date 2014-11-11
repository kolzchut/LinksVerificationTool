using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Collections;
using System.Configuration;

namespace KolZchutLinksVerification
{
    class Program
    {
        class Attempt : IComparable
        {
            public int Count;
            public DateTime Schedule;

            // compare Attempts by their schedule
            public int CompareTo(object obj)
            {
                return Schedule.CompareTo((obj as Attempt).Schedule);
            }
        };

        class Result
        {
            public bool Suceeded;
            public string Status;
        }

        static Dictionary<String, Result> processedURLs = new Dictionary<string, Result>();
        static Dictionary<String, Attempt> attempts = new Dictionary<string, Attempt>();
        static StreamReader inputFile = null;
        static StreamWriter errosFile = null;
        static StreamWriter logFile = null;
        static int secondsBetweenChecks = 10;

        const char SeperatorCharacter = ',';

        static void Main(string[] args)
        {
            ProcessCommandLine(args);

            while (!inputFile.EndOfStream || attempts.Count > 0)
            {
                string line = null;
                bool shouldWait = true;

                line = GetNextLine();

                if (line != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;

                    string pageURL = GetPageURL(line);

                    if (pageURL == null)
                        continue;

                    LogAttempt(line, pageURL);

                    string status;
                    bool succeed = VerifyURLIfNeeded(ref shouldWait, pageURL, out status);

                    if (succeed)
                    {
                        ProcessSuccess(line, pageURL);
                    }
                    else
                    {
                        ProcessFailure(line, pageURL, status);
                    }
                }

                errosFile.Flush();

                if (shouldWait)
                    System.Threading.Thread.Sleep(secondsBetweenChecks * 1000);
            }

            inputFile.Close();
            errosFile.Close();
            logFile.Close();
        }

        private static void ProcessCommandLine(string[] args)
        {
            var inputFilename = args[0];


			var errorsFilename = System.IO.Path.GetFileNameWithoutExtension(inputFilename) + "_ERRORS" + System.IO.Path.GetExtension(inputFilename);
			if (args.Count () > 1) {
				errorsFilename = args [1];
			}

			string logFilename = args.Count() > 2 ? args[2] : System.IO.Path.GetFileNameWithoutExtension(inputFilename) + ".log";

            if (args.Count() > 3)
                int.TryParse(args[3], out secondsBetweenChecks);

            inputFile = new StreamReader(inputFilename, System.Text.Encoding.Default, true);
            errosFile = new StreamWriter(errorsFilename, false, inputFile.CurrentEncoding, 1);

            if (logFilename != null)
                logFile = new StreamWriter(logFilename, false, inputFile.CurrentEncoding, 1);
        }

        private static string GetNextLine()
        {
            string line = null;

            if (attempts.Count > 0)
            {
                var min = attempts.First(m => m.Value == attempts.Values.Min());
                if (min.Value.Schedule < DateTime.Now)
                {
                    line = min.Key;
                }
            }

            while (line == null && !inputFile.EndOfStream)
            {
                line = inputFile.ReadLine().TrimEnd(new char[] { ' ', SeperatorCharacter });
            }
            return line;
        }

        private static string GetPageURL(string line)
        {
            string pageURL = null;

            try
            {
                pageURL = GetURLFromLine(line);
            }
            catch
            {
                errosFile.WriteLine(line + @", ERROR!");
            }

            return pageURL;
        }

        private static void LogAttempt(string line, string pageURL)
        {
            if (attempts.ContainsKey(line))
            {
                attempts[line].Count++;
                WriteToConsole(String.Format("{0} attempt at: {1}", CountDesignation(attempts[line].Count), pageURL));
            }
            else
                WriteToConsole(pageURL);
        }

        private static bool VerifyURLIfNeeded(ref bool shouldWait, string pageURL, out string status)
        {
            bool succeed;
            Result result;
            if (processedURLs.TryGetValue(pageURL, out result))
            {
                succeed = result.Suceeded;
                status = result.Status;

                if (succeed)
                    WriteSuccessToConsole("Success (duplicate)");
                else
                    WriteErrorToConsole("Failed (duplicate)");

                shouldWait = false;
            }
            else
            {
                succeed = VerifyURL(pageURL, out status);
                if (succeed)
                    WriteSuccessToConsole("Success");
            }
            return succeed;
        }

        private static void ProcessSuccess(string line, string pageURL)
        {
            if (attempts.ContainsKey(line))
                attempts.Remove(line);

            processedURLs[pageURL] = new Result() { Suceeded = true }; ; // true means it was processed and identified as a valid URL
        }

        private static void ProcessFailure(string line, string pageURL, string status)
        {
            if (processedURLs.ContainsKey(pageURL)) // failed duplicate, no need to run more attempts
            {
                if (attempts.ContainsKey(line))
                    attempts.Remove(line);

                WriteErrorLineToOutputFile(line, status);
            }
            else if (!attempts.ContainsKey(line)) // This was the first attempt
            {
                attempts.Add(line, new Attempt()
                {
                    Count = 1,
                    Schedule = DateTime.Now + new TimeSpan(0, 1, 0) // check again in 1 minute
                });

                WriteErrorToConsole("First failure - try again in 1 minute");
            }
            else // This was not the first attempt
            {
                if (attempts[line].Count == 2)
                {
                    attempts[line].Schedule = DateTime.Now + new TimeSpan(0, 10, 0); // check again in 10 minutes
                    WriteErrorToConsole("Second failure - try again in 10 minutes");
                }
                else if (attempts[line].Count == 3)
                {
                    attempts[line].Schedule = DateTime.Now + new TimeSpan(1, 0, 0); // check again in 1 hour
                    WriteErrorToConsole("Third failure - try again in 1 hour");
                }
                else // attempts[line].Count == 4
                {
                    WriteErrorToConsole(String.Format("{0} failure - give up!", CountDesignation(attempts[line].Count)));

                    attempts.Remove(line);
                    processedURLs[pageURL] = new Result() { Suceeded = false, Status = status }; // false means it was processed and identified as a invalid URL

                    WriteErrorLineToOutputFile(line, status);
                }
            }
        }

        private static void WriteErrorLineToOutputFile(string line, string status)
        {
            line += String.Format("{0}{1}", SeperatorCharacter, status);
            errosFile.WriteLine(line);
        }

        private static string GetURLFromLine(string line)
        {
            string pageURL = null;

            bool skipVerification = false;

            var columns = line.Split(new char[] { SeperatorCharacter });

            for (int i = 0; i < columns.Length; i++)
            {
                var text = columns[i];

                while (text.StartsWith("\"") && !text.EndsWith("\""))
                    text += SeperatorCharacter + columns[++i];

                text = text.Trim(new char[] { '\"' });

                if (text.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase) ||
                    text.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))
                {
                    pageURL = text;
                    break;
                }

                if (text.StartsWith("mailto:", StringComparison.InvariantCultureIgnoreCase))
                {
                    skipVerification = true;
                    break;
                }
            }

            if (String.IsNullOrWhiteSpace(pageURL) && !skipVerification)
                throw (new Exception());

            return pageURL;
        }


		private static bool isRedirect(int statusCode) {
			return (statusCode == 301 || statusCode == 302) ? true : false;
		}


		/*
		 * @todo Make this actually check for valid cases of redirect (301/302):
		 * Change of protocol (http<->https) with same url
		 * Same URL but with/without "/" (kolz.org.il <-> kolz.org.il/ )
		 * Any redirect from the root of a site to an internal page (kolz.org.il -> kolz.org.il/Pages/default.aspx)
		 * Maybe: with/without subdomain "www" (ahva.org.il <-> www.ahva.org.il)
		 */
		private static bool isValidRedirect(string url, HttpWebResponse response)
		{
			return false;
		}


        private static bool VerifyURL(string url, out string status)
        {
            status = String.Empty;
            try
            {
                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.CookieContainer = new CookieContainer(300, 100, 64000);
                request.Timeout = 60000; //set the timeout to 5 seconds to keep the user from waiting too long for the page to load
                request.MaximumAutomaticRedirections = 100;
                request.MaximumResponseHeadersLength = 256000;
                bool allowAutoredirect = false;
                request.AllowAutoRedirect = (bool.TryParse(ConfigurationManager.AppSettings["AllowAutoRedirect"], out allowAutoredirect) ? allowAutoredirect : false);
                request.Method = "GET"; //Get only the header information -- no need to download any content
                request.Accept = "*/*";
                //request.UserAgent = "KolZchutLinksVerification";
                request.UserAgent = @"Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/6.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.3; .NET4.0C; .NET4.0E)";

                using (var response = request.GetResponse() as HttpWebResponse)
                {

                    status = String.Format("{0} ({1})", response.StatusCode.ToString(), (int)response.StatusCode);
                    int statusCode = (int)response.StatusCode;
                    if (statusCode >= 100 && statusCode < 300) //Good requests
                    {
                        return true;
                    }
                    else 
                    {
                        WriteErrorToConsole(String.Format("Status [{0}] returned for url: {1}", statusCode, url));
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError) //400 errors
                {
                    var webResponse = ex.Response as System.Net.HttpWebResponse;
                    if (webResponse != null)
                        status = String.Format("{0} ({1})", webResponse.StatusCode.ToString(), (int)webResponse.StatusCode);
                    else
                        status = String.Format("{0} ({1})", ex.Message, ex.Status);

                    WriteErrorToConsole(String.Format("Status [{0}] returned for url: {1}", ex.Status, url), ex);
                }
                else
                {
                    status = String.Format("{0} ({1})", ex.Message, ex.Status);

                    WriteErrorToConsole(String.Format("Unhandled status [{0}] returned for url: {1}", ex.Status, url), ex);
                }
            }
            catch (Exception ex)
            {
                status = ex.Message;

                WriteErrorToConsole(String.Format("Could not test url {0}.", url), ex);
            }

            return false;
        }

        // Logging
        private static string CountDesignation(int count)
        {
            switch (count)
            {
                case 1:
                    return "First";
                case 2:
                    return "Second";
                case 3:
                    return "Third";
                case 4:
                    return "Forth";
                default:
                    return String.Format("{0}th", count);
            }
        }

        private static void LogMessage(string message)
        {
            if (logFile != null)
            {
                logFile.WriteLine(String.Format("{0} : {1}", DateTime.Now.ToString(), message));
                logFile.Flush();
            }
        }

        private static void WriteErrorToConsole(string error, Exception ex = null)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            LogMessage(error);
            if (ex != null)
                LogMessage(ex.ToString());
            Console.ForegroundColor = color;
        }

        private static void WriteSuccessToConsole(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            LogMessage(message);
            Console.ForegroundColor = color;
        }

        private static void WriteToConsole(string message)
        {
            Console.WriteLine(message);
            LogMessage(message);
        }
    }
}
