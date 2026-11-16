#pragma warning disable UAC1001, UAC1002, UAC1015
#if !DISABLE_PLAYFABCLIENT_API
using System;
using System.Collections.Generic;
using PlayFab.SharedModels;

namespace PlayFab.ClientModels
{

    [Serializable]
    public class AdCampaignAttributionModel : PlayFabBaseModel
    {
        /// <summary>
        /// UTC time stamp of attribution
        /// </summary>
        public DateTime AttributedAt;
        /// <summary>
        /// Attribution campaign identifier
        /// </summary>
        public string CampaignId;
        /// <summary>
        /// Attribution network name
        /// </summary>
        public string Platform;
    }

    [Serializable]
    public class AddOrUpdateContactEmailResult : PlayFabResultCommon
    {
    }

    /// <summary>
    /// A purchasable item from the item catalog
    /// </summary>
    [Serializable]
    public class CatalogItem : PlayFabBaseModel
    {
        /// <summary>
        /// defines the bundle properties for the item - bundles are items which contain other items, including random drop tables
        /// and virtual currencies
        /// </summary>
        public CatalogItemBundleInfo Bundle;
        /// <summary>
        /// if true, then an item instance of this type can be used to grant a character to a user.
        /// </summary>
        public bool CanBecomeCharacter;
        /// <summary>
        /// catalog version for this item
        /// </summary>
        public string CatalogVersion;
        /// <summary>
        /// defines the consumable properties (number of uses, timeout) for the item
        /// </summary>
        public CatalogItemConsumableInfo Consumable;
        /// <summary>
        /// defines the container properties for the item - what items it contains, including random drop tables and virtual
        /// currencies, and what item (if any) is required to open it via the UnlockContainerItem API
        /// </summary>
        public CatalogItemContainerInfo Container;
        /// <summary>
        /// game specific custom data
        /// </summary>
        public string CustomData;
        /// <summary>
        /// text description of item, to show in-game
        /// </summary>
        public string Description;
        /// <summary>
        /// text name for the item, to show in-game
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// If the item has IsLImitedEdition set to true, and this is the first time this ItemId has been defined as a limited
        /// edition item, this value determines the total number of instances to allocate for the title. Once this limit has been
        /// reached, no more instances of this ItemId can be created, and attempts to purchase or grant it will return a Result of
        /// false for that ItemId. If the item has already been defined to have a limited edition count, or if this value is less
        /// than zero, it will be ignored.
        /// </summary>
        public int InitialLimitedEditionCount;
        /// <summary>
        /// BETA: If true, then only a fixed number can ever be granted.
        /// </summary>
        public bool IsLimitedEdition;
        /// <summary>
        /// if true, then only one item instance of this type will exist and its remaininguses will be incremented instead.
        /// RemainingUses will cap out at Int32.Max (2,147,483,647). All subsequent increases will be discarded
        /// </summary>
        public bool IsStackable;
        /// <summary>
        /// if true, then an item instance of this type can be traded between players using the trading APIs
        /// </summary>
        public bool IsTradable;
        /// <summary>
        /// class to which the item belongs
        /// </summary>
        public string ItemClass;
        /// <summary>
        /// unique identifier for this item
        /// </summary>
        public string ItemId;
                public string ItemImageUrl;
        /// <summary>
        /// override prices for this item for specific currencies
        /// </summary>
        public Dictionary<string,uint> RealCurrencyPrices;
        /// <summary>
        /// list of item tags
        /// </summary>
        public List<string> Tags;
        /// <summary>
        /// price of this item in virtual currencies and "RM" (the base Real Money purchase price, in USD pennies)
        /// </summary>
        public Dictionary<string,uint> VirtualCurrencyPrices;
    }

    [Serializable]
    public class CatalogItemBundleInfo : PlayFabBaseModel
    {
        /// <summary>
        /// unique ItemId values for all items which will be added to the player inventory when the bundle is added
        /// </summary>
        public List<string> BundledItems;
        /// <summary>
        /// unique TableId values for all RandomResultTable objects which are part of the bundle (random tables will be resolved and
        /// add the relevant items to the player inventory when the bundle is added)
        /// </summary>
        public List<string> BundledResultTables;
        /// <summary>
        /// virtual currency types and balances which will be added to the player inventory when the bundle is added
        /// </summary>
        public Dictionary<string,uint> BundledVirtualCurrencies;
    }

    [Serializable]
    public class CatalogItemConsumableInfo : PlayFabBaseModel
    {
        /// <summary>
        /// number of times this object can be used, after which it will be removed from the player inventory
        /// </summary>
        public uint? UsageCount;
        /// <summary>
        /// duration in seconds for how long the item will remain in the player inventory - once elapsed, the item will be removed
        /// (recommended minimum value is 5 seconds, as lower values can cause the item to expire before operations depending on
        /// this item's details have completed)
        /// </summary>
        public uint? UsagePeriod;
        /// <summary>
        /// all inventory item instances in the player inventory sharing a non-null UsagePeriodGroup have their UsagePeriod values
        /// added together, and share the result - when that period has elapsed, all the items in the group will be removed
        /// </summary>
        public string UsagePeriodGroup;
    }

    /// <summary>
    /// Containers are inventory items that can hold other items defined in the catalog, as well as virtual currency, which is
    /// added to the player inventory when the container is unlocked, using the UnlockContainerItem API. The items can be
    /// anything defined in the catalog, as well as RandomResultTable objects which will be resolved when the container is
    /// unlocked. Containers and their keys should be defined as Consumable (having a limited number of uses) in their catalog
    /// defintiions, unless the intent is for the player to be able to re-use them infinitely.
    /// </summary>
    [Serializable]
    public class CatalogItemContainerInfo : PlayFabBaseModel
    {
        /// <summary>
        /// unique ItemId values for all items which will be added to the player inventory, once the container has been unlocked
        /// </summary>
        public List<string> ItemContents;
        /// <summary>
        /// ItemId for the catalog item used to unlock the container, if any (if not specified, a call to UnlockContainerItem will
        /// open the container, adding the contents to the player inventory and currency balances)
        /// </summary>
        public string KeyItemId;
        /// <summary>
        /// unique TableId values for all RandomResultTable objects which are part of the container (once unlocked, random tables
        /// will be resolved and add the relevant items to the player inventory)
        /// </summary>
        public List<string> ResultTableContents;
        /// <summary>
        /// virtual currency types and balances which will be added to the player inventory when the container is unlocked
        /// </summary>
        public Dictionary<string,uint> VirtualCurrencyContents;
    }

