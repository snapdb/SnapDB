//
// Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. 
// (http://www.bouncycastle.org)

// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the 
// following conditions:

// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using SnapDB.Security.Authentication;

// ReSharper disable once CheckNamespace
namespace Org.BouncyCastle.Crypto.Agreement.Srp;

/**
 * Implements the client side SRP-6a protocol. Note that this class is stateful, and therefore NOT threadsafe.
 * This implementation of SRP is based on the optimized message sequence put forth by Thomas Wu in the paper
 * "SRP-6: Improvements and Refinements to the Secure Remote Password Protocol, 2002"
 */
internal class Srp6Client
{
    #region [ Members ]

    protected BigInteger B;

    protected BigInteger PrivA;
    protected BigInteger PubA;

    protected SecureRandom Random;
    protected BigInteger S;
    protected BigInteger U;

    protected BigInteger X;

    private readonly SrpConstants m_param;

    #endregion

    #region [ Constructors ]

    public Srp6Client(SrpConstants param)
    {
        m_param = param;
        Random = new SecureRandom();
    }

    #endregion

    #region [ Methods ]

    /**
     * Generates client's credentials given the client's salt, identity and password
     * @param salt The salt used in the client's verifier.
     * @param identity The user's identity (eg. username)
     * @param password The user's password
     * @return Client's public value to send to server
     */
    public virtual BigInteger GenerateClientCredentials(IDigest digest, byte[] salt, byte[] identity, byte[] password)
    {
        X = Srp6Utilities.CalculateX(digest, m_param.N, salt, identity, password);
        PrivA = SelectPrivateValue();
        PubA = m_param.G.ModPow(PrivA, m_param.N);

        return PubA;
    }

    /**
     * Generates client's verification message given the server's credentials
     * @param serverB The server's credentials
     * @return Client's verification message for the server
     * @throws CryptoException If server's credentials are invalid
     */
    public virtual BigInteger CalculateSecret(IDigest digest, BigInteger serverB)
    {
        B = Srp6Utilities.ValidatePublicValue(m_param.N, serverB);
        U = Srp6Utilities.CalculateU(digest, m_param.N, PubA, B);
        S = CalculateS();

        return S;
    }

    protected virtual BigInteger SelectPrivateValue()
    {
        return Srp6Utilities.GeneratePrivateValue(m_param.N, Random);
    }

    private BigInteger CalculateS()
    {
        BigInteger exp = U.Multiply(X).Add(PrivA);
        BigInteger tmp = m_param.G.ModPow(X, m_param.N).Multiply(m_param.K).Mod(m_param.N);
        return B.Subtract(tmp).Mod(m_param.N).ModPow(exp, m_param.N);
    }

    #endregion
}