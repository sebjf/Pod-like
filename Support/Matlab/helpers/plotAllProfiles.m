function [] = plotAllProfiles(J)
%PLOTTRACKSPEEDS Summary of this function goes here
%   Detailed explanation goes here
profiles = J.Profiles;

names = categorical({J.Profiles.Path})';
profiles = profiles(names == categorical("AlderonInterpolated0.5"),:);

d = [profiles.Distance];
a = [profiles.Actual];
b = [profiles.Sideslip];
d = wrapDistances(d);

figure;
hold all;

% speed
plot(d,a,':');

%decleration
%s = [J.Profiles.Speed];
%ax = gca;
%ax.ColorOrderIndex = 1;
%plot(d,s);


end

