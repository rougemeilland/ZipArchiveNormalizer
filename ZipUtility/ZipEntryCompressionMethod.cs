using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ZipUtility.Compression;

namespace ZipUtility
{
    public class ZipEntryCompressionMethod
    {
        private static Regex _pluginFileNamePattern;
        private static IDictionary<CompressionMethodId, ICompressionMethod> _compresssionMethods;
        private static ZipEntryCompressionMethod _stored;
        private static ZipEntryCompressionMethod _deflateWithNormal;
        private static ZipEntryCompressionMethod _deflateWithMaximum;
        private static ZipEntryCompressionMethod _deflateWithFast;
        private static ZipEntryCompressionMethod _deflateWithSuperFast;
        private static ZipEntryCompressionMethod _deflate64WithNormal;
        private static ZipEntryCompressionMethod _deflate64WithMaximum;
        private static ZipEntryCompressionMethod _deflate64WithFast;
        private static ZipEntryCompressionMethod _deflate64WithSuperFast;
        private static ZipEntryCompressionMethod _bzip2;
        private static ZipEntryCompressionMethod _lzmaWithEOS;
        private static ZipEntryCompressionMethod _lzmaWithoutEOS;
        private static ZipEntryCompressionMethod _ppmd;
        private ICompressionMethod _plugin;
        private ICompressionOption _option;

