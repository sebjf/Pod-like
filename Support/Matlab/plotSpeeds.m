function [h] = plotSpeeds(filename,s0,s1)
%PLOTSPEEDS Summary of this function goes here
%   Detailed explanation goes here

speeds = readSpeeds(filename);

if nargin > 3
    indices = speeds(:,1) > s0 & speeds(:,1) < s1;
    speeds = speeds(indices,2);
end

hold all;
yyaxis left;

h = plot(speeds(:,1),speeds(:,2),'-k','LineWidth',0.8);

end

