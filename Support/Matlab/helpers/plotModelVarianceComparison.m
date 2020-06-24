function [] = plotModelVarianceComparison()

model0 = readKerasModel('model.h5');
model1 = readKerasModel('model1.h5');
model2 = readKerasModel('model2.h5');
model3 = readKerasModel('model3.h5');
model4 = readKerasModel('model4.h5');
model5 = readKerasModel('model5.h5');
model6 = readKerasModel('model6.h5');
model7 = readKerasModel('model7.h5');

hold all

colour1 = [ 0.1 0.1 0.1 0.8 ];
for m = [model0,model1,model2,model3,model4,model5,model6,model7]
  [~,~,~,h1,h2] = plotPredictedProfileByPath(1000, 0, 0.00, 1, m); 
  for h = [h1; h2]'
      h.Color = colour1;
      h.Marker= 'none';
      h.LineStyle = '--';  
  end
end

annotation('textbox',[.15 .58 .5 .2],'String','Model','EdgeColor','none','Color',colour1);
