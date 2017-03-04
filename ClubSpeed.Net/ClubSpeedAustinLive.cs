using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace ClubSpeed.Net
{
    public class ClubSpeedAustinLive
    {
        private string _clientId;
        private int _messageId = 0;
        private HttpClient _client;
        private CancellationTokenSource _tokenSource;

        public delegate void OnUpdateHandler(LiveRaceInfo raceInfo);
        public event OnUpdateHandler OnUpdate;

        public ClubSpeedAustinLive()
        {
            _client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false });
        }

        public void StartPolling()
        {
            _tokenSource = new CancellationTokenSource();
            Negotiate();
        }

        public void StopPolling()
        {
            _tokenSource.Cancel();
        }

        private void OnPollResponse(HttpResponseMessage response)
        {
            if (IsServerError(response))
                throw new Exception("Poll failed with a server error.");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Poll failed with status code " + response.StatusCode);

            string resposeContent = response.Content.ReadAsStringAsync().Result;
            SignalRResult signalRResult = JsonConvert.DeserializeObject<SignalRResult>(resposeContent);

            foreach (Message msg in signalRResult.Messages)
            {
                foreach (LiveRaceInfo raceInfo in msg.Args) //Pretty sure this always has 1 entry but just in case..
                {
                    if (OnUpdate != null)
                        OnUpdate(raceInfo);
                }
            }

            _messageId = signalRResult.MessageId;

            if(!_tokenSource.IsCancellationRequested)
                Poll("https://k1austin.clubspeedtiming.com/sp_center/signalr");
        }

        private void OnNegotiateResponse(HttpResponseMessage response)
        {
            if (IsServerError(response))
                throw new Exception("Negotiation failed with a server error.");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Negotiation failed with status code " + response.StatusCode);

            string resposeContent = response.Content.ReadAsStringAsync().Result;
            NegotiationResponse negoResponse = JsonConvert.DeserializeObject<NegotiationResponse>(resposeContent);

            _clientId = negoResponse.ClientId;

            Poll("https://k1austin.clubspeedtiming.com/sp_center/signalr/connect");
        }

        private void Negotiate()
        {
            _client.GetAsync("https://k1austin.clubspeedtiming.com/sp_center/signalr/negotiate").ContinueWith((responseTask) => OnNegotiateResponse(responseTask.Result));
        }

        private void Poll(string url)
        {
            Dictionary<string, string> postParams = new Dictionary<string, string>()
            {
                { "clientId", _clientId },
                { "messageId", (_messageId > 0) ? _messageId.ToString() : "null" },
                { "connectionData",  "[{\"name\":\"SP_Center.ScoreBoardHub\",\"methods\":[\"refreshGrid\"]}]"}, //This can be ignored as I'm not actually connecting to the hub using SignalR
                { "transport", "longPolling" },
                { "groups" , "SP_Center.ScoreBoardHub.1" }
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(postParams);
            _client.PostAsync(url, content).ContinueWith((responseTask) => OnPollResponse(responseTask.Result));
        }

        private bool IsServerError(HttpResponseMessage response)
        {
            return response.Headers.Location != null &&
                   response.Headers.Location.ToString().Contains("https://k1austin.clubspeedtiming.com/sp_center/ServerError.html");
        }
    }

    public class NegotiationResponse
    {
        public string Url { get; set; }
        public string ClientId { get; set; }
    }

    public class Winner
    {
        public string CustImage { get; set; }
        public string KartNo { get; set; }
        public string RacerName { get; set; }
        public string BestLap { get; set; }
        public string Laps { get; set; }
        public string TrackRecord { get; set; }
    }

    public class Racer
    {
        public string CustID { get; set; }
        public string HeatNo { get; set; }
        public string RacerName { get; set; }
        public string AutoNo { get; set; }
        public string AMBTime { get; set; }
        public string LTime { get; set; }
        public string LapNum { get; set; }
        public string BestLTime { get; set; }
        public string Position { get; set; }
        public string GapToLeader { get; set; }
        public object HeatRanking { get; set; }
        public string LastPassedTime { get; set; }
        public string DlTime { get; set; }
        public string DBestLTime { get; set; }
        public string TimeSinceLastPassed { get; set; }
        public string PenaltyFlags { get; set; }
    }

    public class LiveRaceInfo
    {
        public string Winby { get; set; }
        public string LapsLeft { get; set; }
        public string HeatTypeName { get; set; }
        public List<Winner> Winners { get; set; }
        public List<Racer> ScoreboardData { get; set; }
        public bool RaceRunning { get; set; }
    }

    public class Message
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public List<LiveRaceInfo> Args { get; set; }
    }

    public class TransportData
    {
        public List<string> Groups { get; set; }
        public int LongPollDelay { get; set; }
    }

    public class SignalRResult
    {
        public int MessageId { get; set; }
        public List<Message> Messages { get; set; }
        public TransportData TransportData { get; set; }
    }
}
