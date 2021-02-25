﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ColorPaletteBot.Webhook.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly IBotService _botService;

        public UpdateService(IBotService botService)
        {
            _botService = botService;
        }

        public async Task ProcessAsync(Update update)
        {
            var message = update.Message;

            if (message?.Type == MessageType.Photo)
            {
                var fileId = message.Photo.LastOrDefault()?.FileId;
                var file = await _botService.Client.GetFileAsync(fileId);

                var filename = file.FileId + "." + file.FilePath.Split('.').Last();
                Image image;
                string path;
                using (var saveImageStream = System.IO.File.Open(filename, FileMode.Create))
                {
                    await _botService.Client.DownloadFileAsync(file.FilePath, saveImageStream);
                    image = Image.FromStream(saveImageStream);
                    path = saveImageStream.Name;
                }

                var bitmap = Process(image);
                await _botService.Client.SendPhotoAsync(message.Chat.Id, new Telegram.Bot.Types.InputFiles.InputOnlineFile(bitmap.ToStream(ImageFormat.Jpeg)));
                System.IO.File.Delete(path);
            }
        }

        private Bitmap Process(Image source)
        {
            Dictionary<Color, int> cache = new Dictionary<Color, int>();
            Image.GetThumbnailImageAbort callback = new Image.GetThumbnailImageAbort(() => false);
            var image = new Bitmap(source.GetThumbnailImage(100, 100, callback, IntPtr.Zero));
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixelColor = image.GetPixel(x, y);
                    if (pixelColor.GetBrightness() > 0.2 && pixelColor.GetBrightness() < 0.8)
                    {
                        if (cache.TryGetValue(pixelColor, out int value))
                        {
                            cache[pixelColor]++;
                        }
                        else
                        {
                            cache[pixelColor] = 1;
                        }
                    }
                }
            }

            var colors = cache.OrderByDescending(x => x.Key.GetBrightness())
                .ThenByDescending(x => x.Value)
                .Take((int)(cache.Count() / 1.5))
                .Select(x => x.Key)
                .ToList();

            Bitmap result = new Bitmap(200, 500);
            Graphics graphics = Graphics.FromImage(result);

            int yIndex = 0;
            var count = 0;
            for (int i = 0; i < colors.Count(); i += (colors.Count() / 5))
            {
                if (count <= 4)
                {
                    for (int x = 0; x < result.Width; x++)
                    {
                        for (int j = yIndex; j < yIndex + 100; j++)
                        {
                            result.SetPixel(x, j, colors[i]);
                        }
                    }
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawString(ColorTranslator.ToHtml(colors[i]), new Font("Tahoma", 10), Brushes.Black, new PointF(result.Width / 3, yIndex + 50));
                    yIndex = yIndex + 100;
                }
                count++;
            }

            graphics.Flush();
            return result;
        }
    }
}
