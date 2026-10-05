import json
import numpy as np
from algorithms import lbp, tikhonov, landweber, tv
from sensitivity_io import load
from measurement import vector
from image_mapping import to_image
from custom_algorithm import reconstruct as custom


def run(request_json):
    request = json.loads(request_json)
    sensitivity, xy = load(request["SensitivityPath"], request.get("CoordinatesPath"))
    u = vector(request["Measurements"], request.get("Reference"), request.get("DifferenceMode", "rel"))
    name = request["Algorithm"]
    if name == "Custom":
        values = custom(request["CustomAlgorithmPath"], u, sensitivity, xy, request)
    else:
        algorithms = {"LBP": lbp, "Tikhonov": tikhonov, "Landweber": landweber, "TV": tv}
        if name not in algorithms:
            raise ValueError("Unknown reconstruction algorithm")
        values = algorithms[name].reconstruct(u, sensitivity, xy, request)
    image = to_image(values, xy)
    if image.ndim != 2 or not np.isfinite(image).all():
        raise ValueError("Reconstruction image must be a finite 2D matrix")
    return json.dumps({"Width": image.shape[1], "Height": image.shape[0], "Values": image.ravel().tolist()}, allow_nan=False)
