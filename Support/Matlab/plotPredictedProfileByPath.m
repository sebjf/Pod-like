function [y, distances, observations,h1,h2] = plotPredictedProfileByPath(d, starting_speed, noise, opt, network)
%PLOTPREDICTION Summary of this function goes here
%   Detailed explanation goes here

path = evalin('base', 'path');

if nargin < 5
    network = evalin('base', 'network');
end

if nargin < 3
   noise = 0; 
end

if nargin < 4
   opt = 2; 
end

% make training input
window = 25;
curvaturescale = 20;
camberscale = 200;
inclinationscale = 3;
interval = 10;

[~,i] = min( abs( path(:,1) - d ) );

path_observations = path( i:10:(i+window*interval)-1, :);

curvature = path_observations(:,2);
camber = path_observations(:,3);
inclination = path_observations(:,4);
distances = path_observations(:,1);

observations = [ curvature * curvaturescale camber * camberscale inclination * inclinationscale] + randn(25,3)*noise;
x1 = reshape(observations',1,[]);
x2 = starting_speed;

x_train = [x2 x1];

y = network.predict(x_train);
y = double(y)';

if opt > 0
    
hold all;
yyaxis left
h1 = plot(distances,y);

yyaxis right

h2 = plot(distances,observations); %observations

ylim([-1,1]);

end

if opt > 1

legend(gca,'Predicted','Curvature','Camber','Inclination');
annotation('textbox',[.15 .7 .5 .2],'String',sprintf('Speed %f m/s',x2),'EdgeColor','none');

end

end

