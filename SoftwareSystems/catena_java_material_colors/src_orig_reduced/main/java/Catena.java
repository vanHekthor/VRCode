package main.java;

import java.util.Arrays;
import java.util.Random;

import main.java.components.gamma.GammaInterface;
import main.java.components.graph.GraphInterface;
import main.java.components.graph.algorithms.IdxInterface;
import main.java.components.hash.HashInterface;
import main.java.components.phi.PhiInterface;

public class Catena {
    
    private Helper helper = new Helper();
    
    /**
     * versionID decodes the version of catena
     * possible: "Dragonfly", "Dragonfly-Full", Butterfly", Butterfly-Full"
     */
    private String _vId;
    
    private HashInterface   _h;
    private HashInterface   _hPrime;
    private GammaInterface  _gamma;
    private GraphInterface  _f;
    private PhiInterface    _phi;
    
    private int _d = 0;         // 0 = Password Scrambling (Default)
    private int _n;             // H output size
    private int _k;             // H' output size
    private int _gLow;          
    private int _gHigh;
    private int _lambda;
    
    /**
     * Main function of catena to hash a password
     * 
     * @param pwd           Password to be hashed
     * @param salt          Salt of arbitrary length
     * @param publicInput   User choosen public input
     * @param gamma         Input for graph size
     * @param m             User desired output length of hash
     * 
     * @return xTrun        Hash of pwd
     */
    public byte[] catena(byte[] pwd, byte[] salt, byte[] publicInput,
             byte[] gamma, int m){
        
        byte[] t = compTweak(_vId, _d, _lambda, m, salt.length, publicInput);
        
        _h.update(helper.concateByteArrays(t, pwd, salt));
        byte[] x = _h.doFinal();
        
        erasePwd(pwd);
        
        x = flap(((_gLow+1)/2), x, gamma);
        
        _h.update(x);
        x = _h.doFinal();
        
        byte[] gByte = new byte[1];
        
        for (int g = _gLow; g <= _gHigh; ++g){
//          System.out.println("G: " + g);
            if (x.length < _n){
                x = helper.paddWithZero(x, _n);
            }
            
            x = flap(g, x, gamma);
            
            gByte[0] = (byte)g;
            _h.update(helper.concateByteArrays(gByte, x));
            x = _h.doFinal();
            x = helper.truncate(x, m);
        }
        return x;
    }
    
    /**
     * flap function from catena specification
     * 
     * @param g
     * @param xIn
     * @param gamma
     * @return
     */
    private byte[] flap(int g, byte[] xIn, byte[] gamma){
        
        _hPrime.reset();
        
        byte[] xHinit;
        int iterations = (int)Math.pow(2, g);
        
        byte[][] v = new byte[iterations+2][_k];
        
        xHinit = hInit(xIn);
        
        System.arraycopy(xHinit, 0, v[0], 0, _k);
        System.arraycopy(xHinit, _k, v[1], 0, _k);
        
        for (int i=2; i<iterations+2; ++i){
//          if (i%10000 == 0) {
//              System.out.println("Flap iterations " + i + " / " + iterations);
//          }
            _hPrime.update(helper.concateByteArrays(v[i-1], v[i-2]));
            v[i] = _hPrime.doFinal();
        }
//      System.out.println("Now out.");
        
        byte[][] v2 = new byte[iterations][_k];
        System.arraycopy( v, 2, v2, 0, v2.length );
        
        _hPrime.reset();
        
        v2 = gamma(g, v2, gamma);
        
//      System.out.println("Now out gamma.");
        
        _hPrime.reset();
        
        v2 = f(g, v2, _lambda);
        
//      System.out.println("Now out f.");
        
        _hPrime.reset();
        v2 = phi(g, v2, v2[v2.length-1]);
        
//      System.out.println("Now out phi.");
        return v2[v2.length-1];
    }
    
    public byte[] flapPub(int g, byte[] xIn, byte[] gamma){
        return flap(g, xIn, gamma);
    }
    
    
    /**
     * Initialisation of the 2 values for flap rounds
     * 
     * @param x     Input Array
     * @return      2 hashed values v_-1, V_-2 in one byte array
     *              (output is already splitted in the middle and swapped)
     */
    private byte[] hInit(byte[] x){
        int l = 2*_k/_n;
        byte[][] xLoop = new byte[l][_n];
        byte[] iByte = new byte[1];
        
        for (int i=0; i<= l-1; ++i){
            iByte[0] = (byte) i;
            _h.update(helper.concateByteArrays(iByte, x));
            xLoop[i] = _h.doFinal();
            _h.reset();
        }
        return helper.twoDimByteArrayToOne(xLoop);
    }
    
