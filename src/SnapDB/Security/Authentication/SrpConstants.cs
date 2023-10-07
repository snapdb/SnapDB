﻿//******************************************************************************************************
//  SrpConstants.cs - Gbtc
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
//  07/27/2014 - Steven E. Chisholm
//       Generated original version of source code. 
//       
//  09/22/2023 - Lillian Gensolin
//      Converted code to .NET core.
//
//******************************************************************************************************

using System.ComponentModel;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Math;

namespace SnapDB.Security.Authentication;

/// <summary>
/// Supplies the base constants of SRP (Secure Remote Password) as supplied in RFC 5054 Appendix A.
/// </summary>
/// <remarks>
/// This implementation of SRP uses SHA-512 as the performance difference between
/// SHA1 and SHA-512 is negligible.
/// </remarks>
internal class SrpConstants
{
    #region [ Members ]

    public BigInteger G;
    public byte[] Gb;
    public BigInteger K;
    public byte[] Kb;

    /// <summary>
    /// H(N) xor H(g)
    /// </summary>
    public byte[] Kb2;

    public BigInteger N;
    public byte[] Nb;

    #endregion

    #region [ Constructors ]

    private SrpConstants(string hexN, string decG)
    {
        N = new BigInteger(hexN, 16);
        Nb = N.ToByteArrayUnsigned();
        G = new BigInteger(decG, 10);
        Gb = G.ToByteArrayUnsigned();
        Kb = ComputeHash(Nb, Gb);
        K = new BigInteger(1, Kb);
        Kb2 = Xor(ComputeHash(Nb), ComputeHash(Gb));
    }

    #endregion

    #region [ Static ]

    /// <summary>
    /// Contains the standard N, g, k parameters depending on the bit size
    /// as specified in RFC 5054 Appendix A.
    /// </summary>
    private static readonly Dictionary<int, SrpConstants> s_groupParameters = new();

