using S3FE.Client.Models;
using S3FE.Shared.DTOs;

namespace S3FE.Client.Extensions;

public static class BucketExtensions
{
    public static BucketDTO ToDTO(this Bucket bucket)
    {
        return new BucketDTO { Name = bucket.Name };
    }

    public static Bucket ToModel(this BucketDTO dto)
    {
        return new Bucket(dto.Name);
    }
}
