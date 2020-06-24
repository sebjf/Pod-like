function [rows] = findProfileIdsByDistance(d)
%FINDERRORS Summary of this function goes here
%   Detailed explanation goes here
J = evalin('base', 'J');

distances = [J.Profiles.Distance]'; % rows are profile ids
isbelow = max(distances')' > d;
isabove = min(distances')' < d;

rows = isabove & isbelow;
rows = find(rows);

end