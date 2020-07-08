function [] = plotDrivingProfile(T)
%PLOTSTEERINGANGLE Summary of this function goes here
%   Detailed explanation goes here

hold all;

plot(T.distance,T.direction);
plot(T.distance,deg2rad(T.steeringangle));
plot(T.distance,T.lateralerror / 10);

legend(["Direction", "Steering", "Error"]);

end

