using System.Text.Json;

namespace PaymentGateway.Web.Infrastructure.Providers.Paymob;

/// Paymob's transaction-processed callback signs a specific, ordered concatenation
/// of fields. Ordered exactly per Paymob spec.
public static class PaymobHmacFields
{
    private static readonly string[] Order =
    {
        "amount_cents",
        "created_at",
        "currency",
        "error_occured",
        "has_parent_transaction",
        "id",
        "integration_id",
        "is_3d_secure",
        "is_auth",
        "is_capture",
        "is_refunded",
        "is_standalone_payment",
        "is_voided",
        "order.id",
        "owner",
        "pending",
        "source_data.pan",
        "source_data.sub_type",
        "source_data.type",
        "success",
    };

    public static IDictionary<string, string?> Extract(JsonElement root)
    {
        var dict = new Dictionary<string, string?>();
        var obj = root.TryGetProperty("obj", out var o) ? o : root;
        foreach (var path in Order)
            dict[path] = ReadPath(obj, path);
        return dict;
    }

    private static string? ReadPath(JsonElement obj, string path)
    {
        var parts = path.Split('.');
        var current = obj;
        foreach (var p in parts)
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            if (!current.TryGetProperty(p, out var next)) return null;
            current = next;
        }
        return current.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.String => current.GetString(),
            _ => current.GetRawText(),
        };
    }
}
