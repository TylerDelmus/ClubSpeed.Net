using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClubSpeed.Net
{
    public class ClubSpeedAustinLive
    {
        private const int POLL_INTERVAL = 15000;
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
            if (string.IsNullOrEmpty(_clientId))
            {
                _clientId = Negotiate().ClientId;
                SignalRResult result = GetSignalRResult("https://k1austin.clubspeedtiming.com/SP_Center/signalr/connect");
                _messageId = result.MessageId;
            }

            _tokenSource = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    Poll();
                    await Task.Delay(POLL_INTERVAL, _tokenSource.Token);
                }
            }, _tokenSource.Token);
        }

        public void StopPolling()
        {
            _tokenSource.Cancel();
        }

        private void Poll()
        {
            SignalRResult result = GetSignalRResult("https://k1austin.clubspeedtiming.com/SP_Center/signalr");

            foreach(Message msg in result.Messages)
            {
                foreach (LiveRaceInfo raceInfo in msg.Args) //Pretty sure this always has 1 entry but just in case..
                {
                    if (OnUpdate != null)
                        OnUpdate(raceInfo);
                }
            }

            _messageId = result.MessageId;
        }

        private NegotiationResponse Negotiate()
        {
            HttpResponseMessage response = _client.GetAsync("https://k1austin.clubspeedtiming.com/sp_center/signalr/negotiate").Result;

            if (response.Headers.Location != null && 
                response.Headers.Location.ToString().Contains("https://k1austin.clubspeedtiming.com/sp_center/ServerError.html"))
                throw new Exception("Negotiation failed with a server error.");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Negotiation failed with status code " + response.StatusCode);

            string content = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<NegotiationResponse>(content);
        }

        private SignalRResult GetSignalRResult(string url)
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
            HttpResponseMessage response = _client.PostAsync(url, content).Result;

            if (response.Headers.Location != null &&
                response.Headers.Location.ToString().Contains("https://k1austin.clubspeedtiming.com/sp_center/ServerError.html"))
                throw new Exception("Poll failed with a server error.");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Poll failed with status code " + response.StatusCode);

            string resposecontent = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<SignalRResult>(resposecontent);
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

    public class ScoreboardData
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
        public List<ScoreboardData> ScoreboardData { get; set; }
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
