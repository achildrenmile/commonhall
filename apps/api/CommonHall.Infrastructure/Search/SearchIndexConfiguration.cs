namespace CommonHall.Infrastructure.Search;

public static class SearchIndexConfiguration
{
    public const string NewsIndex = "commonhall-news";
    public const string PagesIndex = "commonhall-pages";
    public const string UsersIndex = "commonhall-users";
    public const string FilesIndex = "commonhall-files";

    public static readonly string[] AllIndexes = { NewsIndex, PagesIndex, UsersIndex, FilesIndex };

    public static class Mappings
    {
        public static readonly object NewsMapping = new
        {
            properties = new
            {
                id = new { type = "keyword" },
                title = new { type = "text", analyzer = "standard", boost = 3.0 },
                teaserText = new { type = "text", analyzer = "standard", boost = 2.0 },
                bodyText = new { type = "text", analyzer = "standard" },
                tags = new { type = "keyword" },
                channelName = new { type = "keyword" },
                channelSlug = new { type = "keyword" },
                spaceName = new { type = "keyword" },
                spaceSlug = new { type = "keyword" },
                authorName = new { type = "text" },
                authorId = new { type = "keyword" },
                status = new { type = "keyword" },
                publishedAt = new { type = "date" },
                createdAt = new { type = "date" },
                updatedAt = new { type = "date" },
                slug = new { type = "keyword" },
                viewCount = new { type = "integer" },
                visibilityRule = new { type = "object", enabled = false },
                teaserImageUrl = new { type = "keyword", index = false }
            }
        };

        public static readonly object PagesMapping = new
        {
            properties = new
            {
                id = new { type = "keyword" },
                title = new { type = "text", analyzer = "standard", boost = 3.0 },
                bodyText = new { type = "text", analyzer = "standard" },
                spaceName = new { type = "keyword" },
                spaceSlug = new { type = "keyword" },
                slug = new { type = "keyword" },
                status = new { type = "keyword" },
                createdAt = new { type = "date" },
                updatedAt = new { type = "date" }
            }
        };

        public static readonly object UsersMapping = new
        {
            properties = new
            {
                id = new { type = "keyword" },
                displayName = new { type = "text", analyzer = "standard", boost = 3.0 },
                email = new { type = "text", analyzer = "standard", boost = 2.0 },
                firstName = new { type = "text" },
                lastName = new { type = "text" },
                department = new { type = "keyword" },
                location = new { type = "keyword" },
                jobTitle = new { type = "text" },
                profilePhotoUrl = new { type = "keyword", index = false },
                isActive = new { type = "boolean" }
            }
        };

        public static readonly object FilesMapping = new
        {
            properties = new
            {
                id = new { type = "keyword" },
                originalName = new { type = "text", analyzer = "standard", boost = 3.0 },
                mimeType = new { type = "keyword" },
                collectionName = new { type = "keyword" },
                collectionId = new { type = "keyword" },
                altText = new { type = "text" },
                fileSize = new { type = "long" },
                createdAt = new { type = "date" },
                url = new { type = "keyword", index = false }
            }
        };
    }
}
