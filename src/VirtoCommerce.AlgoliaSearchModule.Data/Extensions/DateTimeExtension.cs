using System;

namespace VirtoCommerce.AlgoliaSearchModule.Data.Extensions;

public static class DateTimeExtension
{
    public static long DateTimeToUnixTimestamp(DateTime dateTime)
    {
        var sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var unixTime = (long)(dateTime - sTime).TotalSeconds;
        return unixTime;
    }

    public static DateTime UnixTimestampToDateTime(long unixTimestamp)
    {
        var sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateTime = sTime.AddSeconds(unixTimestamp);
        return dateTime;
    }
}
