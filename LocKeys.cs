namespace SPYM;

public static class LocKeys {
    public const string Root = $"Mods.{nameof(SPYM)}";
    public const string Configs = $"{Root}.{nameof(Configs)}";
    public const string Chat = $"{Root}.{nameof(Chat)}";
    public const string Items = $"{Root}.{nameof(Items)}";
    public const string Buffs = $"{Root}.{nameof(Buffs)}";
    public const string RecipesGroups = $"{Root}.{nameof(RecipesGroups)}";
    public const string InfoDisplays = $"{Root}.{nameof(InfoDisplays)}";

    public const string ServerConfig = $"{Configs}.{nameof(SPYM.Configs.ServerConfig)}";
    public const string ClientConfig = $"{Configs}.{nameof(SPYM.Configs.ClientConfig)}";
}