    [Serializable]
    public class CharacterInventory : PlayFabBaseModel
    {
        /// <summary>
        /// The id of this character.
        /// </summary>
        public string CharacterId;
        /// <summary>
        /// The inventory of this character.
        /// </summary>
        public List<ItemInstance> Inventory;
    }

    [Serializable]
    public class CharacterResult : PlayFabBaseModel
    {
        /// <summary>
        /// The id for this character on this player.
        /// </summary>
        public string CharacterId;
        /// <summary>
        /// The name of this character.
        /// </summary>
        public string CharacterName;
        /// <summary>
        /// The type-string that was given to this character on creation.
        /// </summary>
        public string CharacterType;
    }

    [Serializable]
    public class ContactEmailInfoModel : PlayFabBaseModel
    {
        /// <summary>
        /// The email address
        /// </summary>
        public string EmailAddress;
        /// <summary>
        /// The name of the email info data
        /// </summary>
        public string Name;
        /// <summary>
        /// The verification status of the email
        /// </summary>
        public EmailVerificationStatus? VerificationStatus;
    }

    public enum ContinentCode
    {
        AF,
        AN,
        AS,
        EU,
        NA,
        OC,
        SA,
        Unknown
    }

    public enum CountryCode
    {
        AF,
        AX,
        AL,
        DZ,
        AS,
        AD,
        AO,
        AI,
        AQ,
        AG,
        AR,
        AM,
        AW,
        AU,
        AT,
        AZ,
        BS,
        BH,
        BD,
        BB,
        BY,
        BE,
        BZ,
        BJ,
        BM,
        BT,
        BO,
        BQ,
        BA,
        BW,
        BV,
        BR,
        IO,
        BN,
        BG,
        BF,
        BI,
        KH,
        CM,
        CA,
        CV,
        KY,
        CF,
        TD,
        CL,
        CN,
        CX,
        CC,
        CO,
        KM,
        CG,
        CD,
        CK,
        CR,
        CI,
        HR,
        CU,
        CW,
        CY,
        CZ,
        DK,
        DJ,
        DM,
        DO,
        EC,
        EG,
        SV,
        GQ,
        ER,
        EE,
        ET,
        FK,
        FO,
        FJ,
        FI,
        FR,
        GF,
        PF,
        TF,
        GA,
        GM,
        GE,
        DE,
        GH,
        GI,
        GR,
        GL,
        GD,
        GP,
        GU,
        GT,
        GG,
        GN,
        GW,
        GY,
        HT,
        HM,
        VA,
        HN,
        HK,
        HU,
        IS,
        IN,
        ID,
        IR,
        IQ,
        IE,
        IM,
        IL,
        IT,
        JM,
        JP,
        JE,
        JO,
        KZ,
        KE,
        KI,
        KP,
        KR,
        KW,
        KG,
        LA,
        LV,
        LB,
        LS,
        LR,
        LY,
        LI,
        LT,
        LU,
        MO,
        MK,
        MG,
        MW,
        MY,
        MV,
        ML,
        MT,
        MH,
        MQ,
        MR,
        MU,
        YT,
        MX,
        FM,
        MD,
        MC,
        MN,
        ME,
        MS,
        MA,
        MZ,
        MM,
        NA,
        NR,
        NP,
        NL,
        NC,
        NZ,
        NI,
        NE,
        NG,
        NU,
        NF,
        MP,
        NO,
        OM,
        PK,
        PW,
        PS,
        PA,
        PG,
        PY,
        PE,
        PH,
        PN,
        PL,
        PT,
        PR,
        QA,
        RE,
        RO,
        RU,
        RW,
        BL,
        SH,
        KN,
        LC,
        MF,
        PM,
        VC,
        WS,
        SM,
        ST,
        SA,
        SN,
        RS,
        SC,
        SL,
        SG,
        SX,
        SK,
        SI,
        SB,
        SO,
        ZA,
        GS,
        SS,
        ES,
        LK,
        SD,
        SR,
        SJ,
        SZ,
        SE,
        CH,
        SY,
        TW,
        TJ,
        TZ,
        TH,
        TL,
        TG,
        TK,
        TO,
        TT,
        TN,
        TR,
        TM,
        TC,
        TV,
        UG,
        UA,
        AE,
        GB,
        US,
        UM,
        UY,
        UZ,
        VU,
        VE,
        VN,
        VG,
        VI,
        WF,
        EH,
        YE,
        ZM,
        ZW,
        Unknown
    }

