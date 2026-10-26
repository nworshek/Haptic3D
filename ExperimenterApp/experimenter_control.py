import tkinter as tk
from tkinter import ttk
import socket
import serial
import time


# =========================================================
# CONNECTION SETTINGS
# =========================================================

# HoloLens / Unity
HOLOLENS_IP = "192.168.137.182"
HOLOLENS_PORT = 5055

# ESP32 / Haptic Glove
ESP32_PORT = "COM3"
ESP32_BAUD = 115200

# Haptic update rate
HAPTIC_POLL_MS = 100

# If communication with HoloLens is lost for this long,
# force all physical motors OFF.
HOLOLENS_WATCHDOG_SECONDS = 1.0


class ExperimenterControlApp:
    def __init__(self, root):
        self.root = root

        # -------------------------------------------------
        # CONNECTION OBJECTS
        # -------------------------------------------------

        self.esp32_serial = None
        self.hololens_online = False

        # -------------------------------------------------
        # HAPTIC STATE
        # -------------------------------------------------

        # Last values actually sent to the ESP32.
        self.last_motor_values = {
            1: 0,
            2: 0,
            3: 0
        }

        # Time of most recent valid HoloLens STATUS response.
        self.last_hololens_response_time = None

        # Prevent repeated OFF commands during one outage.
        self.watchdog_off_sent = False

        # Experimenter Test Mode state
        self.test_mode_active = False
        self.test_haptics_enabled = False
        self.test_mode_status = tk.StringVar(value="INACTIVE")
        self.test_surface = tk.StringVar(value="None")
        self.test_haptics_status = tk.StringVar(value="OFF")

        # Temporary remote simulated-hand control.
        self.active_move_key = None
        self.move_key_commands = {
            "w": "MOVE:FORWARD",
            "s": "MOVE:BACK",
            "a": "MOVE:LEFT",
            "d": "MOVE:RIGHT",
            "q": "MOVE:DOWN",
            "e": "MOVE:UP"
        }

        # -------------------------------------------------
        # WINDOW
        # -------------------------------------------------

        self.root.title(
            "Haptic3D Experiment Control"
        )

        self.root.geometry(
            "560x1480"
        )

        self.root.minsize(
            560,
            1000
        )

        # -------------------------------------------------
        # VARIABLES
        # -------------------------------------------------

        self.participant_id = tk.StringVar()

        # Hand the participant will use during the study.
        self.participant_hand = tk.StringVar(
            value="Right"
        )

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
            value="OFF (0%)"
        )

        # -------------------------------------------------
        # BUILD GUI
        # -------------------------------------------------

        self.build_interface()

        # Temporary hand-control keyboard bindings.
        self.root.bind("<KeyPress>", self.on_move_key_press)
        self.root.bind("<KeyRelease>", self.on_move_key_release)

        # -------------------------------------------------
        # CONNECT TO ESP32
        # -------------------------------------------------

        self.connect_esp32()

        # -------------------------------------------------
        # BEGIN HOLOLENS / HAPTIC POLLING
        # -------------------------------------------------

        self.root.after(
            HAPTIC_POLL_MS,
            self.poll_unity_status
        )

        # -------------------------------------------------
        # SAFE WINDOW CLOSE
        # -------------------------------------------------

        self.root.protocol(
            "WM_DELETE_WINDOW",
            self.on_close
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

        ttk.Label(
            participant_frame,
            text="Study Hand:"
        ).grid(
            row=1,
            column=0,
            sticky="w",
            padx=(0, 10),
            pady=(10, 0)
        )

        hand_selection_frame = ttk.Frame(
            participant_frame
        )

        hand_selection_frame.grid(
            row=1,
            column=1,
            sticky="w",
            pady=(10, 0)
        )

        self.right_hand_button = ttk.Radiobutton(
            hand_selection_frame,
            text="Right",
            variable=self.participant_hand,
            value="Right"
        )

        self.right_hand_button.pack(
            side="left",
            padx=(0, 15)
        )

        self.left_hand_button = ttk.Radiobutton(
            hand_selection_frame,
            text="Left",
            variable=self.participant_hand,
            value="Left"
        )

        self.left_hand_button.pack(
            side="left"
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


        # -------------------------------------------------
        # EXPERIMENTER TEST MODE
        # -------------------------------------------------

        test_frame = ttk.LabelFrame(main_frame, text="Experimenter Test Mode", padding=15)
        test_frame.pack(fill="x", pady=8)
        self.add_status_row(test_frame, 0, "Mode:", self.test_mode_status)
        self.add_status_row(test_frame, 1, "Surface:", self.test_surface)
        self.add_status_row(test_frame, 2, "Test Haptics:", self.test_haptics_status)

        ttk.Button(test_frame, text="ENTER TEST MODE", command=self.enter_test_mode).grid(row=3, column=0, columnspan=3, sticky="ew", padx=4, pady=(12,6))
        ttk.Button(test_frame, text="FLAT", command=lambda: self.select_test_surface("FLAT")).grid(row=4, column=0, sticky="ew", padx=4, pady=4)
        ttk.Button(test_frame, text="RIDGES", command=lambda: self.select_test_surface("RIDGES")).grid(row=4, column=1, sticky="ew", padx=4, pady=4)
        ttk.Button(test_frame, text="BUMPS", command=lambda: self.select_test_surface("BUMPS")).grid(row=4, column=2, sticky="ew", padx=4, pady=4)
        ttk.Button(test_frame, text="ENABLE HAPTICS", command=self.enable_test_haptics).grid(row=5, column=0, columnspan=2, sticky="ew", padx=4, pady=6)
        ttk.Button(test_frame, text="DISABLE HAPTICS", command=self.disable_test_haptics).grid(row=5, column=2, sticky="ew", padx=4, pady=6)
        ttk.Button(test_frame, text="EXIT TEST MODE", command=self.exit_test_mode).grid(row=6, column=0, columnspan=3, sticky="ew", padx=4, pady=(8,4))
        for column in range(3):
            test_frame.columnconfigure(column, weight=1)

        # -------------------------------------------------
        # TEMPORARY SIMULATED HAND CONTROLS
        # -------------------------------------------------

        hand_frame = ttk.LabelFrame(
            main_frame,
            text="Temporary Simulated Hand Control",
            padding=15
        )
        hand_frame.pack(fill="x", pady=8)

        ttk.Label(
            hand_frame,
            text=(
                "W/S = Forward/Back   A/D = Left/Right   "
                "Q/E = Down/Up   R = Reset"
            ),
            wraplength=450
        ).grid(row=0, column=0, columnspan=3, pady=(0, 10))

        directions = [
            ("Forward (W)", 1, 1, "MOVE:FORWARD"),
            ("Left (A)",    2, 0, "MOVE:LEFT"),
            ("Right (D)",   2, 2, "MOVE:RIGHT"),
            ("Back (S)",    3, 1, "MOVE:BACK"),
            ("Up (E)",      4, 0, "MOVE:UP"),
            ("Down (Q)",    4, 2, "MOVE:DOWN")
        ]

        for label, row, column, command in directions:
            button = ttk.Button(hand_frame, text=label)
            button.grid(
                row=row,
                column=column,
                sticky="ew",
                padx=4,
                pady=4
            )
            self.bind_hold_button(button, command)

        ttk.Button(
            hand_frame,
            text="STOP",
            command=self.stop_remote_hand
        ).grid(
            row=2,
            column=1,
            sticky="ew",
            padx=4,
            pady=4
        )

        ttk.Button(
            hand_frame,
            text="RESET HAND (R)",
            command=self.reset_remote_hand
        ).grid(
            row=5,
            column=0,
            columnspan=3,
            sticky="ew",
            padx=4,
            pady=(8, 4)
        )

        for column in range(3):
            hand_frame.columnconfigure(column, weight=1)


    # =====================================================
    # EXPERIMENTER TEST MODE
    # =====================================================

    def enter_test_mode(self):
        self.stop_remote_hand()
        self.motors_off()
        if not self.send_command("TEST:ENTER"):
            print("Failed to enter Experimenter Test Mode.")
            return
        self.test_mode_active = True
        self.test_haptics_enabled = False
        self.test_mode_status.set("ACTIVE")
        self.test_surface.set("None")
        self.test_haptics_status.set("OFF")
        print("Experimenter Test Mode enabled.")

    def select_test_surface(self, surface):
        if not self.test_mode_active:
            print("Enter Experimenter Test Mode first.")
            return
        surface = surface.upper()
        if surface not in ("FLAT", "RIDGES", "BUMPS"):
            print("Invalid test surface:", surface)
            return
        self.motors_off()
        if not self.send_command("TEST:" + surface):
            print("Failed to select test surface:", surface)
            return
        self.test_surface.set(surface.title())
        print("Experimenter test surface:", surface)

    def enable_test_haptics(self):
        if not self.test_mode_active:
            print("Enter Experimenter Test Mode first.")
            return
        if self.test_surface.get() == "None":
            print("Select a test surface before enabling haptics.")
            return
        self.motors_off()
        if not self.send_command("TEST:HAPTICS:ON"):
            print("Failed to enable Test Mode haptics.")
            return
        self.test_haptics_enabled = True
        self.test_haptics_status.set("ON")
        print("Experimenter Test Mode haptics enabled.")

    def disable_test_haptics(self):
        self.motors_off()
        if self.test_mode_active:
            self.send_command("TEST:HAPTICS:OFF")
        self.test_haptics_enabled = False
        self.test_haptics_status.set("OFF")
        print("Experimenter Test Mode haptics disabled.")

    def exit_test_mode(self):
        self.stop_remote_hand()
        self.motors_off()
        if self.test_mode_active and not self.send_command("TEST:EXIT"):
            print("Could not confirm Test Mode exit with HoloLens.")
        self.test_mode_active = False
        self.test_haptics_enabled = False
        self.test_mode_status.set("INACTIVE")
        self.test_surface.set("None")
        self.test_haptics_status.set("OFF")
        print("Experimenter Test Mode disabled.")

    # =====================================================
    # TEMPORARY REMOTE HAND CONTROL
    # =====================================================

    def bind_hold_button(self, button, command):
        button.bind(
            "<ButtonPress-1>",
            lambda event, c=command: self.start_remote_hand(c)
        )
        button.bind(
            "<ButtonRelease-1>",
            lambda event: self.stop_remote_hand()
        )


    def start_remote_hand(self, command):
        self.send_command(command)


    def stop_remote_hand(self):
        self.active_move_key = None
        self.send_command("MOVE:STOP")


    def reset_remote_hand(self):
        self.active_move_key = None
        self.send_command("MOVE:STOP")
        self.send_command("RESET_HAND")


    def on_move_key_press(self, event):
        # Allow normal typing in the Participant ID field.
        if event.widget == self.participant_entry:
            return

        key = event.keysym.lower()

        if key == "r":
            self.reset_remote_hand()
            return

        if key not in self.move_key_commands:
            return

        # Ignore Windows key-repeat while the same key is held.
        if self.active_move_key == key:
            return

        self.active_move_key = key
        self.send_command(self.move_key_commands[key])


    def on_move_key_release(self, event):
        key = event.keysym.lower()

        if key not in self.move_key_commands:
            return

        if self.active_move_key != key:
            return

        self.active_move_key = None
        self.send_command("MOVE:STOP")


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
    # ESP32 CONNECTION
    # =====================================================

    def connect_esp32(self):
        try:
            self.esp32_serial = serial.Serial(
                ESP32_PORT,
                ESP32_BAUD,
                timeout=1,
                write_timeout=1
            )

            # ESP32-S3 may reset when COM3 is opened.
            time.sleep(2)

            self.esp32_serial.reset_input_buffer()

            # -------------------------------------------------
            # SAFETY: ALWAYS START WITH MOTORS OFF
            # -------------------------------------------------

            self.esp32_serial.write(
                b"OFF\n"
            )

            self.esp32_serial.flush()

            time.sleep(0.1)

            self.esp32_serial.reset_input_buffer()

            # -------------------------------------------------
            # VERIFY CONNECTION WITH PING
            # -------------------------------------------------

            self.esp32_serial.write(
                b"PING\n"
            )

            self.esp32_serial.flush()

            response = (
                self.esp32_serial
                .readline()
                .decode(
                    errors="replace"
                )
                .strip()
            )

            if response == "PONG":
                print(
                    "ESP32 connected on",
                    ESP32_PORT
                )

                self.esp32_status.set(
                    "CONNECTED"
                )

                self.motor_status.set(
                    "OFF (0%)"
                )

                self.last_motor_values = {
                    1: 0,
                    2: 0,
                    3: 0
                }

            else:
                print(
                    "ESP32 did not return PONG. Response:",
                    response
                )

                self.esp32_status.set(
                    "NO RESPONSE"
                )

        except Exception as e:
            print(
                "Could not connect to ESP32:",
                e
            )

            self.esp32_serial = None

            self.esp32_status.set(
                "DISCONNECTED"
            )

            self.motor_status.set(
                "OFF (0%)"
            )


    # =====================================================
    # SEND COMMAND TO ESP32
    # =====================================================

    def send_esp32_command(
        self,
        command,
        quiet=False
    ):
        if (
            self.esp32_serial is None or
            not self.esp32_serial.is_open
        ):
            if not quiet:
                print(
                    "ESP32 command not sent - serial unavailable:",
                    command
                )

            return False

        try:
            self.esp32_serial.write(
                (command + "\n").encode(
                    "utf-8"
                )
            )

            self.esp32_serial.flush()

            if not quiet:
                print(
                    "Sent to ESP32:",
                    command
                )

            return True

        except Exception as e:
            print(
                "ESP32 serial write failed:",
                e
            )

            self.esp32_status.set(
                "DISCONNECTED"
            )

            return False


    # =====================================================
    # SEND ONE MOTOR VALUE
    # =====================================================

    def set_motor(
        self,
        motor_number,
        intensity
    ):
        intensity = max(
            0,
            min(
                100,
                int(intensity)
            )
        )

        # Only transmit when the value changes.
        #
        # This keeps unnecessary serial traffic down while
        # still responding to every actual haptic change.

        if (
            self.last_motor_values[motor_number]
            == intensity
        ):
            return

        command = (
            "M" +
            str(motor_number) +
            ":" +
            str(intensity)
        )

        success = self.send_esp32_command(
            command,
            quiet=True
        )

        if success:
            self.last_motor_values[
                motor_number
            ] = intensity


    # =====================================================
    # APPLY HOLOLENS HAPTIC STATE
    # =====================================================

    def apply_haptic_state(self, condition, trial_status, motor1, motor2, motor3):
        official_haptics = (condition == "Haptic Only" and trial_status.upper() == "ACTIVE")
        experimenter_haptics = self.test_mode_active and self.test_haptics_enabled
        if not (official_haptics or experimenter_haptics):
            motor1 = motor2 = motor3 = 0
        self.set_motor(1, motor1)
        self.set_motor(2, motor2)
        self.set_motor(3, motor3)
        maximum = max(motor1, motor2, motor3)
        if maximum <= 0:
            self.motor_status.set("OFF (0%)")
        else:
            self.motor_status.set("M1:{}%  M2:{}%  M3:{}%".format(motor1, motor2, motor3))


    # =====================================================
    # ALL MOTORS OFF
    # =====================================================

    def motors_off(self):
        success = self.send_esp32_command(
            "OFF"
        )

        # Locally reset our cached values even if serial
        # communication fails. If communication later
        # resumes, subsequent haptic values can be resent.

        self.last_motor_values = {
            1: 0,
            2: 0,
            3: 0
        }

        self.motor_status.set(
            "OFF (0%)"
        )

        return success


    # =====================================================
    # DISCONNECT ESP32
    # =====================================================

    def disconnect_esp32(self):
        if self.esp32_serial is None:
            return

        try:
            if self.esp32_serial.is_open:

                # ---------------------------------------------
                # SAFETY: MOTOR OFF BEFORE CLOSING SERIAL
                # ---------------------------------------------

                try:
                    self.esp32_serial.write(
                        b"OFF\n"
                    )

                    self.esp32_serial.flush()

                except Exception:
                    pass

                self.esp32_serial.close()

        except Exception as e:
            print(
                "Error while closing ESP32:",
                e
            )

        self.esp32_serial = None

        self.esp32_status.set(
            "DISCONNECTED"
        )

        self.motor_status.set(
            "OFF (0%)"
        )


    # =====================================================
    # SEND COMMAND TO HOLOLENS UNITY
    # =====================================================

    def send_command(
        self,
        command
    ):
        try:
            with socket.create_connection(
                (
                    HOLOLENS_IP,
                    HOLOLENS_PORT
                ),
                timeout=0.5
            ) as sock:

                sock.sendall(
                    (command + "\n").encode(
                        "utf-8"
                    )
                )

                response = (
                    sock.recv(
                        1024
                    )
                    .decode(
                        "utf-8"
                    )
                    .strip()
                )

            print(
                "Sent to HoloLens Unity:",
                command,
                "| Response:",
                response
            )

            self.hololens_online = True

            return True

        except Exception as e:
            print(
                "Could not send command to HoloLens Unity:",
                e
            )

            self.hololens_online = False

            return False


    # =====================================================
    # REQUEST STATUS FROM HOLOLENS UNITY
    # =====================================================

    def request_status(self):
        try:
            with socket.create_connection(
                (
                    HOLOLENS_IP,
                    HOLOLENS_PORT
                ),
                timeout=0.75
            ) as sock:

                sock.sendall(
                    b"STATUS\n"
                )

                response = (
                    sock.recv(
                        2048
                    )
                    .decode(
                        "utf-8"
                    )
                    .strip()
                )

                self.hololens_online = True

                return response

        except Exception:
            self.hololens_online = False

            return None


    # =====================================================
    # POLL HOLOLENS UNITY
    # =====================================================

    def poll_unity_status(self):
        response = self.request_status()

        current_time = time.monotonic()

        if response:
            valid = self.process_status(
                response
            )

            if valid:
                self.last_hololens_response_time = (
                    current_time
                )

                self.watchdog_off_sent = False

        # -------------------------------------------------
        # COMMUNICATION WATCHDOG
        # -------------------------------------------------

        if self.last_hololens_response_time is None:
            # We have never received a valid HoloLens
            # status packet. Keep motors OFF.

            if not self.watchdog_off_sent:
                self.motors_off()

                self.watchdog_off_sent = True

        else:
            elapsed = (
                current_time -
                self.last_hololens_response_time
            )

            if (
                elapsed >
                HOLOLENS_WATCHDOG_SECONDS
            ):
                if not self.watchdog_off_sent:
                    print(
                        "HoloLens communication timeout - motors OFF"
                    )

                    self.motors_off()

                    self.watchdog_off_sent = True

        # -------------------------------------------------
        # NEXT HAPTIC UPDATE
        # -------------------------------------------------

        self.root.after(
            HAPTIC_POLL_MS,
            self.poll_unity_status
        )


    # =====================================================
    # PROCESS HOLOLENS STATUS
    # =====================================================

    def process_status(
        self,
        response
    ):
        parts = response.split("|")

        # -------------------------------------------------
        # NEW STATUS FORMAT
        # -------------------------------------------------
        #
        # 0  STATUS
        # 1  Trial Number
        # 2  Total Trials
        # 3  Condition
        # 4  Stimulus
        # 5  Trial Status
        # 6  HoloLens-local ESP32 status
        # 7  Maximum motor intensity
        # 8  M1 - Index
        # 9  M2 - Middle
        # 10 M3 - Ring
        #
        # Example:
        #
        # STATUS|2|6|Haptic Only|Flat|ACTIVE|
        # DISCONNECTED|65|65|0|0
        # -------------------------------------------------

        if len(parts) != 11:
            return False

        if parts[0] != "STATUS":
            return False

        try:
            trial_number = parts[1]
            total_trials = parts[2]

            condition = parts[3]
            stimulus = parts[4]
            status = parts[5]

            motor1 = int(
                parts[8]
            )

            motor2 = int(
                parts[9]
            )

            motor3 = int(
                parts[10]
            )

        except (
            ValueError,
            IndexError
        ):
            return False

        # Clamp values received over the network.

        motor1 = max(
            0,
            min(
                100,
                motor1
            )
        )

        motor2 = max(
            0,
            min(
                100,
                motor2
            )
        )

        motor3 = max(
            0,
            min(
                100,
                motor3
            )
        )

        # -------------------------------------------------
        # UPDATE TRIAL GUI
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

        # -------------------------------------------------
        # FORWARD HAPTIC STATE TO PHYSICAL GLOVE
        # -------------------------------------------------

        self.apply_haptic_state(
            condition,
            status,
            motor1,
            motor2,
            motor3
        )

        return True


    # =====================================================
    # PREPARE
    # =====================================================

    def prepare_trial(self):
        if self.test_mode_active:
            print("Exit Experimenter Test Mode before preparing a participant trial.")
            return
        self.stop_remote_hand()

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

        selected_hand = (
            self.participant_hand
            .get()
            .strip()
            .upper()
        )

        if selected_hand not in ("RIGHT", "LEFT"):
            print(
                "Valid participant study hand required."
            )
            return

        # SAFETY: motors off while preparing.
        self.motors_off()

        participant_success = self.send_command(
            "PARTICIPANT:" + participant
        )

        if not participant_success:
            print(
                "Failed to send participant ID to HoloLens Unity."
            )
            return

        hand_success = self.send_command(
            "HAND:" + selected_hand
        )

        if not hand_success:
            print(
                "Failed to send participant study hand "
                "to HoloLens Unity."
            )
            return

        prepare_success = self.send_command(
            "PREPARE"
        )

        if prepare_success:
            print(
                "Prepare requested for participant:",
                participant,
                "| Study Hand:",
                selected_hand
            )


    # =====================================================
    # START
    # =====================================================

    def start_trial(self):
        if self.test_mode_active:
            print("Exit Experimenter Test Mode before starting a participant trial.")
            return
        self.stop_remote_hand()

        # Begin from a known OFF state.
        self.motors_off()

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
        self.stop_remote_hand()

        # -------------------------------------------------
        # SAFETY: PHYSICAL MOTORS OFF FIRST
        # -------------------------------------------------

        self.motors_off()

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
        if self.test_mode_active:
            print("Exit Experimenter Test Mode before repeating a participant trial.")
            return
        self.stop_remote_hand()

        # -------------------------------------------------
        # SAFETY: PHYSICAL MOTORS OFF FIRST
        # -------------------------------------------------

        self.motors_off()

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
        self.stop_remote_hand()

        print(
            "EMERGENCY STOP REQUESTED"
        )

        # -------------------------------------------------
        # FIRST: PHYSICAL GLOVE OFF
        # -------------------------------------------------

        self.motors_off()

        self.test_mode_active = False
        self.test_haptics_enabled = False
        self.test_mode_status.set("INACTIVE")
        self.test_surface.set("None")
        self.test_haptics_status.set("OFF")

        # -------------------------------------------------
        # SECOND: STOP HOLOLENS EXPERIMENT
        # -------------------------------------------------

        success = self.send_command(
            "EMERGENCY"
        )

        if success:
            print(
                "Emergency stop sent to HoloLens."
            )


    # =====================================================
    # SAFE APPLICATION CLOSE
    # =====================================================

    def on_close(self):
        try:
            self.send_command("MOVE:STOP")
        except Exception:
            pass

        print(
            "Closing experimenter application..."
        )

        # -------------------------------------------------
        # SAFETY: TURN OFF PHYSICAL MOTORS
        # -------------------------------------------------

        self.motors_off()

        # -------------------------------------------------
        # TRY TO STOP HOLOLENS EXPERIMENT
        # -------------------------------------------------

        try:
            self.send_command(
                "EMERGENCY"
            )

        except Exception:
            pass

        # -------------------------------------------------
        # CLOSE SERIAL CONNECTION
        # -------------------------------------------------

        self.disconnect_esp32()

        # -------------------------------------------------
        # CLOSE GUI
        # -------------------------------------------------

        self.root.destroy()


# =========================================================
# START APPLICATION
# =========================================================

if __name__ == "__main__":
    root = tk.Tk()

    app = ExperimenterControlApp(
        root
    )

    root.mainloop()