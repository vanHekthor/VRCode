package main.java;

import main.java.components.gamma.GammaInterface;
import main.java.components.gamma.algorithms.SaltMix;
import main.java.components.graph.GraphInterface;
import main.java.components.graph.algorithms.GenericGraph;
import main.java.components.graph.algorithms.IdxInterface;
import main.java.components.graph.algorithms.index.IndexBRG;
import main.java.components.hash.HashInterface;
import main.java.components.hash.algorithms.Blake2b;
import main.java.components.hash.algorithms.Blake2b_1;
import main.java.components.phi.PhiInterface;
import main.java.components.phi.algorithms.IdentityPhi;

public class DefaultInstances extends Catena {

    /**
     * initalizes Dragonfly default instance of catena
     * 
     * @return  Catena Dragonfly instance
     */
    public Catena initDragonfly(){
        Catena c = new Catena();
        
        HashInterface h = new Blake2b();
        HashInterface hPrime = new Blake2b_1();
        
        GammaInterface gamma = new SaltMix();
        
        GraphInterface f = new GenericGraph();

        IdxInterface idx = new IndexBRG();
        PhiInterface phi = new IdentityPhi();       
        
        int gLow = 21;
        int gHigh = 21;
        int lambda = 2;
        
        String vID = "Dragonfly";
        
        c.init(h, hPrime, gamma, f, idx, phi, gLow, gHigh, lambda, vID);
        
        return c;
    }
    
    /**
     * initalizes Dragonfly Full default instance of catena
     * 
     * @return  Catena Dragonfly Full instance
     */
    public Catena initDragonflyFull(){
        Catena c = new Catena();
        
        HashInterface h = new Blake2b();
        HashInterface hPrime = new Blake2b();
        
        GammaInterface gamma = new SaltMix();
        
        GraphInterface f = new GenericGraph();

        IdxInterface idx = new IndexBRG();
        PhiInterface phi = new IdentityPhi();
        
        int gLow = 22;
        int gHigh = 22;
        
        int lambda = 2;
        
        String vID = "Dragonfly-Full";
        
        c.init(h, hPrime, gamma, f, idx, phi, gLow, gHigh, lambda, vID);
        
        return c;
    }

}
