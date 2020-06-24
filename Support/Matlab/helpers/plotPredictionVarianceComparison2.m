start = 1000;
x = start:1:(start+24*10);
Y = [];

hold all

colour1 = [ 0 0.1 1 ];
for i = 1:10
  [y,distances,observations] = plotPredictedProfileByPath(1000, 0, 0.02, -1); 
  y1 = interp1(distances,y,x);
  Y = [Y; y1];
  yyaxis right
 % h1 = plot(distances,observations);
 % for h = h1'
 %     h.Color = colour1;
 %     h.Marker= 'none';
 %     h.LineStyle = '-';  
 % end
end
yyaxis left
plotShadedRange(x,Y,colour1);

colour2 = [ 0 0.9 0.0 ];
for i = 1:10
  [y,distances] = plotPredictedProfileByPath(1000, i, 0, -1); 
  y1 = interp1(distances,y,x);
  Y = [Y; y1];
end
plotShadedRange(x,Y,colour2);

return

colour3 = [ 0.8 0.2 0.1];
for i = 1:10
  [y,distances] = plotPredictedProfileByPath(1000+i, 0, 0, -1); 
  y1 = interp1(distances,y,x);
  Y = [Y; y1];
end
plotShadedRange(x,Y,colour3);

%annotation('textbox',[.15 .7 .5 .2],'String','Observation Noise','EdgeColor','none','Color',colour1);
%annotation('textbox',[.15 .65 .5 .2],'String','Starting Speed','EdgeColor','none','Color',colour2);
%annotation('textbox',[.15 .6 .5 .2],'String','Spatial Quantisation','EdgeColor','none','Color',colour3);