    public enum Currency
    {
        AED,
        AFN,
        ALL,
        AMD,
        ANG,
        AOA,
        ARS,
        AUD,
        AWG,
        AZN,
        BAM,
        BBD,
        BDT,
        BGN,
        BHD,
        BIF,
        BMD,
        BND,
        BOB,
        BRL,
        BSD,
        BTN,
        BWP,
        BYR,
        BZD,
        CAD,
        CDF,
        CHF,
        CLP,
        CNY,
        COP,
        CRC,
        CUC,
        CUP,
        CVE,
        CZK,
        DJF,
        DKK,
        DOP,
        DZD,
        EGP,
        ERN,
        ETB,
        EUR,
        FJD,
        FKP,
        GBP,
        GEL,
        GGP,
        GHS,
        GIP,
        GMD,
        GNF,
        GTQ,
        GYD,
        HKD,
        HNL,
        HRK,
        HTG,
        HUF,
        IDR,
        ILS,
        IMP,
        INR,
        IQD,
        IRR,
        ISK,
        JEP,
        JMD,
        JOD,
        JPY,
        KES,
        KGS,
        KHR,
        KMF,
        KPW,
        KRW,
        KWD,
        KYD,
        KZT,
        LAK,
        LBP,
        LKR,
        LRD,
        LSL,
        LYD,
        MAD,
        MDL,
        MGA,
        MKD,
        MMK,
        MNT,
        MOP,
        MRO,
        MUR,
        MVR,
        MWK,
        MXN,
        MYR,
        MZN,
        NAD,
        NGN,
        NIO,
        NOK,
        NPR,
        NZD,
        OMR,
        PAB,
        PEN,
        PGK,
        PHP,
        PKR,
        PLN,
        PYG,
        QAR,
        RON,
        RSD,
        RUB,
        RWF,
        SAR,
        SBD,
        SCR,
        SDG,
        SEK,
        SGD,
        SHP,
        SLL,
        SOS,
        SPL,
        SRD,
        STD,
        SVC,
        SYP,
        SZL,
        THB,
        TJS,
        TMT,
        TND,
        TOP,
        TRY,
        TTD,
        TVD,
        TWD,
        TZS,
        UAH,
        UGX,
        USD,
        UYU,
        UZS,
        VEF,
        VND,
        VUV,
        WST,
        XAF,
        XCD,
        XDR,
        XOF,
        XPF,
        YER,
        ZAR,
        ZMW,
        ZWD
    }

    public enum EmailVerificationStatus
    {
        Unverified,
        Pending,
        Confirmed
    }

    /// <summary>
    /// Combined entity type and ID structure which uniquely identifies a single entity.
    /// </summary>
    [Serializable]
    public class EntityKey : PlayFabBaseModel
    {
        /// <summary>
        /// Unique ID of the entity.
        /// </summary>
        public string Id;
        /// <summary>
        /// Entity type. See https://docs.microsoft.com/gaming/playfab/features/data/entities/available-built-in-entity-types
        /// </summary>
        public string Type;
    }

    [Serializable]
    public class EntityTokenResponse : PlayFabBaseModel
    {
        /// <summary>
        /// The entity id and type.
        /// </summary>
        public EntityKey Entity;
        /// <summary>
        /// The token used to set X-EntityToken for all entity based API calls.
        /// </summary>
        public string EntityToken;
        /// <summary>
        /// The time the token will expire, if it is an expiring token, in UTC.
        /// </summary>
        public DateTime? TokenExpiration;
    }

    /// <summary>
    /// If CatalogVersion is not specified, only inventory items associated with the most recent version of the catalog will be
    /// returned.
    /// </summary>
    [Serializable]
    public class GetCatalogItemsResult : PlayFabResultCommon
    {
        /// <summary>
        /// Array of items which can be purchased.
        /// </summary>
        public List<CatalogItem> Catalog;
    }

    /// <summary>
    /// Note: When calling 'GetLeaderboardAround...' APIs, the position of the user defaults to 0 when the user does not have
    /// the corresponding statistic.
    /// </summary>
    [Serializable]
    public class GetLeaderboardAroundPlayerResult : PlayFabResultCommon
    {
        /// <summary>
        /// Ordered listing of users and their positions in the requested leaderboard.
        /// </summary>
        public List<PlayerLeaderboardEntry> Leaderboard;
        /// <summary>
        /// The time the next scheduled reset will occur. Null if the leaderboard does not reset on a schedule.
        /// </summary>
        public DateTime? NextReset;
        /// <summary>
        /// The version of the leaderboard returned.
        /// </summary>
        public int Version;
    }

    /// <summary>
    /// Note: the user's Position is relative to the overall leaderboard.
    /// </summary>
    [Serializable]
    public class GetLeaderboardResult : PlayFabResultCommon
    {
        /// <summary>
        /// Ordered listing of users and their positions in the requested leaderboard.
        /// </summary>
        public List<PlayerLeaderboardEntry> Leaderboard;
        /// <summary>
        /// The time the next scheduled reset will occur. Null if the leaderboard does not reset on a schedule.
        /// </summary>
        public DateTime? NextReset;
        /// <summary>
        /// The version of the leaderboard returned.
        /// </summary>
        public int Version;
    }

    [Serializable]
    public class GetPlayerCombinedInfoResultPayload : PlayFabBaseModel
    {
        /// <summary>
        /// Account information for the user. This is always retrieved.
        /// </summary>
        public UserAccountInfo AccountInfo;
        /// <summary>
        /// Inventories for each character for the user.
        /// </summary>
        public List<CharacterInventory> CharacterInventories;
        /// <summary>
        /// List of characters for the user.
        /// </summary>
        public List<CharacterResult> CharacterList;
        /// <summary>
        /// The profile of the players. This profile is not guaranteed to be up-to-date. For a new player, this profile will not
        /// exist.
        /// </summary>
        public PlayerProfileModel PlayerProfile;
        /// <summary>
        /// List of statistics for this player.
        /// </summary>
        public List<StatisticValue> PlayerStatistics;
        /// <summary>
        /// Title data for this title.
        /// </summary>
        public Dictionary<string,string> TitleData;
        /// <summary>
        /// User specific custom data.
        /// </summary>
        public Dictionary<string,UserDataRecord> UserData;
        /// <summary>
        /// The version of the UserData that was returned.
        /// </summary>
        public uint UserDataVersion;
        /// <summary>
        /// Array of inventory items in the user's current inventory.
        /// </summary>
        public List<ItemInstance> UserInventory;
        /// <summary>
        /// User specific read-only data.
        /// </summary>
        public Dictionary<string,UserDataRecord> UserReadOnlyData;
        /// <summary>
        /// The version of the Read-Only UserData that was returned.
        /// </summary>
        public uint UserReadOnlyDataVersion;
        /// <summary>
        /// Dictionary of virtual currency balance(s) belonging to the user.
        /// </summary>
        public Dictionary<string,int> UserVirtualCurrency;
        /// <summary>
        /// Dictionary of remaining times and timestamps for virtual currencies.
        /// </summary>
        public Dictionary<string,VirtualCurrencyRechargeTime> UserVirtualCurrencyRechargeTimes;
    }

