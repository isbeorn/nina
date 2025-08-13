using Newtonsoft.Json;

namespace NINA.Equipment.Equipment.MyGuider.SkyGuard.SkyGuardMessages 
{
    /// <summary>
    /// Inclure l'url de la doc SkyGuard
    /// </summary>
  class SkyGuardStatusMessage
  {
    [JsonProperty(PropertyName = "status")]
    public string Status { get; set; }

    [JsonProperty(PropertyName = "data")]
    public string Data { get; set; }

    [JsonProperty(PropertyName = "message")]
    public object Message { get; set; }
  }
}
