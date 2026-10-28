using System.Text.Json.Serialization;

namespace BuhtaApp.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShiftStatus { Open, Closed }