    [Serializable]
    public class GetSegmentResult : PlayFabBaseModel
    {
        /// <summary>
        /// Identifier of the segments AB Test, if it is attached to one.
        /// </summary>
        public string ABTestParent;
        /// <summary>
        /// Unique identifier for this segment.
        /// </summary>
        public string Id;
        /// <summary>
        /// Segment name.
        /// </summary>
        public string Name;
    }

    [Serializable]
    public class GetStoreItemsResult : PlayFabResultCommon
    {
        /// <summary>
        /// The base catalog that this store is a part of.
        /// </summary>
        public string CatalogVersion;
        /// <summary>
        /// Additional data about the store.
        /// </summary>
        public StoreMarketingModel MarketingData;
        /// <summary>
        /// How the store was last updated (Admin or a third party).
        /// </summary>
        public SourceType? Source;
        /// <summary>
        /// Array of items which can be purchased from this store.
        /// </summary>
        public List<StoreItem> Store;
        /// <summary>
        /// The ID of this store.
        /// </summary>
        public string StoreId;
    }

    /// <summary>
    /// A unique instance of an item in a user's inventory. Note, to retrieve additional information for an item such as Tags,
    /// Description that are the same across all instances of the item, a call to GetCatalogItems is required. The ItemID of can
    /// be matched to a catalog entry, which contains the additional information. Also note that Custom Data is only set when
    /// the User's specific instance has updated the CustomData via a call to UpdateUserInventoryItemCustomData. Other fields
    /// such as UnitPrice and UnitCurrency are only set when the item was granted via a purchase.
    /// </summary>
    [Serializable]
    public class ItemInstance : PlayFabBaseModel
    {
        /// <summary>
        /// Game specific comment associated with this instance when it was added to the user inventory.
        /// </summary>
        public string Annotation;
        /// <summary>
        /// Array of unique items that were awarded when this catalog item was purchased.
        /// </summary>
        public List<string> BundleContents;
        /// <summary>
        /// Unique identifier for the parent inventory item, as defined in the catalog, for object which were added from a bundle or
        /// container.
        /// </summary>
        public string BundleParent;
        /// <summary>
        /// Catalog version for the inventory item, when this instance was created.
        /// </summary>
        public string CatalogVersion;
        /// <summary>
        /// A set of custom key-value pairs on the instance of the inventory item, which is not to be confused with the catalog
        /// item's custom data.
        /// </summary>
        public Dictionary<string,string> CustomData;
        /// <summary>
        /// CatalogItem.DisplayName at the time this item was purchased.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Timestamp for when this instance will expire.
        /// </summary>
        public DateTime? Expiration;
        /// <summary>
        /// Class name for the inventory item, as defined in the catalog.
        /// </summary>
        public string ItemClass;
        /// <summary>
        /// Unique identifier for the inventory item, as defined in the catalog.
        /// </summary>
        public string ItemId;
        /// <summary>
        /// Unique item identifier for this specific instance of the item.
        /// </summary>
        public string ItemInstanceId;
        /// <summary>
        /// Timestamp for when this instance was purchased.
        /// </summary>
        public DateTime? PurchaseDate;
        /// <summary>
        /// Total number of remaining uses, if this is a consumable item.
        /// </summary>
        public int? RemainingUses;
        /// <summary>
        /// Currency type for the cost of the catalog item. Not available when granting items.
        /// </summary>
        public string UnitCurrency;
        /// <summary>
        /// Cost of the catalog item in the given currency. Not available when granting items.
        /// </summary>
        public uint UnitPrice;
        /// <summary>
        /// The number of uses that were added or removed to this item in this call.
        /// </summary>
        public int? UsesIncrementedBy;
    }

    [Serializable]
    public class LinkedPlatformAccountModel : PlayFabBaseModel
    {
        /// <summary>
        /// Linked account email of the user on the platform, if available
        /// </summary>
        public string Email;
        /// <summary>
        /// Authentication platform
        /// </summary>
        public LoginIdentityProvider? Platform;
        /// <summary>
        /// Unique account identifier of the user on the platform
        /// </summary>
        public string PlatformUserId;
        /// <summary>
        /// Linked account username of the user on the platform, if available
        /// </summary>
        public string Username;
    }

    [Serializable]
    public class LocationModel : PlayFabBaseModel
    {
        /// <summary>
        /// City name.
        /// </summary>
        public string City;
        /// <summary>
        /// The two-character continent code for this location
        /// </summary>
        public ContinentCode? ContinentCode;
        /// <summary>
        /// The two-character ISO 3166-1 country code for the country associated with the location
        /// </summary>
        public CountryCode? CountryCode;
        /// <summary>
        /// Latitude coordinate of the geographic location.
        /// </summary>
        public double? Latitude;
        /// <summary>
        /// Longitude coordinate of the geographic location.
        /// </summary>
        public double? Longitude;
    }
public enum LoginIdentityProvider
{
    Unknown = 0,
    PlayFab = 1,
    Custom = 2,
    GameCenter = 3,
    GooglePlay = 4,
    Steam = 5,
    XBoxLive = 6,
    PSN = 7,
    Kongregate = 8,
    IOSDevice = 10,
    AndroidDevice = 11,
    Twitch = 12,
    WindowsHello = 13,
    GameServer = 14,
    CustomServer = 15,
    NintendoSwitch = 16,
    OpenIdConnect = 18,
    Apple = 19,
    NintendoSwitchAccount = 20,
    GooglePlayGames = 21,
    XboxMobileStore = 22,
    King = 23,
    BattleNet = 24
}
    [Serializable]
    public class LoginResult : PlayFabLoginResultCommon
    {
        /// <summary>
        /// If LoginTitlePlayerAccountEntity flag is set on the login request the title_player_account will also be logged in and
        /// returned.
        /// </summary>
        public EntityTokenResponse EntityToken;
        /// <summary>
        /// Results for requested info.
        /// </summary>
        public GetPlayerCombinedInfoResultPayload InfoResultPayload;
        /// <summary>
        /// The time of this user's previous login. If there was no previous login, then it's DateTime.MinValue
        /// </summary>
        public DateTime? LastLoginTime;
        /// <summary>
        /// True if the master_player_account was newly created on this login.
        /// </summary>
        public bool NewlyCreated;
        /// <summary>
        /// Player's unique PlayFabId.
        /// </summary>
        public string PlayFabId;
        /// <summary>
        /// Unique token authorizing the user and game at the server level, for the current session.
        /// </summary>
        public string SessionTicket;
        /// <summary>
        /// Settings specific to this user.
        /// </summary>
        public UserSettings SettingsForUser;
        /// <summary>
        /// The experimentation treatments for this user at the time of login.
        /// </summary>
        public TreatmentAssignment TreatmentAssignment;
    }