    static SrpConstants()
    {
        SrpConstants c = new("EEAF0AB9ADB38DD69C33F80AFA8FC5E86072618775FF3C0B9EA2314C" + "9C256576D674DF7496EA81D3383B4813D692C6E0E0D5D8E250B98BE4" + "8E495C1D6089DAD15DC7D7B46154D6B6CE8EF4AD69B15D4982559B29" + "7BCF1885C529F566660E57EC68EDBC3C05726CC02FD4CBF4976EAA9A" + "FD5138FE8376435B9FC61D2FC0EB06E3", "2");
        s_groupParameters.Add(1024, c);

        c = new SrpConstants("9DEF3CAFB939277AB1F12A8617A47BBBDBA51DF499AC4C80BEEEA961" + "4B19CC4D5F4F5F556E27CBDE51C6A94BE4607A291558903BA0D0F843" + "80B655BB9A22E8DCDF028A7CEC67F0D08134B1C8B97989149B609E0B" + "E3BAB63D47548381DBC5B1FC764E3F4B53DD9DA1158BFD3E2B9C8CF5" + "6EDF019539349627DB2FD53D24B7C48665772E437D6C7F8CE442734A" + "F7CCB7AE837C264AE3A9BEB87F8A2FE9B8B5292E5A021FFF5E91479E" + "8CE7A28C2442C6F315180F93499A234DCF76E3FED135F9BB", "2");
        s_groupParameters.Add(1536, c);

        c = new SrpConstants("AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC319294" + "3DB56050A37329CBB4A099ED8193E0757767A13DD52312AB4B03310D" + "CD7F48A9DA04FD50E8083969EDB767B0CF6095179A163AB3661A05FB" + "D5FAAAE82918A9962F0B93B855F97993EC975EEAA80D740ADBF4FF74" + "7359D041D5C33EA71D281E446B14773BCA97B43A23FB801676BD207A" + "436C6481F1D2B9078717461A5B9D32E688F87748544523B524B0D57D" + "5EA77A2775D2ECFA032CFBDBF52FB3786160279004E57AE6AF874E73" + "03CE53299CCC041C7BC308D82A5698F3A8D0C38271AE35F8E9DBFBB6" + "94B5C803D89F7AE435DE236D525F54759B65E372FCD68EF20FA7111F" + "9E4AFF73", "2");
        s_groupParameters.Add(2048, c);

        c = new SrpConstants("FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" + "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B" + "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9" + "A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE6" + "49286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8" + "FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D" + "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C" + "180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF695581718" + "3995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D" + "04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7D" + "B3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D226" + "1AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200C" + "BBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFC" + "E0FD108E4B82D120A93AD2CAFFFFFFFFFFFFFFFF", "5");
        s_groupParameters.Add(3072, c);

        c = new SrpConstants("FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" + "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B" + "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9" + "A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE6" + "49286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8" + "FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D" + "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C" + "180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF695581718" + "3995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D" + "04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7D" + "B3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D226" + "1AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200C" + "BBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFC" + "E0FD108E4B82D120A92108011A723C12A787E6D788719A10BDBA5B26" + "99C327186AF4E23C1A946834B6150BDA2583E9CA2AD44CE8DBBBC2DB" + "04DE8EF92E8EFC141FBECAA6287C59474E6BC05D99B2964FA090C3A2" + "233BA186515BE7ED1F612970CEE2D7AFB81BDD762170481CD0069127" + "D5B05AA993B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934063199" + "FFFFFFFFFFFFFFFF", "5");
        s_groupParameters.Add(4096, c);

        c = new SrpConstants("FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" + "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B" + "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9" + "A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE6" + "49286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8" + "FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D" + "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C" + "180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF695581718" + "3995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D" + "04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7D" + "B3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D226" + "1AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200C" + "BBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFC" + "E0FD108E4B82D120A92108011A723C12A787E6D788719A10BDBA5B26" + "99C327186AF4E23C1A946834B6150BDA2583E9CA2AD44CE8DBBBC2DB" + "04DE8EF92E8EFC141FBECAA6287C59474E6BC05D99B2964FA090C3A2" + "233BA186515BE7ED1F612970CEE2D7AFB81BDD762170481CD0069127" + "D5B05AA993B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934028492" + "36C3FAB4D27C7026C1D4DCB2602646DEC9751E763DBA37BDF8FF9406" + "AD9E530EE5DB382F413001AEB06A53ED9027D831179727B0865A8918" + "DA3EDBEBCF9B14ED44CE6CBACED4BB1BDB7F1447E6CC254B33205151" + "2BD7AF426FB8F401378CD2BF5983CA01C64B92ECF032EA15D1721D03" + "F482D7CE6E74FEF6D55E702F46980C82B5A84031900B1C9E59E7C97F" + "BEC7E8F323A97A7E36CC88BE0F1D45B7FF585AC54BD407B22B4154AA" + "CC8F6D7EBF48E1D814CC5ED20F8037E0A79715EEF29BE32806A1D58B" + "B7C5DA76F550AA3D8A1FBFF0EB19CCB1A313D55CDA56C9EC2EF29632" + "387FE8D76E3C0468043E8F663F4860EE12BF2D5B0B7474D6E694F91E" + "6DCC4024FFFFFFFFFFFFFFFF", "5");
        s_groupParameters.Add(6144, c);

        c = new SrpConstants("FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" + "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B" + "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9" + "A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE6" + "49286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8" + "FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D" + "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C" + "180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF695581718" + "3995497CEA956AE515D2261898FA051015728E5A8AAAC42DAD33170D" + "04507A33A85521ABDF1CBA64ECFB850458DBEF0A8AEA71575D060C7D" + "B3970F85A6E1E4C7ABF5AE8CDB0933D71E8C94E04A25619DCEE3D226" + "1AD2EE6BF12FFA06D98A0864D87602733EC86A64521F2B18177B200C" + "BBE117577A615D6C770988C0BAD946E208E24FA074E5AB3143DB5BFC" + "E0FD108E4B82D120A92108011A723C12A787E6D788719A10BDBA5B26" + "99C327186AF4E23C1A946834B6150BDA2583E9CA2AD44CE8DBBBC2DB" + "04DE8EF92E8EFC141FBECAA6287C59474E6BC05D99B2964FA090C3A2" + "233BA186515BE7ED1F612970CEE2D7AFB81BDD762170481CD0069127" + "D5B05AA993B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934028492" + "36C3FAB4D27C7026C1D4DCB2602646DEC9751E763DBA37BDF8FF9406" + "AD9E530EE5DB382F413001AEB06A53ED9027D831179727B0865A8918" + "DA3EDBEBCF9B14ED44CE6CBACED4BB1BDB7F1447E6CC254B33205151" + "2BD7AF426FB8F401378CD2BF5983CA01C64B92ECF032EA15D1721D03" + "F482D7CE6E74FEF6D55E702F46980C82B5A84031900B1C9E59E7C97F" + "BEC7E8F323A97A7E36CC88BE0F1D45B7FF585AC54BD407B22B4154AA" + "CC8F6D7EBF48E1D814CC5ED20F8037E0A79715EEF29BE32806A1D58B" + "B7C5DA76F550AA3D8A1FBFF0EB19CCB1A313D55CDA56C9EC2EF29632" + "387FE8D76E3C0468043E8F663F4860EE12BF2D5B0B7474D6E694F91E" + "6DBE115974A3926F12FEE5E438777CB6A932DF8CD8BEC4D073B931BA" + "3BC832B68D9DD300741FA7BF8AFC47ED2576F6936BA424663AAB639C" + "5AE4F5683423B4742BF1C978238F16CBE39D652DE3FDB8BEFC848AD9" + "22222E04A4037C0713EB57A81A23F0C73473FC646CEA306B4BCBC886" + "2F8385DDFA9D4B7FA2C087E879683303ED5BDD3A062B3CF5B3A278A6" + "6D2A13F83F44F82DDF310EE074AB6A364597E899A0255DC164F31CC5" + "0846851DF9AB48195DED7EA1B1D510BD7EE74D73FAF36BC31ECFA268" + "359046F4EB879F924009438B481C6CD7889A002ED5EE382BC9190DA6" + "FC026E479558E4475677E9AA9E3050E2765694DFC81F56E880B96E71" + "60C980DD98EDD3DFFFFFFFFFFFFFFFFF", "19");
        s_groupParameters.Add(8192, c);
    }

