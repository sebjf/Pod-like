function [T] = readDrivingProfile(filename)
%READSPEEDS Summary of this function goes here
%   Detailed explanation goes here

if nargin < 1
   filename = "drivingprofile.json"; 
end

J = jsondecode(fscanf(fopen(filename),'%s'));
T = struct2table(J);

end

