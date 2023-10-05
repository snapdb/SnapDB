﻿//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//******************************************************************************************************
//  GenerateCertificate.cs - Gbtc
//
//  Copyright © 2014, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/29/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/18/2023 - Lillian Gensolin
//       Converted code to .NET core.
//
//******************************************************************************************************

using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace SnapDB.Security;

//http://stackoverflow.com/questions/3770233/is-it-possible-to-programmatically-generate-an-x509-certificate-using-only-c/3771913#3771913
//http://stackoverflow.com/questions/22230745/generate-self-signed-certificate-on-the-fly
//https://blog.differentpla.net/post/53/how-do-i-create-a-self-signed-certificate-using-bouncy-castle-
//https://blog.differentpla.net/post/20/how-do-i-convert-a-bouncy-castle-certificate-to-a-net-certificate-
//http://www.fkollmann.de/v2/post/Creating-certificates-using-BouncyCastle.aspx

/// <summary>
/// Generates <see cref="X509Certificate2"/>s.
/// </summary>
public static class GenerateCertificate
{
    #region [ Static ]

    /// <summary>
    /// Opens a certificate, loading the private key of the PFX file.
    /// </summary>
    /// <param name="fileName">The path to the X.509 certificate file.</param>
    /// <param name="password">The password to decrypt the private key (if encrypted).</param>
    /// <returns>
    /// An <see cref="X509Certificate2"/> object representing the certificate loaded from the file.
    /// </returns>
    public static X509Certificate2 OpenCertificate(string fileName, string password)
    {
        return new X509Certificate2(fileName, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
    }

    /// <summary>
    /// Creates new certificate.
    /// </summary>
    public static void CreateSelfSignedCertificate(string subjectDirName, DateTime startDate, DateTime endDate, int signatureBits, int keyStrength, string password, string fileName)
    {
        switch (signatureBits)
        {
            case 160:
                break;
            case 224:
                break;
            case 256:
                break;
            case 384:
                break;
            case 512:
                break;
            default:
                throw new ArgumentException("Invalid signature bit size.", nameof(signatureBits));
        }

        // Generating Random Numbers
        CryptoApiRandomGenerator randomGenerator = new();
        SecureRandom random = new(randomGenerator);

        // Generate public/private keys.

        KeyGenerationParameters keyGenerationParameters = new(random, keyStrength);
        RsaKeyPairGenerator keyPairGenerator = new();
        keyPairGenerator.Init(keyGenerationParameters);
        AsymmetricCipherKeyPair encryptionKeys = keyPairGenerator.GenerateKeyPair();

        // The Certificate Generator
        X509V3CertificateGenerator certificateGenerator = new();
        certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random));

        // TODO: JRC - check to see what has changed here and if this is necessary
        //certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

        certificateGenerator.SetIssuerDN(new X509Name(subjectDirName));
        certificateGenerator.SetSubjectDN(new X509Name(subjectDirName));
        certificateGenerator.SetNotBefore(startDate);
        certificateGenerator.SetNotAfter(endDate);
        certificateGenerator.SetPublicKey(encryptionKeys.Public);

        // -- commented out due to Bouncy Castle changes --
        // self-sign certificate
        //Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(encryptionKeys.Private, random);

        // -- commented out due to Bouncy Castle changes --
        //Pkcs12Store store = new Pkcs12Store();
        //string friendlyName = certificate.SubjectDN.ToString();
        //X509CertificateEntry certificateEntry = new X509CertificateEntry(certificate);
        //store.SetCertificateEntry(friendlyName, certificateEntry);
        //store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(encryptionKeys.Private), new[] { certificateEntry });

        //MemoryStream stream = new();
        //store.Save(stream, password.ToCharArray(), random);

        // -- commented out due to Bouncy Castle changes --
        //Verify that the certificate is valid.
        //_ = new X509Certificate2(stream.ToArray(), password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

        // -- commented out due to Bouncy Castle changes --
        //Write the file.
        //File.WriteAllBytes(fileName, stream.ToArray());

        // -- commented out due to Bouncy Castle changes --
        //File.WriteAllBytes(Path.ChangeExtension(fileName, ".cer"), certificate.GetEncoded());
    }


    /// <summary>
    /// Creates a self signed certificate that can be used in SSL communications.
    /// </summary>
    /// <param name="subjectDirName">A valid DirName formated string. Example: CN=ServerName</param>
    /// <param name="signatureBits">Bitstrength of signature algorithm. Supported Lengths are 160,256, and 384 </param>
    /// <param name="keyStrength">RSA key strength. Typically a multiple of 1024.</param>
    /// <returns>
    /// An <see cref="X509Certificate2"/> object representing the self-signed certificate.
    /// </returns>
    public static X509Certificate2 CreateSelfSignedCertificate(string subjectDirName, int signatureBits, int keyStrength)
    {
        switch (signatureBits)
        {
            case 160:
                break;
            case 256:
                break;
            case 384:
                break;
            default:
                throw new ArgumentException("Invalid signature bit size.", nameof(signatureBits));
        }

        // Generating Random Numbers
        CryptoApiRandomGenerator randomGenerator = new();
        SecureRandom random = new(randomGenerator);

        // Generate public/private keys.

        KeyGenerationParameters keyGenerationParameters = new(random, keyStrength);
        RsaKeyPairGenerator keyPairGenerator = new();
        keyPairGenerator.Init(keyGenerationParameters);
        keyPairGenerator.GenerateKeyPair();

        // TODO: JRC - check to see what has changed here and if this is necessary (see above for the same)

        //// The Certificate Generator
        //X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();
        //certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random));
        //certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);
        //certificateGenerator.SetIssuerDN(new X509Name(subjectDirName));
        //certificateGenerator.SetSubjectDN(new X509Name(subjectDirName));
        //certificateGenerator.SetNotBefore(startDate);
        //certificateGenerator.SetNotAfter(endDate);
        //certificateGenerator.SetPublicKey(encryptionKeys.Public);

        //// selfsign certificate
        //Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(encryptionKeys.Private, random);

        //Pkcs12Store store = new Pkcs12Store();
        //string friendlyName = certificate.SubjectDN.ToString();
        //X509CertificateEntry certificateEntry = new X509CertificateEntry(certificate);
        //store.SetCertificateEntry(friendlyName, certificateEntry);
        //store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(encryptionKeys.Private), new[] { certificateEntry });

        //MemoryStream stream = new();
        //store.Save(stream, "".ToCharArray(), random);

        //Verify that the certificate is valid.
        //X509Certificate2 convertedCertificate = new(stream.ToArray(), "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

        //return convertedCertificate;

        return null;
    }

    #endregion

    //private static bool addCertToStore(X509Certificate2 cert, StoreName st, StoreLocation sl)
    //{
    //    bool bRet = false;

    //    try
    //    {
    //        X509Store store = new X509Store(st, sl);
    //        store.Open(OpenFlags.ReadWrite);
    //        store.Add(cert);

    //        store.Close();
    //    }
    //    catch (Exception ex)
    //    {
    //        System.Console.WriteLine(ex.ToString());
    //    }

    //    return bRet;
    //}
}