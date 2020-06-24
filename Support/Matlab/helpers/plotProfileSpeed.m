function [] = plotProfileSpeed(j)
%PLOTPROFILEOBSERVATIONS Summary of this function goes here
%   Detailed explanation goes here
hold all;
plot(j.Distance,j.Actual);
legend("Speed");
end

