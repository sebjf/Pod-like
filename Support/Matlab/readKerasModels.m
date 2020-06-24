function [ models ] = readKerasModels(car)

filenames = dir(string(car) + "*.h5")';
models = [];

for filename = filenames
    
    if isempty(regexp(filename.name,'\d.h5$','once'))
        continue;
    end

    model = importKerasLayers(fullfile(filename.folder,filename.name),'ImportWeights',true);
    %model = replaceLayer(model,'gaussian_noise_1',gaussianNoiseLayer(0,'new_gaussian_noise_1')); %if we turn on guassian noise
    network = assembleNetwork(model);

    models = [models; network];
end

if nargin < 1
   assignin('base','models',models); 
end

end