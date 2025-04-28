//******************************************************************************************************
//  SecureStreamClient.cs - Gbtc
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

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Gemstone.Diagnostics;
using Gemstone.IO.StreamExtensions;
using Org.BouncyCastle.Crypto;
using SnapDB.Security.Authentication;

namespace SnapDB.Security;

/// <summary>
/// Creates a secure stream that connects to a server.
/// </summary>
public abstract class SecureStreamClientBase : DisposableLoggingClassBase
{
    #region [ Members ]

    private byte[] m_resumeTicket;
    private byte[] m_sessionSecret;

    #endregion

    #region [ Constructors ]

    /// <summary>
    /// Initializes a new instance of the <see cref="SecureStreamClientBase"/> class.
    /// This constructor is protected and internally accessible within the component's message class.
    /// </summary>
    protected internal SecureStreamClientBase() : base(MessageClass.Component)
    {
    }

    #endregion

    #region [ Methods ]

    /// <summary>
    /// Attempts to authenticate the supplied network stream, optionally using SSL/TLS encryption.
    /// </summary>
    /// <param name="stream">The stream to authenticate.</param>
    /// <param name="useSsl"><c>true</c> if SSL is to be used; otherwise, <c>false</c>.</param>
    /// <param name="secureStream">
    /// When successful, contains the authenticated and optionally encrypted stream.</param>
    /// <returns>
    /// <c>true</c> if authentication is successful and if SSL/TLS encryption is used and successful; otherwise, <c>false</c>.
    /// </returns>
    public bool TryAuthenticate(Stream stream, bool useSsl, out Stream? secureStream)
    {
        secureStream = null;
        SslStream? ssl = null;
        Stream stream2;
        byte[] certSignatures;

        if (useSsl)
        {
            if (!TryConnectSsl(stream, out ssl))
                return false;

            stream2 = ssl;
            certSignatures = SecureStream.ComputeCertificateChallenge(false, ssl);
        }

        else
        {
            stream2 = stream;
            certSignatures = [];
        }

        try
        {
            try
            {
                if (TryResumeSession(ref secureStream, stream2, certSignatures))
                    return true;
            }
            catch (FileNotFoundException ex)
            {
                Log.Publish(MessageLevel.Info, "Bouncy Castle dll is missing! Oh No!", null, null, ex);
            }


            if (InternalTryAuthenticate(stream2, certSignatures))
            {
                if (stream2.ReadBoolean())
                {
                    m_resumeTicket = stream2.ReadBytes(stream2.ReadNextByte());
                    m_sessionSecret = stream2.ReadBytes(stream2.ReadNextByte());
                }

                secureStream = stream2;
                return true;
            }

            ssl?.Dispose();
            return false;
        }

        catch (Exception ex)
        {
            Log.Publish(MessageLevel.Info, "Authentication Failed", null, null, ex);
            ssl?.Dispose();

            return false;
        }
    }

    /// <summary>
    /// Attempts to authenticate the provided stream, disposing the secure stream upon completion.
    /// </summary>
    /// <param name="stream">The stream to authenticate.</param>
    /// <param name="useSsl">Gets if SSL will be used to authenticate.</param>
    /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
    public bool TryAuthenticate(Stream stream, bool useSsl = true)
    {
        Stream? secureStream = null;

        try
        {
            return TryAuthenticate(stream, useSsl, out secureStream);
        }
        finally
        {
            secureStream?.Dispose();
        }
    }

