using CSharpImageLibrary.General;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GothicStarter.Utils
{
    #region Public Classes
    public static class ImageUtils
    {
        #region Public Methods
        /// <summary>
        /// Načte ze souboru obrázek ve formátu ZTEX.
        /// </summary>
        /// <param name="filePath">Cesta k souboru.</param>
        /// <remarks>
        /// Struktura ZTEX formátu.
        /// 
        /// +------------------------------------+
        /// | File Header(ZTEX_FILE_HEADER)      |
        /// |   Signature(ZTEX_FILE_SIGNATURE)   |
        /// |   Version(ZTEX_FILE_VERSION_0)     |
        /// |   Info Block(ZTEX_INFO)            |
        /// |     Format(ZTEX_FORMAT)            |
        /// |     Width of mipmap 0              |
        /// |     Height of mipmap 0             |
        /// |     Number of mipmaps(0 = none)    |
        /// |     Reference Width(ingame)        |
        /// |     Reference Height(ingame)       |
        /// |     Average Color(A8R8G8B8)        |
        /// +------------------------------------+
        /// | Texture data                       |
        /// | +--------------------------------+ |
        /// | | Palette data(ZTEXFMT_P8 only)  | |
        /// | +--------------------------------+ |
        /// | +--------------------------------+ |
        /// | | Pixel data for smallest mipmap | |
        /// | +--------------------------------+ |
        /// | |              ...               | |
        /// | +--------------------------------+ |
        /// | | Pixel data mipmap 0 (biggest)  | |
        /// | +--------------------------------+ |
        /// +------------------------------------+
        /// </remarks>
        public static Image LoadZTEXImage(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
            {
                ZTEX_FILE_HEADER header = ReadZTEXHeader(reader);

                if (header.TexInfo.Format >= ZTEX_FORMAT.ZTEXFMT_DXT1)
                {
                    ImageEngineFormat ddsFormat = ImageEngineFormat.Unknown;

                    switch (header.TexInfo.Format)
                    {
                        case ZTEX_FORMAT.ZTEXFMT_DXT1: ddsFormat = ImageEngineFormat.DDS_DXT1; break;
                        case ZTEX_FORMAT.ZTEXFMT_DXT2: ddsFormat = ImageEngineFormat.DDS_DXT2; break;
                        case ZTEX_FORMAT.ZTEXFMT_DXT3: ddsFormat = ImageEngineFormat.DDS_DXT3; break;
                        case ZTEX_FORMAT.ZTEXFMT_DXT4: ddsFormat = ImageEngineFormat.DDS_DXT4; break;
                        case ZTEX_FORMAT.ZTEXFMT_DXT5: ddsFormat = ImageEngineFormat.DDS_DXT5; break;
                    }

                    DDSGeneral.DDS_HEADER ddsHeader = DDSGeneral.Build_DDS_Header(header.TexInfo.MipMaps, header.TexInfo.Height, header.TexInfo.Width, ddsFormat);

                    using (MemoryStream ms = new MemoryStream())
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        DDSGeneral.Write_DDS_Header(ddsHeader, bw);
                        reader.BaseStream.CopyTo(ms);
                        ImageEngineImage img = new ImageEngineImage(ms);
                        return img.GetGDIBitmap(false, false);
                    }
                }

                //// TODO: Přidat podporu pro DXTn formáty textur...
                //// https://en.wikipedia.org/wiki/S3_Texture_Compression
                //switch (header.TexInfo.Format)
                //{
                //    case ZTEX_FORMAT.ZTEXFMT_DXT1:
                //    case ZTEX_FORMAT.ZTEXFMT_DXT2:
                //    case ZTEX_FORMAT.ZTEXFMT_DXT3:
                //    case ZTEX_FORMAT.ZTEXFMT_DXT4:
                //    case ZTEX_FORMAT.ZTEXFMT_DXT5:
                //        throw new NotSupportedException($"Formát textur DXTn není podporován.");
                //}

                // Pokud jsou barvy indexované, nejprve načíst paletu
                byte[] pallete = null;

                if (header.TexInfo.Format == ZTEX_FORMAT.ZTEXFMT_P8)
                    pallete = reader.ReadBytes(3 * ZTEX_FILE_HEADER.PalleteSize);

                // Přeskočení všech menších mipmap
                SkipMipMaps(reader, header);

                // Načtení dat
                int w = header.TexInfo.Width;
                int h = header.TexInfo.Height;
                int bytesPerPixel = BytesPerPixel(header.TexInfo.Format);

                byte[] data = reader.ReadBytes(w * h * bytesPerPixel);

                // Formát vhodný pro GDI+ knihovnu
                PixelFormat format = PixelFormat.DontCare;

                switch (header.TexInfo.Format)
                {
                    case ZTEX_FORMAT.ZTEXFMT_B8G8R8A8:
                        format = PixelFormat.Format32bppArgb;
                        data.SwapElementsInBlocks(4);
                        break;
                    case ZTEX_FORMAT.ZTEXFMT_R8G8B8A8:
                        format = PixelFormat.Format32bppArgb;

                        for (int i = 0; i < data.Length; i += 4)
                        {
                            byte tmp = data[i + 3];

                            data[i + 3] = data[i + 2];
                            data[i + 2] = data[i + 1];
                            data[i + 1] = data[i + 0];
                            data[i + 0] = tmp;
                        }

                        break;
                    case ZTEX_FORMAT.ZTEXFMT_A8B8G8R8:
                        format = PixelFormat.Format32bppArgb;

                        for (int i = 0; i < data.Length; i += 4)
                        {
                            byte tmp = data[i + 1];

                            data[i + 1] = data[i + 3];
                            data[i + 3] = tmp;
                        }

                        break;
                    case ZTEX_FORMAT.ZTEXFMT_A8R8G8B8:
                        format = PixelFormat.Format32bppArgb;
                        break;
                    case ZTEX_FORMAT.ZTEXFMT_B8G8R8:
                        format = PixelFormat.Format24bppRgb;
                        data.SwapElementsInBlocks(3);
                        break;
                    case ZTEX_FORMAT.ZTEXFMT_R8G8B8:
                        format = PixelFormat.Format24bppRgb;
                        break;
                    case ZTEX_FORMAT.ZTEXFMT_A4R4G4B4:
                        format = PixelFormat.Format32bppArgb;
                        byte[] argb = new byte[data.Length * 2];

                        for (int i = 0; i < data.Length; i++)
                        {
                            argb[2 * i + 0] = (byte)(data[i] & 0xf0);
                            argb[2 * i + 1] = (byte)((data[i] & 0x0f) << 4);
                        }

                        break;
                    case ZTEX_FORMAT.ZTEXFMT_A1R5G5B5:
                        format = PixelFormat.Format16bppArgb1555;
                        break;
                    case ZTEX_FORMAT.ZTEXFMT_R5G6B5:
                        format = PixelFormat.Format16bppRgb565;
                        break;
                    case ZTEX_FORMAT.ZTEXFMT_P8:
                        format = PixelFormat.Format24bppRgb;
                        byte[] rgb = new byte[data.Length * 3];

                        for (int i = 0; i < data.Length; i++)
                        {
                            rgb[3 * i + 0] = pallete[3 * data[i] + 0];
                            rgb[3 * i + 1] = pallete[3 * data[i] + 1];
                            rgb[3 * i + 2] = pallete[3 * data[i] + 2];
                        }

                        data = rgb;
                        break;
                }

                // Převod na Image objekt pomocí GDI+ knihovny
                Bitmap image = new Bitmap(w, h, format);
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                BitmapData bitmapData = image.LockBits(rect, ImageLockMode.WriteOnly, image.PixelFormat);
                Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
                image.UnlockBits(bitmapData);

                return image;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Přeskočí všechny menší mipmapy.
        /// </summary>
        static void SkipMipMaps(BinaryReader reader, ZTEX_FILE_HEADER header)
        {
            int n = header.TexInfo.MipMaps;

            if (n == 1)
                return;

            // n-tá odmocnina
            int w = (int)Math.Pow(header.TexInfo.Width, 1d / n);
            int h = (int)Math.Pow(header.TexInfo.Height, 1d / n);

            // Součet h^i * w^i, kde i jde od 1 do n - 1
            int pixels = ((int)Math.Pow(h * w, n) - (h * w)) / (h * w - 1);
            int bytesPerPixel = BytesPerPixel(header.TexInfo.Format);

            reader.ReadBytes(bytesPerPixel * pixels);
        }


        /// <summary>
        /// Vrátí počet bitů na jeden pixel zadaného formátu.
        /// </summary>
        static int BytesPerPixel(ZTEX_FORMAT format)
        {
            switch (format)
            {
                case ZTEX_FORMAT.ZTEXFMT_B8G8R8A8:
                case ZTEX_FORMAT.ZTEXFMT_R8G8B8A8:
                case ZTEX_FORMAT.ZTEXFMT_A8B8G8R8:
                case ZTEX_FORMAT.ZTEXFMT_A8R8G8B8: return 4;
                case ZTEX_FORMAT.ZTEXFMT_B8G8R8:
                case ZTEX_FORMAT.ZTEXFMT_R8G8B8: return 3;
                case ZTEX_FORMAT.ZTEXFMT_A4R4G4B4:
                case ZTEX_FORMAT.ZTEXFMT_A1R5G5B5:
                case ZTEX_FORMAT.ZTEXFMT_R5G6B5: return 2;
                case ZTEX_FORMAT.ZTEXFMT_P8: return 1;
                default: return 0;
            }
        }

        /// <summary>
        /// Vrátí hlavičku ZTEX formátu.
        /// </summary>
        /// <param name="reader">
        /// <see cref="BinaryReader"/> ukazující na začátek souboru.
        /// Po načtení potřebných dat bude ukazovat za hlavičku ZTEX souboru.</param>
        static ZTEX_FILE_HEADER ReadZTEXHeader(BinaryReader reader)
        {
            ZTEX_FILE_HEADER header = new ZTEX_FILE_HEADER();

            header.Signature = reader.ReadInt32();

            if (header.Signature != ZTEX_FILE_HEADER.SignatureLittleEndian && header.Signature != ZTEX_FILE_HEADER.SignatureBigEndian)
                throw new Exception($"Invalid signature 0x{header.Signature:x}.");

            bool isLittleEndian = header.Signature == ZTEX_FILE_HEADER.SignatureLittleEndian;

            header.Version = reader.ReadInt32().CorrectEndianness(isLittleEndian);

            if (header.Version != ZTEX_FILE_HEADER.SupportedVersion)
                throw new Exception($"Unsuppored version {header.Version}.");

            header.TexInfo.Format = (ZTEX_FORMAT)reader.ReadInt32().CorrectEndianness(isLittleEndian);

            if (header.TexInfo.Format >= ZTEX_FORMAT.ZTEXFMT_COUNT)
                throw new Exception($"Invalid format 0x{header.TexInfo.Format:x}.");

            header.TexInfo.Width = reader.ReadInt32().CorrectEndianness(isLittleEndian);
            header.TexInfo.Height = reader.ReadInt32().CorrectEndianness(isLittleEndian);
            header.TexInfo.MipMaps = reader.ReadInt32().CorrectEndianness(isLittleEndian);
            header.TexInfo.RefWidth = reader.ReadInt32().CorrectEndianness(isLittleEndian);
            header.TexInfo.RefHeight = reader.ReadInt32().CorrectEndianness(isLittleEndian);
            header.TexInfo.AvgColor = reader.ReadInt32().CorrectEndianness(isLittleEndian);

            return header;
        }

        /// <summary>
        /// Opraví endianitu u načtených čísel.
        /// </summary>
        static int CorrectEndianness(this int value, bool isLittleEndian)
        {
            if (BitConverter.IsLittleEndian == isLittleEndian)
                return value;

            return value; // TODO
        }
        #endregion
    }
    #endregion

    #region Enums
    /// <summary>
    /// ZenGin Texture - Render Formats
    /// </summary>
    enum ZTEX_FORMAT
    {
        /// <summary>
        /// 32-bit ARGB pixel format with alpha, using 8 bits per channel.
        /// </summary>
        ZTEXFMT_B8G8R8A8,
        /// <summary>
        /// 32-bit ARGB pixel format with alpha, using 8 bits per channel.
        /// </summary>
        ZTEXFMT_R8G8B8A8,
        /// <summary>
        /// 32-bit ARGB pixel format with alpha, using 8 bits per channel.
        /// </summary>
        ZTEXFMT_A8B8G8R8,
        /// <summary>
        /// 32-bit ARGB pixel format with alpha, using 8 bits per channel.
        /// </summary>
        ZTEXFMT_A8R8G8B8,
        /// <summary>
        /// 24-bit RGB pixel format with 8 bits per channel.
        /// </summary>
        ZTEXFMT_B8G8R8,
        /// <summary>
        /// 24-bit RGB pixel format with 8 bits per channel.
        /// </summary>
        ZTEXFMT_R8G8B8,
        /// <summary>
        /// 16-bit ARGB pixel format with 4 bits for each channel.
        /// </summary>
        ZTEXFMT_A4R4G4B4,
        /// <summary>
        /// 16-bit pixel format where 5 bits are reserved for each color and 1 bit is reserved for alpha.
        /// </summary>
        ZTEXFMT_A1R5G5B5,
        /// <summary>
        /// 16-bit RGB pixel format with 5 bits for red, 6 bits for green, and 5 bits for blue.
        /// </summary>
        ZTEXFMT_R5G6B5,
        /// <summary>
        /// 8-bit color indexed.
        /// </summary>
        ZTEXFMT_P8,
        /// <summary>
        /// DXT1 compression texture format.
        /// </summary>
        ZTEXFMT_DXT1,
        /// <summary>
        /// DXT2 compression texture format.
        /// </summary>
        ZTEXFMT_DXT2,
        /// <summary>
        /// DXT3 compression texture format.
        /// </summary>
        ZTEXFMT_DXT3,
        /// <summary>
        /// DXT4 compression texture format.
        /// </summary>
        ZTEXFMT_DXT4,
        /// <summary>
        /// DXT5 compression texture format.
        /// </summary>
        ZTEXFMT_DXT5,
        ZTEXFMT_COUNT
    }
    #endregion

    #region Internal Classes
    class ZTEX_INFO
    {
        public ZTEX_FORMAT Format { get; set; }

        /// <summary>
        /// Šířka mipmapy 0 (největší).
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Výška mipmapy 0 (největší).
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Počet mipmap (včetně mipmap 0).
        /// </summary>
        public int MipMaps { get; set; }

        // Šířka objektu ve hře.
        public int RefWidth { get; set; }

        /// <summary>
        /// Výška objektu ve hře.
        /// </summary>
        public int RefHeight { get; set; }

        /// <summary>
        /// Průměrná barva ve formátu A8R8G8B8.
        /// </summary>
        public int AvgColor { get; set; }
    }

    class ZTEX_FILE_HEADER
    {
        /// <summary>
        /// 'XETZ' (little-endian).
        /// </summary>
        public static int SignatureLittleEndian => 0x5845545A;

        /// <summary>
        /// 'ZTEX' (big-endian).
        /// </summary>
        public static int SignatureBigEndian => 0x5A544558;

        /// <summary>
        /// Podporovaná verze formátu ZTEX.
        /// </summary>
        public static int SupportedVersion => 0x00000000;

        /// <summary>
        /// Velikost palety barev, pokud se používají indexy barev.
        /// </summary>
        public static int PalleteSize => 0x100;

        /// <summary>
        /// ASCII řetězec 'ZTEX' nebo 'XETZ' značící typ souboru.
        /// </summary>
        public int Signature { get; set; }

        /// <summary>
        /// Verze formátu ZTEX.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Další informace o souboru.
        /// </summary>
        public ZTEX_INFO TexInfo { get; set; } = new ZTEX_INFO();
    }
    #endregion
}
