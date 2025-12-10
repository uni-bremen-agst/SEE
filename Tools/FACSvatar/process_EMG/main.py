import sys
import argparse
import sys
import json
import time
import numpy as np

# own imports; if statement for documentation
if __name__ == '__main__':
    sys.path.append("..")
    from facsvatarzeromq import FACSvatarZeroMQ
else:
    from modules.facsvatarzeromq import FACSvatarZeroMQ


# client to message broker server
class FACSvatarMessages(FACSvatarZeroMQ):
    """Receives FACS and Head movement data; forward to output function"""

    def __init__(self, **kwargs):
        super().__init__(**kwargs)

    async def sub(self):
        mu = 0  # Mittelwert
        sigma = 400  # Breitere Standardabweichung
        max_value = 100  # Maximaler Wert für die Kurvenspitze

        # x-Werte erzeugen (von -1000 bis 1000 in 0.1er-Schritten für mehr Datenpunkte)
        x_values = np.arange(-1500, 1500, 0.1)

        # Berechnung der y-Werte
        y_values = max_value * np.exp(-0.5 * ((x_values - mu) / sigma) ** 2)

        # init a message dict
        msg = dict()

        # metadata in message
        msg['frame'] = -1
        msg['timestamp'] = time.time()
        msg['pose'] = {"pose_Rx" : 0.0, "pose_Ry" : 0.0, "pose_Rz" : 0.0}

        for intensity in y_values:
            print(round(intensity/100, 2))
            msg['au_r'] = {"AU01": 0.0,
                            "AU02": 0.0,
                            "AU04": round(intensity/100, 2),
                            "AU05": 0.0,
                            "AU06": 0.0,
                            "AU07": 0.0,
                            "AU09": 0.0,
                            "AU10": 0.0,
                            "AU12": 0.0,
                            "AU14": 0.0,
                            "AU15": 0.0,
                            "AU17": 0.0,
                            "AU20": 0.0,
                            "AU23": 0.0,
                            "AU25": 0.0,
                            "AU26": 0.0,
                            "AU45": 0}
            await self.pub_socket.pub(msg, "gui.face_config")

if __name__ == '__main__':
    # command line arguments
    parser = argparse.ArgumentParser()

    # publisher setup commandline arguments
    parser.add_argument("--pub_ip", default=argparse.SUPPRESS,
                        help="IP (e.g. 192.168.x.x) of where to pub to; Default: 127.0.0.1 (local)")
    parser.add_argument("--pub_port", default="5570",
                        help="Port of where to pub to; Default: 5570")
    parser.add_argument("--pub_key", default="gui.face_config",
                        help="Key for filtering message; Default: openface")
    parser.add_argument("--pub_bind", default=False,
                        help="True: socket.bind() / False: socket.connect(); Default: False")

    args, leftovers = parser.parse_known_args()

    # init FACSvatar message class
    facsvatar_messages = FACSvatarMessages(**vars(args))
    # start processing messages; give list of functions to call async
    facsvatar_messages.start([facsvatar_messages.sub])