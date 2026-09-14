import tkinter as tk
from tkinter import ttk
import socket


class ExperimenterControlApp:
    def __init__(self, root):
        self.root = root

        self.root.title(
            "Haptic3D Experiment Control"
        )

        self.root.geometry(
            "520x760"
        )

        self.root.minsize(
            520,
            760
        )

        # -------------------------------------------------
        # VARIABLES
        # -------------------------------------------------

        self.participant_id = tk.StringVar()

        self.trial_number = tk.StringVar(
            value="0 / 6"
        )

        self.condition = tk.StringVar(
            value="None"
        )

        self.stimulus = tk.StringVar(
            value="None"
        )

        self.trial_status = tk.StringVar(
            value="WAITING"
        )

        self.esp32_status = tk.StringVar(
            value="DISCONNECTED"
        )

        self.motor_status = tk.StringVar(
            value="OFF"
        )

        self.build_interface()

        # Begin requesting status from Unity.
        self.root.after(
            250,
            self.poll_unity_status
        )


    # =====================================================
    # INTERFACE
    # =====================================================

    def build_interface(self):
        main_frame = ttk.Frame(
            self.root,
            padding=20
        )

        main_frame.pack(
            fill="both",
            expand=True
        )

        # -------------------------------------------------
        # TITLE
        # -------------------------------------------------

        title = ttk.Label(
            main_frame,
            text="Haptic3D Experiment Control",
            font=("Segoe UI", 18, "bold")
        )

        title.pack(
            pady=(0, 20)
        )


        # -------------------------------------------------
        # PARTICIPANT
        # -------------------------------------------------

        participant_frame = ttk.LabelFrame(
            main_frame,
            text="Participant",
            padding=15
        )

        participant_frame.pack(
            fill="x",
            pady=8
        )

        ttk.Label(
            participant_frame,
            text="Participant ID:"
        ).grid(
            row=0,
            column=0,
            sticky="w",
            padx=(0, 10)
        )

        self.participant_entry = ttk.Entry(
            participant_frame,
            textvariable=self.participant_id,
            width=25
        )

        self.participant_entry.grid(
            row=0,
            column=1,
            sticky="ew"
        )

        participant_frame.columnconfigure(
            1,
            weight=1
        )


        # -------------------------------------------------
        # TRIAL INFORMATION
        # -------------------------------------------------

        trial_frame = ttk.LabelFrame(
            main_frame,
            text="Current Trial",
            padding=15
        )

        trial_frame.pack(
            fill="x",
            pady=8
        )

        self.add_status_row(
            trial_frame,
            0,
            "Trial:",
            self.trial_number
        )

        self.add_status_row(
            trial_frame,
            1,
            "Condition:",
            self.condition
        )

        self.add_status_row(
            trial_frame,
            2,
            "Stimulus:",
            self.stimulus
        )

        self.add_status_row(
            trial_frame,
            3,
            "Status:",
            self.trial_status
        )


        # -------------------------------------------------
        # HARDWARE
        # -------------------------------------------------

        hardware_frame = ttk.LabelFrame(
            main_frame,
            text="Hardware",
            padding=15
        )

        hardware_frame.pack(
            fill="x",
            pady=8
        )

        self.add_status_row(
            hardware_frame,
            0,
            "ESP32:",
            self.esp32_status
        )

        self.add_status_row(
            hardware_frame,
            1,
            "Motor:",
            self.motor_status
        )


        # -------------------------------------------------
        # CONTROLS
        # -------------------------------------------------

        controls_frame = ttk.LabelFrame(
            main_frame,
            text="Trial Controls",
            padding=15
        )

        controls_frame.pack(
            fill="x",
            pady=8
        )

        self.prepare_button = ttk.Button(
            controls_frame,
            text="PREPARE TRIAL",
            command=self.prepare_trial
        )

        self.prepare_button.grid(
            row=0,
            column=0,
            columnspan=2,
            sticky="ew",
            padx=5,
            pady=6
        )


        self.start_button = ttk.Button(
            controls_frame,
            text="START",
            command=self.start_trial
        )

        self.start_button.grid(
            row=1,
            column=0,
            sticky="ew",
            padx=5,
            pady=6
        )


        self.end_button = ttk.Button(
            controls_frame,
            text="END",
            command=self.end_trial
        )

        self.end_button.grid(
            row=1,
            column=1,
            sticky="ew",
            padx=5,
            pady=6
        )


        self.repeat_button = ttk.Button(
            controls_frame,
            text="REPEAT",
            command=self.repeat_trial
        )

        self.repeat_button.grid(
            row=2,
            column=0,
            columnspan=2,
            sticky="ew",
            padx=5,
            pady=6
        )


        separator = ttk.Separator(
            controls_frame,
            orient="horizontal"
        )

        separator.grid(
            row=3,
            column=0,
            columnspan=2,
            sticky="ew",
            pady=15
        )


        self.emergency_button = tk.Button(
            controls_frame,
            text="EMERGENCY STOP",
            font=("Segoe UI", 12, "bold"),
            command=self.emergency_stop
        )

        self.emergency_button.grid(
            row=4,
            column=0,
            columnspan=2,
            sticky="ew",
            padx=5,
            pady=6,
            ipady=8
        )


        controls_frame.columnconfigure(
            0,
            weight=1
        )

        controls_frame.columnconfigure(
            1,
            weight=1
        )


    # =====================================================
    # STATUS ROW
    # =====================================================

    def add_status_row(
        self,
        parent,
        row,
        label_text,
        variable
    ):
        ttk.Label(
            parent,
            text=label_text
        ).grid(
            row=row,
            column=0,
            sticky="w",
            pady=4
        )

        ttk.Label(
            parent,
            textvariable=variable,
            font=("Segoe UI", 10, "bold")
        ).grid(
            row=row,
            column=1,
            sticky="w",
            padx=(15, 0),
            pady=4
        )


    # =====================================================
    # SEND COMMAND TO UNITY
    # =====================================================

    def send_command(
        self,
        command
    ):
        try:
            with socket.create_connection(
                ("127.0.0.1", 5055),
                timeout=0.5
            ) as sock:

                sock.sendall(
                    (command + "\n").encode(
                        "utf-8"
                    )
                )

                # Unity sends an acknowledgement.
                response = sock.recv(
                    1024
                ).decode(
                    "utf-8"
                ).strip()

            print(
                "Sent to Unity:",
                command,
                "| Response:",
                response
            )

            return True

        except Exception as e:
            print(
                "Could not send command to Unity:",
                e
            )

            return False


    # =====================================================
    # REQUEST STATUS FROM UNITY
    # =====================================================

    def request_status(self):
        try:
            with socket.create_connection(
                ("127.0.0.1", 5055),
                timeout=0.25
            ) as sock:

                sock.sendall(
                    b"STATUS\n"
                )

                response = sock.recv(
                    2048
                ).decode(
                    "utf-8"
                ).strip()

                return response

        except Exception:
            return None


    # =====================================================
    # POLL UNITY
    # =====================================================

    def poll_unity_status(self):
        response = self.request_status()

        if response:
            self.process_status(
                response
            )

        else:
            # Unity is not currently reachable.
            self.esp32_status.set(
                "UNITY OFFLINE"
            )

        # Request another update in 250 ms.
        self.root.after(
            250,
            self.poll_unity_status
        )


    # =====================================================
    # PROCESS UNITY STATUS
    # =====================================================

    def process_status(
        self,
        response
    ):
        parts = response.split("|")

        # Expected:
        #
        # STATUS
        # trial number
        # total trials
        # condition
        # stimulus
        # trial status
        # ESP32 status
        # motor intensity

        if len(parts) != 8:
            return

        if parts[0] != "STATUS":
            return

        trial_number = parts[1]
        total_trials = parts[2]
        condition = parts[3]
        stimulus = parts[4]
        status = parts[5]
        esp32 = parts[6]

        try:
            motor_intensity = int(
                parts[7]
            )

        except ValueError:
            motor_intensity = 0


        # -------------------------------------------------
        # UPDATE GUI
        # -------------------------------------------------

        self.trial_number.set(
            trial_number +
            " / " +
            total_trials
        )

        self.condition.set(
            condition
        )

        self.stimulus.set(
            stimulus
        )

        self.trial_status.set(
            status
        )

        self.esp32_status.set(
            esp32
        )

        if motor_intensity <= 0:
            self.motor_status.set(
                "OFF (0%)"
            )

        else:
            self.motor_status.set(
                str(motor_intensity) +
                "%"
            )


    # =====================================================
    # PREPARE
    # =====================================================

    def prepare_trial(self):
        participant = (
            self.participant_id
            .get()
            .strip()
        )

        if participant == "":
            print(
                "Participant ID required."
            )

            return


        participant_success = (
            self.send_command(
                "PARTICIPANT:" +
                participant
            )
        )

        if not participant_success:
            print(
                "Failed to send participant ID to Unity."
            )

            return


        prepare_success = (
            self.send_command(
                "PREPARE"
            )
        )

        if prepare_success:
            print(
                "Prepare requested for participant:",
                participant
            )


    # =====================================================
    # START
    # =====================================================

    def start_trial(self):
        success = self.send_command(
            "START"
        )

        if success:
            print(
                "Start requested."
            )


    # =====================================================
    # END
    # =====================================================

    def end_trial(self):
        success = self.send_command(
            "END"
        )

        if success:
            print(
                "End requested."
            )


    # =====================================================
    # REPEAT
    # =====================================================

    def repeat_trial(self):
        success = self.send_command(
            "REPEAT"
        )

        if success:
            print(
                "Repeat requested."
            )


    # =====================================================
    # EMERGENCY STOP
    # =====================================================

    def emergency_stop(self):
        success = self.send_command(
            "EMERGENCY"
        )

        if success:
            print(
                "Emergency stop requested."
            )


# =========================================================
# START APPLICATION
# =========================================================

if __name__ == "__main__":
    root = tk.Tk()

    app = ExperimenterControlApp(
        root
    )

    root.mainloop()