    [Serializable]
    public class MembershipModel : PlayFabBaseModel
    {
        /// <summary>
        /// Whether this membership is active. That is, whether the MembershipExpiration time has been reached.
        /// </summary>
        public bool IsActive;
        /// <summary>
        /// The time this membership expires
        /// </summary>
        public DateTime MembershipExpiration;
        /// <summary>
        /// The id of the membership
        /// </summary>
        public string MembershipId;
        /// <summary>
        /// Membership expirations can be explicitly overridden (via game manager or the admin api). If this membership has been
        /// overridden, this will be the new expiration time.
        /// </summary>
        public DateTime? OverrideExpiration;
        /// <summary>
        /// The list of subscriptions that this player has for this membership
        /// </summary>
        public List<SubscriptionModel> Subscriptions;
    }

    [Serializable]
    public class PlayerLeaderboardEntry : PlayFabBaseModel
    {
        /// <summary>
        /// Title-specific display name of the user for this leaderboard entry.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// PlayFab unique identifier of the user for this leaderboard entry.
        /// </summary>
        public string PlayFabId;
        /// <summary>
        /// User's overall position in the leaderboard.
        /// </summary>
        public int Position;
        /// <summary>
        /// The profile of the user, if requested.
        /// </summary>
        public PlayerProfileModel Profile;
        /// <summary>
        /// Specific value of the user's statistic.
        /// </summary>
        public int StatValue;
    }

    [Serializable]
    public class PlayerProfileModel : PlayFabBaseModel
    {
        /// <summary>
        /// List of advertising campaigns the player has been attributed to
        /// </summary>
        public List<AdCampaignAttributionModel> AdCampaignAttributions;
        /// <summary>
        /// URL of the player's avatar image
        /// </summary>
        public string AvatarUrl;
        /// <summary>
        /// If the player is currently banned, the UTC Date when the ban expires
        /// </summary>
        public DateTime? BannedUntil;
        /// <summary>
        /// List of all contact email info associated with the player account
        /// </summary>
        public List<ContactEmailInfoModel> ContactEmailAddresses;
        /// <summary>
        /// Player record created
        /// </summary>
        public DateTime? Created;
        /// <summary>
        /// Player display name
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// List of experiment variants for the player. Note that these variants are not guaranteed to be up-to-date when returned
        /// during login because the player profile is updated only after login. Instead, use the LoginResult.TreatmentAssignment
        /// property during login to get the correct variants and variables.
        /// </summary>
        public List<string> ExperimentVariants;
        /// <summary>
        /// UTC time when the player most recently logged in to the title
        /// </summary>
        public DateTime? LastLogin;
        /// <summary>
        /// List of all authentication systems linked to this player account
        /// </summary>
        public List<LinkedPlatformAccountModel> LinkedAccounts;
        /// <summary>
        /// List of geographic locations from which the player has logged in to the title
        /// </summary>
        public List<LocationModel> Locations;
        /// <summary>
        /// List of memberships for the player, along with whether are expired.
        /// </summary>
        public List<MembershipModel> Memberships;
        /// <summary>
        /// Player account origination
        /// </summary>
        public LoginIdentityProvider? Origination;
        /// <summary>
        /// PlayFab player account unique identifier
        /// </summary>
        public string PlayerId;
        /// <summary>
        /// Publisher this player belongs to
        /// </summary>
        public string PublisherId;
        /// <summary>
        /// List of configured end points registered for sending the player push notifications
        /// </summary>
        public List<PushNotificationRegistrationModel> PushNotificationRegistrations;
        /// <summary>
        /// List of leaderboard statistic values for the player
        /// </summary>
        public List<StatisticModel> Statistics;
        /// <summary>
        /// List of player's tags for segmentation
        /// </summary>
        public List<TagModel> Tags;
        /// <summary>
        /// Title ID this player profile applies to
        /// </summary>
        public string TitleId;
        /// <summary>
        /// Sum of the player's purchases made with real-money currencies, converted to US dollars equivalent and represented as a
        /// whole number of cents (1/100 USD). For example, 999 indicates nine dollars and ninety-nine cents.
        /// </summary>
        public uint? TotalValueToDateInUSD;
        /// <summary>
        /// List of the player's lifetime purchase totals, summed by real-money currency
        /// </summary>
        public List<ValueToDateModel> ValuesToDate;
    }

    public enum PushNotificationPlatform
    {
        ApplePushNotificationService,
        GoogleCloudMessaging
    }

    [Serializable]
    public class PushNotificationRegistrationModel : PlayFabBaseModel
    {
        /// <summary>
        /// Notification configured endpoint
        /// </summary>
        public string NotificationEndpointARN;
        /// <summary>
        /// Push notification platform
        /// </summary>
        public PushNotificationPlatform? Platform;
    }

    public enum SourceType
    {
        Admin,
        BackEnd,
        GameClient,
        GameServer,
        Partner,
        Custom,
        API
    }

