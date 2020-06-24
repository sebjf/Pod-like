function [] = plotPredictedProfileId(i,n)
%PLOTPREDICTION Summary of this function goes here
%   Detailed explanation goes here

network = evalin('base', 'network');
J = evalin('base', 'J');

if nargin < 2
    n = 2;
end

% make training input
window = 25;
curvaturescale = 20;
camberscale = 200;
inclinationscale = 3;

allspeeds = [J.Profiles(i).Actual];
indices = n:n+window-1;

curvature = J.Profiles(i).Curvature(indices);
camber = J.Profiles(i).Camber(indices);
inclination = J.Profiles(i).Inclination(indices);

observations = [ curvature * curvaturescale camber * camberscale inclination * inclinationscale];
x1 = reshape(observations',1,[]);
x2 = allspeeds(n-1);

x_train = [x2 x1];
y_speed = allspeeds(indices);

distances = J.Profiles(i).Distance(indices);

y = network.predict(x_train);
y = double(y)';
e = y - y_speed;

clf;
hold all;
plot(y_speed);
plot(y);
plot(e);

yyaxis right

plot(observations); %observations

ylim([-1,1]);
legend(gca,'Actual','Predicted','Error','Curvature','Camber','Inclination');

%annotation('textbox',[.15 .7 .5 .2],'String',sprintf('Speed %f m/s',x3),'EdgeColor','none')
text(1,0.8,sprintf('Speed %f m/s',x2))

ticks = linspace(1,length(indices),7);

xticks(ticks);
tickdistances = distances(ticks);

xticklabels(arrayfun(@(z) sprintf("%0.0f",z), tickdistances, 'UniformOutput', false));


end

