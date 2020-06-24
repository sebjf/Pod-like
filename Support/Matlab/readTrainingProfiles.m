function J = readTrainingProfiles(car)

J.Profiles = [];
for f = dir(string(car) + "." + "*.trainingprofile.json")'
     j = jsondecode(fscanf(fopen(fullfile(f.folder,f.name)),'%s'));
     J.Profiles = [J.Profiles; j.Profiles];  
end
fclose('all');

end