    [Serializable]
    public class StatisticModel : PlayFabBaseModel
    {
        /// <summary>
        /// Statistic name
        /// </summary>
        public string Name;
        /// <summary>
        /// Statistic value
        /// </summary>
        public int Value;
        /// <summary>
        /// Statistic version (0 if not a versioned statistic)
        /// </summary>
        public int Version;
    }

    [Serializable]
    public class StatisticValue : PlayFabBaseModel
    {
        /// <summary>
        /// unique name of the statistic
        /// </summary>
        public string StatisticName;
        /// <summary>
        /// statistic value for the player
        /// </summary>
        public int Value;
        /// <summary>
        /// for updates to an existing statistic value for a player, the version of the statistic when it was loaded
        /// </summary>
        public uint Version;
    }

    /// <summary>
    /// A store entry that list a catalog item at a particular price
    /// </summary>
    [Serializable]
    public class StoreItem : PlayFabBaseModel
    {
        /// <summary>
        /// Store specific custom data. The data only exists as part of this store; it is not transferred to item instances
        /// </summary>
        public object CustomData;
        /// <summary>
        /// Intended display position for this item. Note that 0 is the first position
        /// </summary>
        public uint? DisplayPosition;
        /// <summary>
        /// Unique identifier of the item as it exists in the catalog - note that this must exactly match the ItemId from the
        /// catalog
        /// </summary>
        public string ItemId;
        /// <summary>
        /// Override prices for this item for specific currencies
        /// </summary>
        public Dictionary<string,uint> RealCurrencyPrices;
        /// <summary>
        /// Override prices for this item in virtual currencies and "RM" (the base Real Money purchase price, in USD pennies)
        /// </summary>
        public Dictionary<string,uint> VirtualCurrencyPrices;
    }

    /// <summary>
    /// Marketing data about a specific store
    /// </summary>
    [Serializable]
    public class StoreMarketingModel : PlayFabBaseModel
    {
        /// <summary>
        /// Tagline for a store.
        /// </summary>
        public string Description;
        /// <summary>
        /// Display name of a store as it will appear to users.
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// Custom data about a store.
        /// </summary>
        public object Metadata;
    }

    [Serializable]
    public class SubscriptionModel : PlayFabBaseModel
    {
        /// <summary>
        /// When this subscription expires.
        /// </summary>
        public DateTime Expiration;
        /// <summary>
        /// The time the subscription was orignially purchased
        /// </summary>
        public DateTime InitialSubscriptionTime;
        /// <summary>
        /// Whether this subscription is currently active. That is, if Expiration > now.
        /// </summary>
        public bool IsActive;
        /// <summary>
        /// The status of this subscription, according to the subscription provider.
        /// </summary>
        public SubscriptionProviderStatus? Status;
        /// <summary>
        /// The id for this subscription
        /// </summary>
        public string SubscriptionId;
        /// <summary>
        /// The item id for this subscription from the primary catalog
        /// </summary>
        public string SubscriptionItemId;
        /// <summary>
        /// The provider for this subscription. Apple or Google Play are supported today.
        /// </summary>
        public string SubscriptionProvider;
    }

    public enum SubscriptionProviderStatus
    {
        NoError,
        Cancelled,
        UnknownError,
        BillingError,
        ProductUnavailable,
        CustomerDidNotAcceptPriceChange,
        FreeTrial,
        PaymentPending
    }

    [Serializable]
    public class TagModel : PlayFabBaseModel
    {
        /// <summary>
        /// Full value of the tag, including namespace
        /// </summary>
        public string TagValue;
    }

    public enum TitleActivationStatus
    {
        None,
        ActivatedTitleKey,
        PendingSteam,
        ActivatedSteam,
        RevokedSteam
    }

    [Serializable]
    public class TitleNewsItem : PlayFabBaseModel
    {
        /// <summary>
        /// News item text.
        /// </summary>
        public string Body;
        /// <summary>
        /// Unique identifier of news item.
        /// </summary>
        public string NewsId;
        /// <summary>
        /// Date and time when the news item was posted.
        /// </summary>
        public DateTime Timestamp;
        /// <summary>
        /// Title of the news item.
        /// </summary>
        public string Title;
    }

    [Serializable]
    public class TreatmentAssignment : PlayFabBaseModel
    {
        /// <summary>
        /// List of the experiment variables.
        /// </summary>
        public List<Variable> Variables;
        /// <summary>
        /// List of the experiment variants.
        /// </summary>
        public List<string> Variants;
    }

    [Serializable]
    public class UpdateUserTitleDisplayNameResult : PlayFabResultCommon
    {
        /// <summary>
        /// Current title display name for the user (this will be the original display name if the rename attempt failed).
        /// </summary>
        public string DisplayName;
    }

