using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NL.MinVWS.Encoding;
using Org.BouncyCastle.Crypto;

namespace DGC
{
    public class GreenCertificateEncoder
    {
        private readonly AsymmetricCipherKeyPair keypair;

        public GreenCertificateEncoder(AsymmetricCipherKeyPair keypair)
        {
            this.keypair = keypair;
        }

        public string Encode(CWT cwt)
        {
            var cwtBytes = cwt.EncodeToBytes();
            var coseBytes = GetCOSEBytes(cwtBytes);
            var commpressed = GetCompressedBytes(coseBytes);
            return GetBase45(commpressed);
        }

        private string GetBase45(byte[] deflateBytes)
        {
            return "HC1:" + Base45Encoding.Encode(deflateBytes);
        }

        private byte[] GetCompressedBytes(byte[] buffer)
        {
            using (var inputStream = new MemoryStream(buffer))
            using (var outStream = new MemoryStream())
            using (var deflateStream = new DeflaterOutputStream(outStream, new Deflater(Deflater.BEST_COMPRESSION)))
            {
                inputStream.CopyTo(deflateStream);
                deflateStream.Finish();
                return outStream.ToArray();
            }
        }

        private byte[] GetCOSEBytes(byte[] cborBytes)
        {
            var msg = new Sign1CoseMessage();
            msg.Content = cborBytes;

            if (keypair.Private is Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)
            {
                msg.Sign(keypair, HCertSupportedAlgorithm.PS256);
            }
            else if (keypair.Private is Org.BouncyCastle.Crypto.Parameters.ECKeyParameters)
            {
                msg.Sign(keypair, HCertSupportedAlgorithm.ES256);
            }
            else
            {
                throw new System.NotSupportedException("Private key algorithm not supported");
            }

            return msg.EncodeToBytes();
        }
    }
}