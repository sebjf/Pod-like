function [] = plotShadedRange(x,Y,colour)
%PLOTSHADEDRANGE Summary of this function goes here

% https://uk.mathworks.com/matlabcentral/fileexchange/58262-shaded-area-error-bar-plot

patch = fill([x, fliplr(x)], [max(Y), fliplr(min(Y))], colour);
patch.EdgeColor = 'black';
patch.FaceAlpha = 0.2;

end