    [Serializable]
    public class UserAccountInfo : PlayFabBaseModel
    {
        /// <summary>
        /// User Android device information, if an Android device has been linked
        /// </summary>
        public UserAndroidDeviceInfo AndroidDeviceInfo;
        /// <summary>
        /// Sign in with Apple account information, if an Apple account has been linked
        /// </summary>
        public UserAppleIdInfo AppleAccountInfo;
        /// <summary>
        /// Battle.net account information, if a Battle.net account has been linked
        /// </summary>
        public UserBattleNetInfo BattleNetAccountInfo;
        /// <summary>
        /// Timestamp indicating when the user account was created
        /// </summary>
        public DateTime Created;
        /// <summary>
        /// Custom ID information, if a custom ID has been assigned
        /// </summary>
        public UserCustomIdInfo CustomIdInfo;
        /// <summary>
        /// User Gamecenter information, if a Gamecenter account has been linked
        /// </summary>
        public UserGameCenterInfo GameCenterInfo;
        /// <summary>
        /// User Google account information, if a Google account has been linked
        /// </summary>
        public UserGoogleInfo GoogleInfo;
        /// <summary>
        /// User Google Play Games account information, if a Google Play Games account has been linked
        /// </summary>
        public UserGooglePlayGamesInfo GooglePlayGamesInfo;
        /// <summary>
        /// User iOS device information, if an iOS device has been linked
        /// </summary>
        public UserIosDeviceInfo IosDeviceInfo;
        /// <summary>
        /// User Kongregate account information, if a Kongregate account has been linked
        /// </summary>
        public UserKongregateInfo KongregateInfo;
        /// <summary>
        /// Nintendo Switch account information, if a Nintendo Switch account has been linked
        /// </summary>
        public UserNintendoSwitchAccountIdInfo NintendoSwitchAccountInfo;
        /// <summary>
        /// Nintendo Switch device information, if a Nintendo Switch device has been linked
        /// </summary>
        public UserNintendoSwitchDeviceIdInfo NintendoSwitchDeviceIdInfo;
        /// <summary>
        /// OpenID Connect information, if any OpenID Connect accounts have been linked
        /// </summary>
        public List<UserOpenIdInfo> OpenIdInfo;
        /// <summary>
        /// Unique identifier for the user account
        /// </summary>
        public string PlayFabId;
        /// <summary>
        /// Personal information for the user which is considered more sensitive
        /// </summary>
        public UserPrivateAccountInfo PrivateInfo;
        /// <summary>
        /// User PlayStation :tm: Network account information, if a PlayStation :tm: Network account has been linked
        /// </summary>
        public UserPsnInfo PsnInfo;
        /// <summary>
        /// Server Custom ID information, if a server custom ID has been assigned
        /// </summary>
        public UserServerCustomIdInfo ServerCustomIdInfo;
        /// <summary>
        /// User Steam information, if a Steam account has been linked
        /// </summary>
        public UserSteamInfo SteamInfo;
        /// <summary>
        /// Title-specific information for the user account
        /// </summary>
        public UserTitleInfo TitleInfo;
        /// <summary>
        /// User Twitch account information, if a Twitch account has been linked
        /// </summary>
        public UserTwitchInfo TwitchInfo;
        /// <summary>
        /// User account name in the PlayFab service
        /// </summary>
        public string Username;
        /// <summary>
        /// User XBox account information, if a XBox account has been linked
        /// </summary>
        public UserXboxInfo XboxInfo;
    }

    [Serializable]
    public class UserAndroidDeviceInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Android device ID
        /// </summary>
        public string AndroidDeviceId;
    }

    [Serializable]
    public class UserAppleIdInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Apple subject ID
        /// </summary>
        public string AppleSubjectId;
    }

    [Serializable]
    public class UserBattleNetInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Battle.net identifier
        /// </summary>
        public string BattleNetAccountId;
        /// <summary>
        /// Battle.net display name
        /// </summary>
        public string BattleNetBattleTag;
    }

    [Serializable]
    public class UserCustomIdInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Custom ID
        /// </summary>
        public string CustomId;
    }

    /// <summary>
    /// Indicates whether a given data key is private (readable only by the player) or public (readable by all players). When a
    /// player makes a GetUserData request about another player, only keys marked Public will be returned.
    /// </summary>
    public enum UserDataPermission
    {
        Private,
        Public
    }

    [Serializable]
    public class UserDataRecord : PlayFabBaseModel
    {
        /// <summary>
        /// Timestamp for when this data was last updated.
        /// </summary>
        public DateTime LastUpdated;
        /// <summary>
        /// Indicates whether this data can be read by all users (public) or only the user (private). This is used for GetUserData
        /// requests being made by one player about another player.
        /// </summary>
        public UserDataPermission? Permission;
        /// <summary>
        /// Data stored for the specified user data key.
        /// </summary>
        public string Value;
    }

    [Serializable]
    public class UserGameCenterInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Gamecenter identifier
        /// </summary>
        public string GameCenterId;
    }

    [Serializable]
    public class UserGoogleInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Email address of the Google account
        /// </summary>
        public string GoogleEmail;
        /// <summary>
        /// Gender information of the Google account
        /// </summary>
        public string GoogleGender;
        /// <summary>
        /// Google ID
        /// </summary>
        public string GoogleId;
        /// <summary>
        /// Locale of the Google account
        /// </summary>
        public string GoogleLocale;
        /// <summary>
        /// Name of the Google account user
        /// </summary>
        public string GoogleName;
    }

    [Serializable]
    public class UserGooglePlayGamesInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Avatar image url of the Google Play Games player
        /// </summary>
        public string GooglePlayGamesPlayerAvatarImageUrl;
        /// <summary>
        /// Display name of the Google Play Games player
        /// </summary>
        public string GooglePlayGamesPlayerDisplayName;
        /// <summary>
        /// Google Play Games player ID
        /// </summary>
        public string GooglePlayGamesPlayerId;
    }

    [Serializable]
    public class UserIosDeviceInfo : PlayFabBaseModel
    {
        /// <summary>
        /// iOS device ID
        /// </summary>
        public string IosDeviceId;
    }

    [Serializable]
    public class UserKongregateInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Kongregate ID
        /// </summary>
        public string KongregateId;
        /// <summary>
        /// Kongregate Username
        /// </summary>
        public string KongregateName;
    }

    [Serializable]
    public class UserNintendoSwitchAccountIdInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Nintendo Switch account subject ID
        /// </summary>
        public string NintendoSwitchAccountSubjectId;
    }

    [Serializable]
    public class UserNintendoSwitchDeviceIdInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Nintendo Switch Device ID
        /// </summary>
        public string NintendoSwitchDeviceId;
    }

    [Serializable]
    public class UserOpenIdInfo : PlayFabBaseModel
    {
        /// <summary>
        /// OpenID Connection ID
        /// </summary>
        public string ConnectionId;
        /// <summary>
        /// OpenID Issuer
        /// </summary>
        public string Issuer;
        /// <summary>
        /// OpenID Subject
        /// </summary>
        public string Subject;
    }
