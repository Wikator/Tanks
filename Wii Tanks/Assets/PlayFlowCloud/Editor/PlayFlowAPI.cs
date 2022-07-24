using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;


public class PlayFlowAPI
{
    private static string API_URL = "https://api-playflowcloud.qp3qljphuhlk6.us-east-1.cs.amazonlightsail.com/";
    private static string version = "2";
    
    private static void addHeadersToClient(WebClient client, Dictionary<string, string> headers)
    {
        client.Headers.Add("version", version);
        foreach (KeyValuePair<string, string> pair in headers)
        {
            client.Headers.Add(pair.Key, pair.Value);
        }
    }
    
    public class PlayFlowWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = 10000000;
            return request;
        }
    }

    public static string Upload(string fileLocation, string token,string region)
    {
        string actionUrl = API_URL + "upload_server_files";

        byte[] responseArray;
        using (PlayFlowWebClient client = new PlayFlowWebClient())
        {
            client.Headers.Add("token", token);
            client.Headers.Add("region", region);
            client.Headers.Add("version", version);

            responseArray = client.UploadFile(actionUrl, fileLocation);
        }

        return ((System.Text.Encoding.ASCII.GetString(responseArray)));
    }

    public static async Task<string> StartServer(string token,string region, string arguments, string ssl)
     {

         string actionUrl = API_URL + "start_game_server";

         using (var client = new HttpClient())
         using (var formData = new MultipartFormDataContent())
         {
             formData.Headers.Add("token", token);
             formData.Headers.Add("region", region);
             formData.Headers.Add("version", version);
             formData.Headers.Add("arguments", arguments);
             formData.Headers.Add("ssl", ssl);


             var response = await client.PostAsync(actionUrl, formData);
             
             if (!response.IsSuccessStatusCode)
             {
                 Debug.Log( await response.Content.ReadAsStringAsync());
             }
             
             return await response.Content.ReadAsStringAsync();;
         }
     }
    
    public static async Task<string> RestartServer(string token,string region, string arguments, string ssl, string match)
    {

        string actionUrl = API_URL + "restart_game_server";
        
        match = ssl_cleanup(match);


        using (var client = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        {
            formData.Headers.Add("token", token);
            formData.Headers.Add("region", region);
            formData.Headers.Add("version", version);
            formData.Headers.Add("arguments", arguments);
            formData.Headers.Add("ssl", ssl);
            formData.Headers.Add("serverid", match);

            var response = await client.PostAsync(actionUrl, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log(System.Text.Encoding.UTF8.GetString( await response.Content.ReadAsByteArrayAsync()));
            }
            return System.Text.Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        }
    }
    
    public static async Task<string> StopServer(string token,string region, string match)
    {

        string actionUrl = API_URL + "stop_game_server";
        
        match = ssl_cleanup(match);


        using (var client = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        {
            formData.Headers.Add("token", token);
            formData.Headers.Add("region", region);
            formData.Headers.Add("version", version);
            formData.Headers.Add("serverid", match);

            var response = await client.PostAsync(actionUrl, formData);
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log(System.Text.Encoding.UTF8.GetString( await response.Content.ReadAsByteArrayAsync()));
            }
            
            return System.Text.Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        }
    }
    
    public static async Task<string> GetServerLogs(string token,string region, string match)
    {
        string actionUrl = API_URL + "get_server_logs";

        match = ssl_cleanup(match);

        using (var client = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        {
            formData.Headers.Add("token", token);
            formData.Headers.Add("region", region);
            formData.Headers.Add("version", version);
            formData.Headers.Add("serverid", match);

            var response = await client.PostAsync(actionUrl, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log(await response.Content.ReadAsStringAsync());
            }
            return await response.Content.ReadAsStringAsync();
        }
    }
    
    
    public static async Task<string> GetActiveServers(string token,string region)
    {

        string actionUrl = API_URL + "list_servers";

        using (var client = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        {
            formData.Headers.Add("token", token);
            formData.Headers.Add("region", region);
            formData.Headers.Add("version", version);
            
            var response = await client.PostAsync(actionUrl, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                Debug.Log(System.Text.Encoding.UTF8.GetString( await response.Content.ReadAsByteArrayAsync()));
            }
            
            return System.Text.Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
        }
    }

    public static string ssl_cleanup(string input)
    {
        if (input.Contains("SSL"))
        {
            string[] strarray = input.Split();
            return strarray[0];
        }

        return input;
    }
}