    /// <summary>
    /// Authenticates the supplied stream. Returns the secure stream.
    /// </summary>
    /// <param name="stream">The stream to authenticate.</param>
    /// <param name="useSsl">
    /// Indicates whether to use SSL for secure communication. Defaults to true if not specified.
    /// </param>
    /// <returns>
    /// A secure stream if authentication succeeds; otherwise, an exception is thrown.
    /// </returns>
    public Stream Authenticate(Stream stream, bool useSsl = true)
    {
        Stream? secureStream = null;

        try
        {
            if (TryAuthenticate(stream, useSsl, out secureStream))
                return secureStream;

            throw new AuthenticationException("Authentication Failed");
        }
        catch
        {
            secureStream?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Attempts to authenticate a network stream with the provided certificate signatures.
    /// </summary>
    /// <param name="stream">The network stream to authenticate.</param>
    /// <param name="certSignatures">The certificate signatures used for authentication.</param>
    /// <returns><c>true</c> if authentication was successful; otherwise, <c>false</c>.</returns>
    protected abstract bool InternalTryAuthenticate(Stream stream, byte[] certSignatures);

    private bool TryConnectSsl(Stream stream, out SslStream ssl)
    {
        ssl = new SslStream(stream, false, UserCertificateValidationCallback, UserCertificateSelectionCallback, EncryptionPolicy.RequireEncryption);

        try
        {
            ssl.AuthenticateAsClient("Local", null, SslProtocols.Tls12, false);
        }
        catch (Exception ex)
        {
            Log.Publish(MessageLevel.Info, "Authentication Failed", null, null, ex);
            
            ssl.Dispose();
            ssl = null;

            return false;
        }

        return true;
    }

    private X509Certificate UserCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
    {
        return s_tempCert;
    }

    private bool UserCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    private bool TryResumeSession(ref Stream secureStream, Stream stream2, byte[] certSignatures)
    {
    #if SQLCLR
        return false;
    #else
        if (m_resumeTicket is not null && m_sessionSecret is not null)
        {
            //Resume Session:
            // C => S
            // byte    ResumeSession
            // byte    TicketLength
            // byte[]  Ticket
            // byte    ClientChallengeLength
            // byte[]  ClientChallenge

            byte[] clientChallenge = SaltGenerator.Create(16);
            stream2.WriteByte((byte)AuthenticationMode.ResumeSession);
            stream2.WriteByte((byte)m_resumeTicket.Length);
            stream2.Write(m_resumeTicket);
            stream2.WriteByte((byte)clientChallenge.Length);
            stream2.Write(clientChallenge);
            stream2.Flush();

            // S <= C
            // byte    HashMethod
            // byte    ServerChallengeLength
            // byte[]  ServerChallenge

            HashMethod hashMethod = (HashMethod)stream2.ReadNextByte();
            IDigest hash = Scram.CreateDigest(hashMethod);
            byte[] serverChallenge = stream2.ReadBytes(stream2.ReadNextByte());

            // C => S
            // byte    ClientResponseLength
            // byte[]  ClientChallenge

            byte[] clientResponse = hash.ComputeHash(serverChallenge, clientChallenge, m_sessionSecret, certSignatures);
            byte[] serverResponse = hash.ComputeHash(clientChallenge, serverChallenge, m_sessionSecret, certSignatures);

            stream2.WriteByte((byte)clientResponse.Length);
            stream2.Write(clientResponse);
            stream2.Flush();

            // S => C
            // bool   IsSuccessful
            // byte   ServerResponseLength
            // byte[] ServerResponse

            if (stream2.ReadBoolean())
            {
                byte[] serverResponseCheck = stream2.ReadBytes(stream2.ReadNextByte());

                // C => S
                // bool   IsSuccessful
                if (serverResponse.SecureEquals(serverResponseCheck))
                {
                    stream2.Write(true);
                    stream2.Flush();
                    secureStream = stream2;

                    return true;
                }

                stream2.Write(false);
                stream2.Flush();
            }

            m_resumeTicket = null;
            m_sessionSecret = null;
        }

        return false;
    #endif
    }

    #endregion

    #region [ Static ]

    private static readonly X509Certificate2 s_tempCert;

    static SecureStreamClientBase()
    {
#if !SQLCLR
#if !DEBUG
        try
#endif
        {
            s_tempCert = GenerateCertificate.CreateSelfSignedCertificate("CN=Local", 256, 1024);
        }
#if !DEBUG
        catch (Exception ex)
        {
            Logger.SwallowException(ex);
        }
#endif
#endif
    }

    #endregion
}