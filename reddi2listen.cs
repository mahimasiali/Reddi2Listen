 // 
 // Example Resource: https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/quickstarts/csharp
// pick up point: log file data into a list
// Try interpreting sentences you've logged in
// Take the webscraper data 

// Not sure how: https://docs.microsoft.com/bs-latn-ba/azure/cognitive-services/text-analytics/how-tos/text-analytics-how-to-keyword-extraction

/* Steps followed
    1) Setup link with Microsoft Azure (Sentiment Analysis library)
    2) Check Sentiment analysis with manually entered sentences
    3) Read a text file with sentences
    4) Next: Automate object creation
    5) Next: Keyword extraction
    6) Sort the list: shorten it, only keep the sad sentiments
 */

 /*
    Note: no asessment on how serious - HOW happy/sad it
 
  */

// 1. Add using statements
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Rest;

// For file handling
using static System.Console;
using System.IO;

namespace Analysis
{
    public static class Globals
    {
        public static List<MultiLanguageInput> sadList = new List<MultiLanguageInput>();
        public static List<string> phraseList = new List<string>();
    }
    class Program
    {
        // 2. Constant variables. Paste Subscription Key and Endpoint from Azure
        private const string SubscriptionKey = "fd33394fa0c6481785aaf46fb6262280";
        private const string Endpoint = "https://eastus.api.cognitive.microsoft.com/";
        //List <string> fileLines = new List <string>();

        // Create a Text Analytics Client
        static void Main(string[] args)
        {
            var credentials = new ApiKeyServiceClientCredentials(SubscriptionKey);
            var client = new TextAnalyticsClient(credentials)
            {
                Endpoint = Endpoint
            };
            // Change console encoding to display non ASCII
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            // Call Cognitive Services Functions
            SentimentAnalysisExample(client).Wait();
            KeyPhraseExtractionExample(client).Wait();
            Console.ReadLine();

            // Instantiate out here
            //fileLines = new List <string> () ;
        }

        static List<string> ReadAndDisplay(string path)
        {
            FileMode mode = FileMode.Open;
            FileAccess access = FileAccess.Read;
            List<string> fileRead = new List <string> () ;
            using ( FileStream file = new FileStream (path, mode, access))
			{
				using ( StreamReader reader = new StreamReader (file))
				{
					while(! reader.EndOfStream)
					{
						string line = reader.ReadLine();
						// Console.WriteLine(line);
                        fileRead.Add(line);
					}
				}
			}
            return fileRead;
        }

        public static void WriteData(string outPath)
        {
			FileMode mode = FileMode.Create;
			FileAccess access = FileAccess.Write;
			
			// creating instance for FileStream
			using (FileStream file = new FileStream(outPath , mode , access))
			{
				using (StreamWriter writer = new StreamWriter (file))
				{
                    for(int i=0; i<Globals.phraseList.Count; i++)
                    {
                        if(Globals.phraseList[i].ToLower()!=("x200b"))
                            writer.WriteLine(Globals.phraseList[i].ToLower());
                    } 
				}
			}
			
		}

// ------------------------------------ Cognitive Services ----------------------------------------//
        
        public static async Task SentimentAnalysisExample(TextAnalyticsClient client)
        {
            WriteLine("----------------------------Sentiment Analysis------------------------");
            // The documents to be analyzed. Add the language of the document (en). The ID can be any value.
            const string path = "sample.txt"; 
            List<string> inputLines = ReadAndDisplay(path);
            WriteLine("--------- Check to make sure the file was read properly and lines stored in list----------");
            for(int i=0; i<inputLines.Count; i++)
				WriteLine(inputLines[i]);

            // Create this list of Input objects to pass in
            List<MultiLanguageInput> myInputList = new List<MultiLanguageInput>();
            for(int i=0; i<inputLines.Count; i++)
            {
                int id = i+5;
                string idStr = id.ToString();
                myInputList.Add(
                    new MultiLanguageInput("en", idStr, inputLines[i]));
            }
            var inputDocuments = new MultiLanguageBatchInput(myInputList); // inputs (MultiLanguageBatchInput Objects)
            var result = await client.SentimentAsync(false, inputDocuments);

            // create a list with corresponding scores
            // then sort list using the lookup score

            List<double> scores = new List <double>();

            // Printing sentiment results
            foreach (var document in result.Documents)
            {
                Console.WriteLine($"Document ID: {document.Id} , Sentiment Score: {document.Score:0.00}");
                scores.Add((double)document.Score);
            }

            // Sort out sad
            //List<MultiLanguageInput> sadList = new List<MultiLanguageInput>();
           // List<MultiLanguageInput> sadList = new List<MultiLanguageInput>();
            for (int i=0; i<scores.Count; i++)
            {
                if(scores[i]<0.5)
                   Globals.sadList.Add(myInputList[i]);
            }
        }

        public static async Task KeyPhraseExtractionExample(TextAnalyticsClient client)
        {
            WriteLine("----------------------------Keyword Extraction------------------------");
            const string outPath = "new.txt";
            var inputDocuments = new MultiLanguageBatchInput(Globals.sadList);
            var kpResults = await client.KeyPhrasesAsync(false, inputDocuments);

            // Printing keyphrases
            foreach (var document in kpResults.Documents)
            {
                Console.WriteLine($"Document ID: {document.Id} ");
                Console.WriteLine("\t Key phrases:");
                foreach (string keyphrase in document.KeyPhrases)
                {
                    Console.WriteLine($"\t\t{keyphrase}");
                    Globals.phraseList.Add(keyphrase);
                }
            }
            WriteData(outPath);
    //...
        }

        
    
    }

    /// <summary>
/// Allows authentication to the API by using a basic apiKey mechanism
    class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        private readonly string subscriptionKey;
        /// Creates a new instance of the ApiKeyServiceClientCredentails class
        /// <param name="subscriptionKey">The subscription key to authenticate and authorize as</param>
        public ApiKeyServiceClientCredentials(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
        }

        /// Add the Basic Authentication Header to each outgoing request
        /// <param name="request">The outgoing request</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            request.Headers.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
