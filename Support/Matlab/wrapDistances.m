function [d] = wrapDistances(d)
%WRAPDISTANCES Summary of this function goes here
%   Detailed explanation goes here
for i = 1:size(d,2)
    wrap = find(diff(d(:,i)) < 0);
    d(wrap+1:end,i) = d(wrap+1:end,i) + d(wrap,i);
end

end

