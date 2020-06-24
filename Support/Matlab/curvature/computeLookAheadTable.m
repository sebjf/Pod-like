function [] = computeLookAheadTable()
%COMPUTELOOKAHEADTABLE script to check if we have inverted the Menger
%Curvature correctly by comparing plots of both directions.

bs = 2:0.1:10;
h = 2;

function c = MC(x,y,z,b,h)
    c = (4 * 0.5 * b * h) / (norm(y-x) * norm(y-z) * norm(x-z));
end

function c = MCbh(b,h)
    %c = (2 * h) / ((b*0.5)^2 + (h^2));
    %c = (2 * h) / (0.25*b^2 + (h^2));
    c = 1/((h) / (2) + (b^2)/ (8*h));
end

function b = B(c,h)
   b = sqrt( ((8*h) / c) - (4*h^2)); 
end

X1 = bs;
C = [];
Cbh = [];

for b = bs

    x = [ 0 0 ];
    y = [ b/2 h ];
    z = [ b 0 ];

    c = MC(x,y,z,b,h);
    
    C = [C; c];
    
    Cbh = [Cbh; MCbh(b,h);];
end

X2= [];
for c = C'
    b = B(c,h);
    
    X2 = [X2; b];
end

clf;
hold all;
plot(X1,C);
plot(X1,Cbh);
plot(X1,C-Cbh);


yyaxis right
hold all;
plot(X2,C);
%plot(X1,1./C);
%plot(X2,1./C);


end

