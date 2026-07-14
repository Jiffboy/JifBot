using Discord;
using System.IO;
using System.Net;


namespace JifBot.Utils
{
    public class CommonImage
    {
        public bool isValid = true;
        public bool isNull = false;
        public byte[] imgBytes = null;
        public string imgType = null;
        public string imgName = "";
        public string thumbnailUrl = "";

        public CommonImage(IAttachment image)
        {
            // Images aren't always provided. Just leave everything null if not. Its what the DB expects.
            if (image == null)
            {
                isNull = true;
                return;
            }
            if (!image.ContentType.StartsWith("image/"))
            {
                isNull = true;
                isValid = false;
                return;
            }
            imgBytes = GetBytesFromAttachment(image);
            imgType = image.ContentType.Replace("image/", "");
            imgName = image.Filename;
            thumbnailUrl = image.Url;
        }

        public CommonImage(byte[] imageBytes,  string imageType)
        {
            if (imageBytes == null || imageType == null)
            {
                isValid = false;
                isNull = true;
            }
            else
            {
                imgBytes = imageBytes;
                imgType = imageType;
                imgName = $"image.{imgType}";
                thumbnailUrl = $"attachment://{imgName}";
            }
        }

        public MemoryStream GetMS()
        {
            return new MemoryStream(imgBytes);
        }

        private byte[] GetBytesFromAttachment(IAttachment attachment)
        {
            var client = new WebClient();
            return client.DownloadData(attachment.Url);
        }
    }
}