    /// <summary>
    /// Looks up the valid precomputed constants for SRP given the specified bit strength.
    /// </summary>
    /// <param name="strength">The bit strength to lookup.</param>
    /// <returns>The SRP constants for the specified strength.</returns>
    public static SrpConstants Lookup(SrpStrength strength)
    {
        int bits = (int)strength;

        if (!s_groupParameters.TryGetValue(bits, out SrpConstants? value))
            throw new InvalidEnumArgumentException("strength");

        return value;
    }

    /// <summary>
    /// Computes the XOR of the supplied parameters
    /// </summary>
    private static byte[] Xor(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            throw new Exception();

        byte[] rv = new byte[a.Length];

        for (int x = 0; x < a.Length; x++)
            rv[x] = (byte)(a[x] ^ b[x]);

        return rv;
    }

    /// <summary>
    /// Computes the hash of all of the supplied parameters.
    /// </summary>
    /// <param name="words">The byte arrays to be hashed.</param>
    /// <returns>The computed SHA-512 hash as a byte array.</returns>
    private static byte[] ComputeHash(params byte[][] words)
    {
        Sha512Digest hash = new();

        hash.Reset();

        foreach (byte[] w in words)
            hash.BlockUpdate(w, 0, w.Length);

        byte[] rv = new byte[hash.GetDigestSize()];

        hash.DoFinal(rv, 0);

        return rv;
    }

    #endregion
}