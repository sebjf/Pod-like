function [T] = readPathTimings(directory)
%READPATHTIMINGS Summary of this function goes here
%   Detailed explanation goes here

if nargin < 1
    directory = fullfile(pwd,"..","TrainingData");
end

T = [];

files = dir(fullfile(directory,"*.pathtimings.json"));

for file = files'

J = jsondecode(fscanf(fopen(fullfile(file.folder, file.name)),'%s'));
indices = J.crossoverIndices;

for j = J.profiles'
   
    rows = 1:length(j.times);
    
    times(rows) = j.times;
    coefficient(rows) = j.coefficient;
   
    section(rows) = 1;
    for i = 1:length(indices)
        section(indices(i):end) = section(indices(i):end) + 1;
    end
    
    times = times';
    coefficient = coefficient';
    section = section';
    
    times(1) = 0;
    
    name = extractBetween(file.name,"",".pathtimings.json");
    name = categorical(name);
    name(rows) = name;
    name = name';
    
    T = vertcat(T, table(name,times,coefficient,section));
    
    times = [];
    coefficient = [];
    section = [];
end

end

end

