using System.Text.Json.Serialization;

namespace BuhtaApp.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus { Open, Closed, Cancelled }