        static ZipEntryCompressionMethod()
        {
            _pluginFileNamePattern = new Regex(@"^ZipUtility\.Compression\.[a-zA-Z0-9_]+\.dll$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _compresssionMethods = EnumeratePlugin();
            ICompressionMethod plugin;

            if (_compresssionMethods.TryGetValue(Compression.CompressionMethodId.Stored, out plugin))
                _stored = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Stored, plugin, null);
            else
                _stored = null;

            if (_compresssionMethods.TryGetValue(Compression.CompressionMethodId.Deflate, out plugin))
            {
                _deflateWithNormal = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate, plugin, new DeflateCompressionOption { CompressionLevel =  DeflateCompressionLevel.Normal });
                _deflateWithMaximum = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Maximum });
                _deflateWithFast = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Fast });
                _deflateWithSuperFast = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.SuperFast });
            }
            else
            {
                _deflateWithNormal = null;
                _deflateWithMaximum = null;
                _deflateWithFast = null;
                _deflateWithSuperFast = null;
            }

            if (_compresssionMethods.TryGetValue(Compression.CompressionMethodId.Deflate64, out plugin))
            {
                _deflate64WithNormal = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate64, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Normal });
                _deflate64WithMaximum = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate64, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Maximum });
                _deflate64WithFast = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate64, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.Fast });
                _deflate64WithSuperFast = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.Deflate64, plugin, new DeflateCompressionOption { CompressionLevel = DeflateCompressionLevel.SuperFast });
            }
            else
            {
                _deflate64WithNormal = null;
                _deflate64WithMaximum = null;
                _deflate64WithFast = null;
                _deflate64WithSuperFast = null;
            }

            if (_compresssionMethods.TryGetValue(Compression.CompressionMethodId.BZIP2, out plugin))
                _bzip2 = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.BZIP2, plugin, null);
            else
                _bzip2 = null;

            if (_compresssionMethods.TryGetValue(Compression.CompressionMethodId.LZMA, out plugin))
            {
                _lzmaWithEOS = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.LZMA, plugin, new LzmaCompressionOption {  UseEndOfStreamMarker = true   });
                _lzmaWithoutEOS = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.LZMA, plugin, new LzmaCompressionOption {  UseEndOfStreamMarker = false });
            }
            else
            {
                _lzmaWithEOS = null;
                _lzmaWithoutEOS = null;
            }

            if (_compresssionMethods.TryGetValue(Compression.CompressionMethodId.PPMd, out plugin))
                _ppmd = new ZipEntryCompressionMethod(ZipEntryCompressionMethodId.PPMd, plugin, null);
            else
                _ppmd = null;
        }

        internal ZipEntryCompressionMethod(ZipEntryCompressionMethodId compressMethodId, ICompressionMethod plugin, ICompressionOption option)
        {
            if (plugin == null)
                throw new ArgumentNullException();
            CompressionMethodId = compressMethodId;
            _plugin = plugin;
            _option = option;
        }

        public static IEnumerable<ZipEntryCompressionMethodId> SupportedCompresssionMethodIds => _compresssionMethods.Keys.Select(id => GetCompressionMethodId(id));
        public static ZipEntryCompressionMethod Stored => _stored ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Stored);
        public static ZipEntryCompressionMethod DeflateWithNormal => _deflateWithNormal ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate);
        public static ZipEntryCompressionMethod DeflateWithMaximum => _deflateWithMaximum ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate);
        public static ZipEntryCompressionMethod DeflateWithFast => _deflateWithFast ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate);
        public static ZipEntryCompressionMethod DeflateWithSuperFast => _deflateWithSuperFast ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate);
        public static ZipEntryCompressionMethod Deflate64WithNormal => _deflate64WithNormal ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate64);
        public static ZipEntryCompressionMethod Deflate64WithMaximum => _deflate64WithMaximum ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate64);
        public static ZipEntryCompressionMethod Deflate64WithFast => _deflate64WithFast ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate64);
        public static ZipEntryCompressionMethod Deflate64WithSuperFast => _deflate64WithSuperFast ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.Deflate64);
        public static ZipEntryCompressionMethod BZIP2 => _bzip2 ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.BZIP2);
        public static ZipEntryCompressionMethod LZMAWithEOS => _lzmaWithEOS ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.LZMA);
        public static ZipEntryCompressionMethod LZMAWithoutEOS => _lzmaWithoutEOS ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.LZMA);
        public static ZipEntryCompressionMethod PPMd => _ppmd ?? throw new CompressionMethodNotSupportedException(ZipEntryCompressionMethodId.PPMd);

        public ZipEntryCompressionMethodId CompressionMethodId { get; }

        public Stream GetInputStream(Stream baseStream, long packedSize, long size, bool leaveOpen = false)
        {
            try
            {
                return _plugin.GetInputStream(baseStream, _option, null, packedSize, size, leaveOpen);
            }
            catch (IOException ex)
            {
                throw new BadZipFileFormatException(string.Format("Failed to decompress: method='{0}'", CompressionMethodId), ex);
            }
        }

        public Stream GetInputStream(Stream baseStream, long offset, long packedSize, long size, bool leaveOpen = false)
        {
            try
            {
                return _plugin.GetInputStream(baseStream, _option, offset, packedSize, size, leaveOpen);
            }
            catch (IOException ex)
            {
                throw new BadZipFileFormatException(string.Format("Failed to decompress: method='{0}'", CompressionMethodId), ex);
            }
        }

        public Stream GetOutputStream(Stream baseStream, bool leaveOpen = false)
        {
            try
            {
                return _plugin.GetOutputStream(baseStream, _option, null, null, leaveOpen);
            }
            catch (IOException ex)
            {
                throw new BadZipFileFormatException(string.Format("Failed to compress: method='{0}'", CompressionMethodId), ex);
            }
        }

        public Stream GetOutputStream(Stream baseStream, long offset, bool leaveOpen = false)
        {
            try
            {
                return _plugin.GetOutputStream(baseStream, _option, offset, null, leaveOpen);
            }
            catch (IOException ex)
            {
                throw new BadZipFileFormatException(string.Format("Failed to compress: method='{0}'", CompressionMethodId), ex);
            }
        }

        public Stream GetOutputStream(Stream baseStream, long offset, long size, bool leaveOpen = false)
        {
            try
            {
                return _plugin.GetOutputStream(baseStream, _option, offset, size, leaveOpen);
            }
            catch (IOException ex)
            {
                throw new BadZipFileFormatException(string.Format("Failed to compress: method='{0}'", CompressionMethodId), ex);
            }
        }

        internal static ZipEntryCompressionMethod GetCompressionMethod(ZipEntryCompressionMethodId compressionMethodId, ZipEntryGeneralPurposeBitFlag flag)
        {
            var pluginId = GetPluginId(compressionMethodId);

            ICompressionMethod plugin;
            if (!_compresssionMethods.TryGetValue(pluginId, out plugin))
                throw new CompressionMethodNotSupportedException(compressionMethodId);

            var option =
                plugin.CreateOptionFromGeneralPurposeFlag(
                    flag.HasFlag(ZipEntryGeneralPurposeBitFlag.CompresssionOption0),
                    flag.HasFlag(ZipEntryGeneralPurposeBitFlag.CompresssionOption1));
            return new ZipEntryCompressionMethod(compressionMethodId, plugin, option);
        }

        private static IDictionary<CompressionMethodId, ICompressionMethod> EnumeratePlugin()
        {

            var interfaceType = typeof(ICompressionMethod);
            var interfaceTypeName = interfaceType.FullName;
            var thisAssembly = typeof(ZipEntryCompressionMethodIdExtensions).Assembly;
            var pluginLocation = Path.GetDirectoryName(thisAssembly.Location);
            return
                Directory.EnumerateFiles(pluginLocation, "*.dll", SearchOption.AllDirectories)
                    .Where(filePath =>
                        _pluginFileNamePattern.IsMatch(Path.GetFileName(filePath)) &&
                        !string.Equals(filePath, thisAssembly.Location, StringComparison.OrdinalIgnoreCase))
                    .Select(filePath =>
                    {
                        try
                        {
                            return Assembly.LoadFile(filePath);
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    })
                    .Where(assembly => assembly != null)
                    .Select(assembly =>
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("loaded {0}", assembly.Location));
#endif
                        return new { assembly, externalAssembly = true };
                    })
                    .Concat(new[]
                    {
                        new { assembly = thisAssembly, externalAssembly = false },
                        new { assembly = interfaceType.Assembly, externalAssembly = true },
                    })
                    .SelectMany(item =>
                        item.assembly.GetTypes()
                        .Where(type =>
                            type.IsClass == true &&
                            (type.IsPublic == true || item.externalAssembly == false) &&
                            type.IsAbstract == false &&
                            type.GetInterface(interfaceTypeName) != null)
                        .Select(type =>
                        {
                            try
                            {
#if DEBUG
                                System.Diagnostics.Debug.WriteLine(string.Format("create plugin {0}", type.FullName));
#endif
                                return item.assembly.CreateInstance(type.FullName) as ICompressionMethod;
                            }
                            catch (Exception)
                            {
                                return null;
                            }
                        })
                        .Where(plugin => plugin != null && plugin.CompressionMethodId != Compression.CompressionMethodId.Unknown))
                    .ToDictionary(plugin => plugin.CompressionMethodId, plugin => plugin);
        }

        private static ZipUtility.Compression.CompressionMethodId GetPluginId(ZipEntryCompressionMethodId compressionMethodId)
        {
            switch (compressionMethodId)
            {
                case ZipEntryCompressionMethodId.Stored:
                    return ZipUtility.Compression.CompressionMethodId.Stored;
                case ZipEntryCompressionMethodId.Deflate:
                    return ZipUtility.Compression.CompressionMethodId.Deflate;
                case ZipEntryCompressionMethodId.Deflate64:
                    return ZipUtility.Compression.CompressionMethodId.Deflate64;
                case ZipEntryCompressionMethodId.BZIP2:
                    return ZipUtility.Compression.CompressionMethodId.BZIP2;
                case ZipEntryCompressionMethodId.LZMA:
                    return ZipUtility.Compression.CompressionMethodId.LZMA;
                case ZipEntryCompressionMethodId.PPMd:
                    return ZipUtility.Compression.CompressionMethodId.PPMd;
                default:
                    return ZipUtility.Compression.CompressionMethodId.Unknown;
            }
        }

        private static ZipEntryCompressionMethodId GetCompressionMethodId(ZipUtility.Compression.CompressionMethodId  pluginId)
        {
            switch (pluginId)
            {
                case ZipUtility.Compression.CompressionMethodId.Stored:
                    return ZipEntryCompressionMethodId.Stored;
                case ZipUtility.Compression.CompressionMethodId.Deflate:
                    return ZipEntryCompressionMethodId.Deflate;
                case ZipUtility.Compression.CompressionMethodId.Deflate64:
                    return ZipEntryCompressionMethodId.Deflate64;
                case ZipUtility.Compression.CompressionMethodId.BZIP2:
                    return ZipEntryCompressionMethodId.BZIP2;
                case ZipUtility.Compression.CompressionMethodId.LZMA:
                    return ZipEntryCompressionMethodId.LZMA;
                case ZipUtility.Compression.CompressionMethodId.PPMd:
                    return ZipEntryCompressionMethodId.PPMd;
                default:
                    return ZipEntryCompressionMethodId.Unknown;
            }
        }
    }
}