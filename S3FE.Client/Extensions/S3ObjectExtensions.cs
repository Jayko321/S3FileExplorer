using S3FE.Client.Models;
using S3FE.Shared.DTOs;

namespace S3FE.Client.Extensions;

public static class S3ObjectExtensions
{
    public static S3Object ToModel(this S3ObjectDTO dto)
    {
        return new S3Object(dto.Key, dto.Size, dto.LastModified, dto.ETag)
        {
            VersionIds = dto.VersionIds,
            VersionId = dto.VersionId,
            IsLatest = dto.IsLatest,
            IsDeleteMarker = dto.IsDeleteMarker
        };
    }
}
