function [] = plotBeltane(P)
%SHADEBELTANESTRAIGHT Summary of this function goes here
%   Detailed explanation goes here

straight = P.distance > 1737 & P.distance < 4249 | P.distance > 4885 & P.distance < 7337;

P1 = P( straight,:);
P2 = P(~straight,:);

hold all
plot(P1.distance,abs(P1.curvature) * 20,'Color','red');
plot(P2.distance,abs(P2.curvature) * 20,'Color','blue');

end

