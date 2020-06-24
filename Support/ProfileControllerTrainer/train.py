import numpy as np
import keras
import tensorflow
import json
import glob
import os
import subprocess

# environment parameters

modelname = "gamma"

source = os.path.abspath(os.path.join(os.getcwd(), "../TrainingData"))
destination = os.path.abspath(os.path.join(os.getcwd(), "../../Assets/StreamingAssets"))

train_controller = True
train_classifier = True

# comment out as appropriate...
#files = [
#    "D:/temp11/alderon.profile.json",
#    "D:/temp11/beltane.profile.json"
#]
files = glob.glob(os.path.join(source, modelname + ".*.trainingprofile.json"))

# network parameters

window = 25

# process input data

x_train = None
y_speed = None
y_brake = None

# read in profiles

profiles = []

for n in files:
    with open(n) as f:
        data = json.load(f)
        profiles.extend(data['Profiles'])

for profile in profiles:

    # width is the dynamic profile length

    profile_width = profile['Speed'].__len__()

    curvature = np.array(profile['Curvature'])
    camber = np.array(profile['Camber'])
    inclination = np.array(profile['Inclination'])
    #speed = np.array(profile['Speed'])
    actual = np.array(profile['Actual'])
    sideslip = np.array(profile['Sideslip'])
    braking = np.array(profile['Braking'])

    # input transforms
    # calculated from the approximate reciprocal of the maximum absolute values of each d.o.f in turn.
    # these figures are effectively part of the network and should be applied with it
    # the figures are dependent on what paths the training agent has been exposed to. they are physically based, and vary with track design, but practically once found for a couple of examples they will not change much

    curvature = curvature * 20
    camber = camber * 200
    inclination = inclination * 3
    sideslip = sideslip * 0.01

    observations = np.vstack((curvature,camber,inclination))

    # resample the training data.
    # move a window over the example data from n+1 to capture the behaviour with a given starting speed
    
    num_windows = profile_width - window

    for n in range(1,num_windows):
        _observations = observations[:, n:n+window].reshape(window*3, order='F')  # F means change first axis first, so its [ curvature, camber, inclination, curvature, camber, inclination, etc, etc]...
        _starting_speed = actual[n-1]
        _starting_slip = sideslip[n-1]
        x = np.hstack((_starting_speed,_starting_slip,_observations))

        if x_train is None:
            x_train = x
        else:
            x_train = np.vstack((x_train, x))

        #y = speed[n:n+window] # learn deceleration profile
        #y = actual[n:n+window] # learn realistic profile
        y = actual[n+1] # learn single-speed controller

        if y_speed is None:
            y_speed = y
        else:
            y_speed = np.vstack((y_speed, y))

        y = any(braking[n:n+window])

        if y_brake is None:
            y_brake = y
        else:
            y_brake = np.vstack((y_brake, y))


# create model

flattened_width = x_train.shape[1]

if train_controller:
    models = []
    for n in range(5):
        keras.backend.clear_session()
        inputs = keras.Input(shape=(flattened_width,))
        x = inputs
        x = keras.layers.Dense(100, activation='relu')(x)
        outputs = keras.layers.Dense(1, activation='relu')(x)

        model = keras.Model(inputs=inputs, outputs=outputs)
        print(model.summary())

        def my_mean_squared_error(y_true, y_pred):
            if not keras.backend.is_keras_tensor(y_pred):
                y_pred = keras.backend.constant(y_pred)
            y_true = keras.backend.cast(y_true, y_pred.dtype)
            biased = keras.backend.square(keras.backend.relu(y_true - y_pred)) * (0 + n) + keras.backend.square(y_pred - y_true)
            return keras.backend.mean(biased, axis=-1)

        #model.compile(optimizer='adam',loss='mean_squared_error')
        model.compile(optimizer='adam',loss=my_mean_squared_error)

        model.fit(
            x=x_train,
            y=y_speed,
            batch_size=100000,
            epochs=8000,
            validation_split=0.02,
            callbacks=[keras.callbacks.EarlyStopping(monitor='val_loss', mode='min', min_delta=0.01, patience=1000)]
            )
        
        model.name = modelname + str(n)
        model.save(os.path.join(source, model.name + ".h5"))
        models.append(model)

        modelh5 = os.path.join(source, model.name + ".h5")
        modelnn = os.path.join(destination, model.name + ".nn")
        subprocess.run("python Barracuda/Tools/keras_to_barracuda.py \"{0}\" \"{1}\"".format(modelh5, modelnn), shell=True) # run in subprocess because calls are stateful


if train_classifier:
    keras.backend.clear_session()
    inputs = keras.Input(shape=(flattened_width,))
    x = keras.layers.Dense(100, activation='relu')(inputs)
    outputs = keras.layers.Dense(1, activation='sigmoid')(x)

    model = keras.Model(inputs=inputs, outputs=outputs)
    print(model.summary())

    model.compile(optimizer='adam', loss='binary_crossentropy', metrics=['accuracy'])

    model.fit(
        x=x_train,
        y=y_brake,
        batch_size=100000,
        epochs=5000,
        validation_split=0.02,
        callbacks=[keras.callbacks.EarlyStopping(monitor='val_accuracy', mode='max', min_delta=0.005, patience=500)]
        )
    
    model.name = modelname + "C"
    model.save(os.path.join(source, model.name + ".h5"))

    modelh5 = os.path.join(source, model.name + ".h5")
    modelnn = os.path.join(destination, model.name + ".nn")
    subprocess.run("python Barracuda/Tools/keras_to_barracuda.py \"{0}\" \"{1}\"".format(modelh5, modelnn), shell=True) # run in subprocess because calls are stateful

# Enter interactive mode to inspect results or run perform additional fitting. CTRL+Z & Enter returns control to the module.
import code
code.interact(local=locals())