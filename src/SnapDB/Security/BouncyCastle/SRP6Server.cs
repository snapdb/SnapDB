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

namespace Org.BouncyCastle.Crypto.Agreement.Srp;

/**
 * Implements the server side SRP-6a protocol. Note that this class is stateful, and therefore NOT threadsafe.
 * This implementation of SRP is based on the optimized message sequence put forth by Thomas Wu in the paper
 * "SRP-6: Improvements and Refinements to the Secure Remote Password Protocol, 2002"
 */
internal class Srp6Server
{
    #region [ Members ]

    protected BigInteger A;

    protected BigInteger PrivB;
    protected BigInteger PubB;
    protected BigInteger S;

    protected BigInteger U;
    protected BigInteger V;

    private readonly SrpConstants m_param;

    #endregion

    #region [ Constructors ]

    /**
    * Initialises the server to accept a new client authentication attempt
    * @param N The safe prime associated with the client's verifier
    * @param g The group parameter associated with the client's verifier
    * @param v The client's verifier
    * @param digest The digest algorithm associated with the client's verifier
    * @param random For key generation
    */
    public Srp6Server(SrpConstants param, BigInteger v)
    {
        m_param = param;
        V = v;

    #pragma warning disable CS0436 // Type conflicts with imported type
        PrivB = Srp6Utilities.GeneratePrivateValue(param.N, new SecureRandom());
    #pragma warning restore CS0436 // Type conflicts with imported type
    }

    #endregion

    #region [ Methods ]

    /**
     * Generates the server's credentials that are to be sent to the client.
     * @return The server's public value to the client
     */
    public virtual BigInteger GenerateServerCredentials()
    {
        PubB = m_param.K.Multiply(V).Mod(m_param.N).Add(m_param.G.ModPow(PrivB, m_param.N)).Mod(m_param.N);
        return PubB;
    }

    /**
     * Processes the client's credentials. If valid the shared secret is generated and returned.
     * @param clientA The client's credentials
     * @return A shared secret BigInteger
     * @throws CryptoException If client's credentials are invalid
     */
    public virtual BigInteger CalculateSecret(IDigest digest, BigInteger clientA)
    {
        A = Srp6Utilities.ValidatePublicValue(m_param.N, clientA);
        U = Srp6Utilities.CalculateU(digest, m_param.N, A, PubB);
        S = V.ModPow(U, m_param.N).Multiply(A).Mod(m_param.N).ModPow(PrivB, m_param.N);
        return S;
    }

    #endregion
}