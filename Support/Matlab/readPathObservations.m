function [path] = readPathObservations(filename)
%READSPEEDS Summary of this function goes here
%   Detailed explanation goes here

if nargin < 1
   filename = "path.txt"; 
end

dataLines = [1, Inf];

% Setup the Import Options and import the data
opts = delimitedTextImportOptions("NumVariables", 1);

% Specify range and delimiter
opts.DataLines = dataLines;
opts.Delimiter = ",";

% Specify column names and types
opts.VariableNames = "VarName1";
opts.VariableTypes = "double";

% Specify file level properties
opts.ExtraColumnsRule = "ignore";
opts.EmptyLineRule = "read";

% Import the data
path = readtable(filename, opts);
path = table2array(path);
path = reshape(path,4,[])';

distance = path(:,1);
curvature = path(:,2);
camber = path(:,3);
inclination = path(:,4);

path = table(distance,curvature,camber,inclination);

if nargin < 1
   assignin('base','path',path);
end

end