public enum UserOrigination
{
    Organic = 0,
    Steam = 1,
    Google = 2,
    Amazon = 3,
    Kongregate = 5,
    GamersFirst = 6,
    Unknown = 7,
    IOS = 8,
    LoadTest = 9,
    Android = 10,
    PSN = 11,
    GameCenter = 12,
    CustomId = 13,
    XboxLive = 14,
    Parse = 15,
    Twitch = 16,
    ServerCustomId = 17,
    NintendoSwitchDeviceId = 18,
    OpenIdConnect = 20,
    Apple = 21,
    NintendoSwitchAccount = 22,
    GooglePlayGames = 23,
    XboxMobileStore = 24,
    King = 25,
    BattleNet = 26
}
    [Serializable]
    public class UserPrivateAccountInfo : PlayFabBaseModel
    {
        /// <summary>
        /// user email address
        /// </summary>
        public string Email;
    }

    [Serializable]
    public class UserPsnInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Id of the PlayStation :tm: Network issuer environment this account is keyed under. Supply this value as IssuerId when
        /// looking the account up.
        /// </summary>
        public int? IssuerId;
        /// <summary>
        /// PlayStation :tm: Network account ID
        /// </summary>
        public string PsnAccountId;
        /// <summary>
        /// PlayStation :tm: Network online ID
        /// </summary>
        public string PsnOnlineId;
        /// <summary>
        /// PlayStation :tm: Network sandbox ID
        /// </summary>
        public string PsnSandboxId;
    }

    [Serializable]
    public class UserServerCustomIdInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Custom ID
        /// </summary>
        public string CustomId;
    }

    [Serializable]
    public class UserSettings : PlayFabBaseModel
    {
        /// <summary>
        /// Boolean for whether this player is eligible for gathering device info.
        /// </summary>
        public bool GatherDeviceInfo;
        /// <summary>
        /// Boolean for whether this player should report OnFocus play-time tracking.
        /// </summary>
        public bool GatherFocusInfo;
        /// <summary>
        /// Boolean for whether this player is eligible for ad tracking.
        /// </summary>
        public bool NeedsAttribution;
    }

    [Serializable]
    public class UserSteamInfo : PlayFabBaseModel
    {
        /// <summary>
        /// what stage of game ownership the user is listed as being in, from Steam
        /// </summary>
        public TitleActivationStatus? SteamActivationStatus;
        /// <summary>
        /// the country in which the player resides, from Steam data
        /// </summary>
        public string SteamCountry;
        /// <summary>
        /// currency type set in the user Steam account
        /// </summary>
        public Currency? SteamCurrency;
        /// <summary>
        /// Steam identifier
        /// </summary>
        public string SteamId;
        /// <summary>
        /// Steam display name
        /// </summary>
        public string SteamName;
    }

    [Serializable]
    public class UserTitleInfo : PlayFabBaseModel
    {
        /// <summary>
        /// URL to the player's avatar.
        /// </summary>
        public string AvatarUrl;
        /// <summary>
        /// timestamp indicating when the user was first associated with this game (this can differ significantly from when the user
        /// first registered with PlayFab)
        /// </summary>
        public DateTime Created;
        /// <summary>
        /// name of the user, as it is displayed in-game
        /// </summary>
        public string DisplayName;
        /// <summary>
        /// timestamp indicating when the user first signed into this game (this can differ from the Created timestamp, as other
        /// events, such as issuing a beta key to the user, can associate the title to the user)
        /// </summary>
        public DateTime? FirstLogin;
        /// <summary>
        /// boolean indicating whether or not the user is currently banned for a title
        /// </summary>
        public bool? isBanned;
        /// <summary>
        /// timestamp for the last user login for this title
        /// </summary>
        public DateTime? LastLogin;
        /// <summary>
        /// source by which the user first joined the game, if known
        /// </summary>
        public UserOrigination? Origination;
        /// <summary>
        /// Title player account entity for this user
        /// </summary>
        public EntityKey TitlePlayerAccount;
    }

    [Serializable]
    public class UserTwitchInfo : PlayFabBaseModel
    {
        /// <summary>
        /// Twitch ID
        /// </summary>
        public string TwitchId;
        /// <summary>
        /// Twitch Username
        /// </summary>
        public string TwitchUserName;
    }

    [Serializable]
    public class UserXboxInfo : PlayFabBaseModel
    {
        /// <summary>
        /// XBox user ID
        /// </summary>
        public string XboxUserId;
        /// <summary>
        /// XBox user sandbox
        /// </summary>
        public string XboxUserSandbox;
    }

    [Serializable]
    public class ValueToDateModel : PlayFabBaseModel
    {
        /// <summary>
        /// ISO 4217 code of the currency used in the purchases
        /// </summary>
        public string Currency;
        /// <summary>
        /// Total value of the purchases in a whole number of 1/100 monetary units. For example, 999 indicates nine dollars and
        /// ninety-nine cents when Currency is 'USD')
        /// </summary>
        public uint TotalValue;
        /// <summary>
        /// Total value of the purchases in a string representation of decimal monetary units. For example, '9.99' indicates nine
        /// dollars and ninety-nine cents when Currency is 'USD'.
        /// </summary>
        public string TotalValueAsDecimal;
    }

    [Serializable]
    public class Variable : PlayFabBaseModel
    {
        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string Name;
        /// <summary>
        /// Value of the variable.
        /// </summary>
        public string Value;
    }

    [Serializable]
    public class VirtualCurrencyRechargeTime : PlayFabBaseModel
    {
        /// <summary>
        /// Maximum value to which the regenerating currency will automatically increment. Note that it can exceed this value
        /// through use of the AddUserVirtualCurrency API call. However, it will not regenerate automatically until it has fallen
        /// below this value.
        /// </summary>
        public int RechargeMax;
        /// <summary>
        /// Server timestamp in UTC indicating the next time the virtual currency will be incremented.
        /// </summary>
        public DateTime RechargeTime;
        /// <summary>
        /// Time remaining (in seconds) before the next recharge increment of the virtual currency.
        /// </summary>
        public int SecondsToRecharge;
    }
}
#endif
