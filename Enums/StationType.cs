using System.Text.Json.Serialization;

namespace BuhtaApp.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StationType { Bar, Kitchen }