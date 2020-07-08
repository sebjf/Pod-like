function [S] = summariseSectionSpeeds(T)

% find the lap times for each car by looking for the change in coefficient

T.c = [diff(T.coefficient); 1];
coefficient_times = [];
lap_time_rows = [0; find(T.c)];
for i = 2:length(lap_time_rows)
    start = lap_time_rows(i-1);
    endd = lap_time_rows(i);
    row = T(endd,:);
    row.time = T.times(endd) - T.times(start + 2);
    coefficient_times = [coefficient_times; row];
end

% and the same but for each section change within each run

T.s = [diff(T.section); 1];
section_times = [];
lap_time_rows = [0; find(T.s)];
for i = 2:length(lap_time_rows)
    start = lap_time_rows(i-1);
    endd = lap_time_rows(i);
    row = T(endd,:);
    row.time = T.times(endd) - T.times(start + 2);
    section_times = [section_times; row];
end

S = [];

for n = unique(T.name)'
    B = coefficient_times(coefficient_times.name == n,:);
    C = section_times(section_times.name == n,:);
    
    [mintime,minindex] = min(B.time);
    [maxtime,maxindex] = max(B.time);
    
    range = maxtime - mintime;
    coefficient = B.coefficient(minindex);
    minimumtime = mintime;
    
    genome = [];
    for s = unique(C.section)'
        c = C(C.section==s,:);
        [mintime,minindex] = min(c.time);
        minc = c.coefficient(minindex);
        genome = [genome; s, minc, mintime];
    end
    
    best_genome_time = sum(genome(:,3));
    
    % summary table row
    R = table(n,range,minimumtime,coefficient,best_genome_time);
    S = [S; R];
end

end

