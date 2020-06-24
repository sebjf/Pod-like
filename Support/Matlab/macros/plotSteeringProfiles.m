hold all;
h1 = plot(T.distance, T.direction, '--');
h2 = plot(T.distance, T.steeringangle/40, '-');

h1.Color = [ 0 0 1 ];
h2.Color = [ 0 0 1 ];

h1 = plot(T3.distance, T3.direction, '--');
h2 = plot(T3.distance, T3.steeringangle/40, '-');

h1.Color = [ 1 0 1 ];
h2.Color = [ 1 0 1 ];