function [] = plotProfileObservations(j)
%PLOTPROFILEOBSERVATIONS Summary of this function goes here
%   Detailed explanation goes here
hold all;
plot(j.Distance,j.Curvature);
plot(j.Distance,j.Camber);
plot(j.Distance,j.Inclination * 0.5);
legend("Curvature","Camber","Inclination");
end

