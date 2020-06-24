hold all

readPath;

colour1 = [ 0 0.2 1 0.5 ];
for i = 1:10
  [y,distances,observations,h1,h2] = plotPredictedProfileByPath(1000, 0, 0.02, 1); 
  for h = [h1; h2]'
      h.Color = colour1;
      h.Marker= 'none';
      h.LineStyle = '-';  
  end
end

colour2 = [ 0 0.8 0.2 0.5 ];
for i = 1:10
  [y,distances,observations,h1,h2] = plotPredictedProfileByPath(1000, i, 0, 1); 
  for h = [h1; h2]'
      h.Color = colour2;
      h.Marker= 'none';
      h.LineStyle = '-';  
  end
end

colour3 = [ 0.8 0.2 0.1 0.5];
for i = 1:10
  [y,distances,observations,h1,h2] = plotPredictedProfileByPath(1000+i, 0, 0, 1); 
  for h = [h1; h2]'
      h.Color = colour3;
      h.Marker= 'none';
      h.LineStyle = '-';  
  end
end

annotation('textbox',[.15 .70 .5 .2],'String','Observation Noise','EdgeColor','none','Color',colour1);
annotation('textbox',[.15 .66 .5 .2],'String','Starting Speed','EdgeColor','none','Color',colour2);
annotation('textbox',[.15 .62 .5 .2],'String','Spatial Quantisation','EdgeColor','none','Color',colour3);