    /**
     * No clue how to test private functions so this wrapper exists
     * 
     * @param x     Initial value to instantiate v-2 and v-1
     * @return      v-2 and v-1 combined in one array
     */
    public byte[] testHInit(byte[] x){
        return hInit(x);
    }
    
    /**
     * salt dependent update with random access
     * 
     * @param g     garlic
     * @param x     hash array
     * @param gamma gamma
     * @return      hash array
     */
    private byte[][] gamma(int g, byte[][] x, byte[] gamma){
        return _gamma.gamma(g, x, gamma);
    }
    
    /**
     * Memory expensive graph Layer
     * 
     * @param x     hash input
     * @return      hash output
     */
    private byte[][] f(int g, byte[][] x, int lambda){
        return _f.graph(g, x, lambda);
    }
    
    /**
     * phi function from catena specification
     * 
     * @param x     hash input
     * @return      hash output
     */
    private byte[][] phi(int garlic, byte[][] x, byte[] m){
        return _phi.phi(garlic, x, m);
    }
    
    /**
     * Combine Tweak Array
     * 
     * @param vId       Version ID
     * @param mode      Mode of catena
     * @param lambda    Lambda
     * @param outLen    Output Length
     * @param sLen      Salt Length
     * @param aData     Additional Data
     * @return          Combined Tweak
     */
    private byte[] compTweak(String vId, int mode, 
            int lambda, int outLen, int sLen, byte[] aData) {
        
        byte[] modeByte = new byte[1];
        byte[] lambdaByte = new byte[1];
        byte[] outLenByte = helper.intToByteArrayLittleEndian(outLen, 2);
        byte[] sLenByte = helper.intToByteArrayLittleEndian(sLen, 2);
        
        _h.update((helper.string2Bytes(vId)));
        byte[] vIdH = _h.doFinal();
        _h.reset();

        _h.update(aData);
        byte[] aDataH = _h.doFinal();
        _h.reset();
        
        modeByte[0] = (byte) mode;
        lambdaByte[0] = (byte) lambda;

        return helper.concateByteArrays(vIdH, modeByte, 
                lambdaByte, outLenByte, sLenByte, aDataH);
    }
    
    /**
     * public interface for testing tweak computation
     * 
     * @param vId       String, VersionID
     * @param mode      Integer, Mode of Catena
     * @param lambda    Integer, The depth of the graph structure.
     * @param outLen    Integer, Output length.
     * @param sLen      Integer, Salt length.
     * @param aData     byte[], Associated data of the user and/or the host.
     * @return tweak    byte[], The calculatetd tweak.
     */
    public byte[] testCompTweak (String vId, int mode, 
            int lambda, int outLen, int sLen, byte[] aData){
        return compTweak(vId, mode, lambda, outLen, sLen, aData);
    }
    
    /**
     * Clear the password
     * 
     * @param pwd   the password to be cleared
     */
    private final void erasePwd(byte[] pwd) {
        Arrays.fill(pwd, (byte) 0);
    }

    /**
     * Initializes Catena
     * 
     * initializrs all needed variables and functions with default values
     * 
     * @param h         main hash function
     * @param hPrime    reduced hash function
     * @param gamma     gamma function (e.g. SaltMix)
     * @param f         graph
     * @param idx       index function for graph
     * @param phi       phi function
     * @param gLow      minimum Garlic
     * @param gHigh     maximum Garlic
     * @param lambda    depth of graphs
     * @param vID       version ID
     */
    public void init(HashInterface h, HashInterface hPrime, 
            GammaInterface gamma, GraphInterface f, 
            IdxInterface idx, PhiInterface phi, int gLow, int gHigh,
            int lambda, String vID){
        
        _h = h;
        _hPrime = hPrime;
        
        _gamma = gamma;
        _gamma.setH(_h);
        _gamma.setHPrime(_hPrime);
        
        _f = f;
        _f.setH(_h);
        _f.setHPrime(_hPrime);
        _f.setIndexing(idx);
        
        _phi = phi;
        _phi.setH(_h);
        _phi.setHPrime(_hPrime);
        
        _gLow = gLow;
        _gHigh = gHigh;
        
        _lambda = lambda;
        
        _n = _h.getOutputSize();
        _k = _hPrime.getOutputSize();
        
        _vId = vID;
    }
    
    public String getVID(){
        return _vId;
    }
    
    public int getLambda(){
        return _lambda;
    }
    
    public void setGHigh(int gHigh){
        _gHigh = gHigh;
    }
    
    public void setGLow(int gLow){
        _gLow = gLow;
    }
    
    public void setD(int d){
        _d = d;
    }
    
}
