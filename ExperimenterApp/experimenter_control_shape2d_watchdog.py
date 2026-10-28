import tkinter as tk
from tkinter import ttk, messagebox
import socket
import serial
import time
# =========================================================
# CONNECTION SETTINGS
# =========================================================
# HoloLens / Unity
HOLOLENS_IP = "192.168.1.48"
HOLOLENS_PORT = 5055
# ESP32 / Haptic Glove
ESP32_PORT = "COM3"
ESP32_BAUD = 115200
# Haptic update rate
HAPTIC_POLL_MS = 100
ESP32_HEARTBEAT_MS = 250
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
        self.last_esp32_heartbeat_time = 0.0
        # Shape2D Experimenter Test Mode state
        self.participant_trial_active = False
        self.shape2d_test_mode_active = False
        self.shape2d_test_haptics_enabled = False
        self.shape2d_test_mode_status = tk.StringVar(value="INACTIVE")
        self.shape2d_test_shape = tk.StringVar(value="None")
        self.shape2d_test_haptics_status = tk.StringVar(value="OFF")
        # -------------------------------------------------
        # WINDOW
        # -------------------------------------------------
        self.root.title(
            "Shape2D Experiment Control"
        )
        self.root.geometry(
            "600x1050"
        )
        self.root.minsize(
            560,
            900
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
            value="0 / 3"
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
            text="Shape2D Experiment Control",
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
        # PARTICIPANT TRIAL CONTROLS
        # -------------------------------------------------
        control_frame = ttk.LabelFrame(main_frame, text="Participant Trials (Square, Circle, Triangle)", padding=12)
        control_frame.pack(fill="x", pady=8)
        ttk.Button(control_frame, text="PREPARE NEXT TRIAL", command=self.prepare_participant_trial).grid(row=0, column=0, columnspan=3, sticky="ew", padx=4, pady=4)
        ttk.Button(control_frame, text="START TRIAL", command=self.start_participant_trial).grid(row=1, column=0, sticky="ew", padx=4, pady=4)
        ttk.Button(control_frame, text="END TRIAL", command=self.end_participant_trial).grid(row=1, column=1, columnspan=2, sticky="ew", padx=4, pady=4)
        for i in range(3):
            control_frame.columnconfigure(i, weight=1)

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
        # SHAPE2D EXPERIMENTER TEST MODE
        # -------------------------------------------------
        shape2d_frame = ttk.LabelFrame(main_frame, text="Shape2D Experimenter Test Mode", padding=15)
        shape2d_frame.pack(fill="x", pady=8)
        self.add_status_row(shape2d_frame, 0, "Mode:", self.shape2d_test_mode_status)
        self.add_status_row(shape2d_frame, 1, "Shape:", self.shape2d_test_shape)
        self.add_status_row(shape2d_frame, 2, "Test Haptics:", self.shape2d_test_haptics_status)
        ttk.Button(shape2d_frame, text="CALIBRATE SHAPE2D", command=self.calibrate_shape2d).grid(row=3, column=0, columnspan=3, sticky="ew", padx=4, pady=(12,6))
        ttk.Button(shape2d_frame, text="ENTER SHAPE2D TEST MODE", command=self.enter_shape2d_test_mode).grid(row=4, column=0, columnspan=3, sticky="ew", padx=4, pady=6)
        ttk.Button(shape2d_frame, text="SQUARE", command=lambda: self.select_shape2d_test_shape("SQUARE")).grid(row=5, column=0, sticky="ew", padx=4, pady=4)
        ttk.Button(shape2d_frame, text="CIRCLE", command=lambda: self.select_shape2d_test_shape("CIRCLE")).grid(row=5, column=1, sticky="ew", padx=4, pady=4)
        ttk.Button(shape2d_frame, text="TRIANGLE", command=lambda: self.select_shape2d_test_shape("TRIANGLE")).grid(row=5, column=2, sticky="ew", padx=4, pady=4)
        ttk.Button(shape2d_frame, text="ENABLE HAPTICS", command=self.enable_shape2d_test_haptics).grid(row=6, column=0, columnspan=2, sticky="ew", padx=4, pady=6)
        ttk.Button(shape2d_frame, text="DISABLE HAPTICS", command=self.disable_shape2d_test_haptics).grid(row=6, column=2, sticky="ew", padx=4, pady=6)
        ttk.Button(shape2d_frame, text="EXIT SHAPE2D TEST MODE", command=self.exit_shape2d_test_mode).grid(row=7, column=0, columnspan=3, sticky="ew", padx=4, pady=(8,4))
        self.emergency_button = tk.Button(
            shape2d_frame,
            text="SHAPE2D EMERGENCY STOP",
            font=("Segoe UI", 12, "bold"),
            command=self.emergency_stop
        )
        self.emergency_button.grid(
            row=8, column=0, columnspan=3,
            sticky="ew", padx=4, pady=(12,4), ipady=8
        )
        for column in range(3):
            shape2d_frame.columnconfigure(column, weight=1)
    # =====================================================
    # PARTICIPANT TRIAL COMMANDS
    # =====================================================
    def prepare_participant_trial(self):
        if self.shape2d_test_mode_active or self.participant_trial_active:
            messagebox.showwarning("Shape2D", "Exit test mode and end the active trial first.")
            return
        if not self.participant_id.get().strip():
            messagebox.showwarning("Shape2D", "Enter a coded participant ID first.")
            return
        if self.trial_status.get() == "EXPERIMENT COMPLETE":
            messagebox.showinfo("Shape2D", "All three trials are complete.")
            return
        self.motors_off()
        if not self.send_command("PARTICIPANT:" + self.participant_id.get().strip()):
            return
        if not self.send_command("HAND:" + self.participant_hand.get().upper()):
            return
        self.send_command("SHAPE2D:TRIAL:PREPARE")

    def start_participant_trial(self):
        if self.shape2d_test_mode_active:
            messagebox.showwarning("Shape2D", "Exit test mode first.")
            return
        if self.trial_status.get() != "PREPARED":
            messagebox.showwarning("Shape2D", "Prepare a trial before starting.")
            return
        if self.esp32_serial is None:
            messagebox.showwarning("Shape2D", "ESP32 is disconnected. Reconnect before starting.")
            return
        if self.send_command("SHAPE2D:TRIAL:START"):
            # Motor output is still gated by Unity STATUS=ACTIVE.
            self.participant_trial_active = True

    def end_participant_trial(self):
        self.motors_off()
        if self.send_command("SHAPE2D:TRIAL:END"):
            self.participant_trial_active = False

    # =====================================================
    # SHAPE2D EXPERIMENTER TEST MODE
    # =====================================================
    def calibrate_shape2d(self):
        self.motors_off()
        if self.shape2d_test_haptics_enabled:
            self.disable_shape2d_test_haptics()
        if self.send_command("SHAPE2D:CALIBRATE"):
            print("Shape2D calibration requested.")
    def enter_shape2d_test_mode(self):
        self.motors_off()
        if not self.send_command("SHAPE2D:TEST:ENTER"):
            print("Failed to enter Shape2D Test Mode.")
            return
        self.shape2d_test_mode_active = True
        self.shape2d_test_haptics_enabled = False
        self.shape2d_test_mode_status.set("ACTIVE")
        self.shape2d_test_shape.set("None")
        self.shape2d_test_haptics_status.set("OFF")
        print("Shape2D Test Mode enabled.")
    def select_shape2d_test_shape(self, shape):
        if not self.shape2d_test_mode_active:
            print("Enter Shape2D Test Mode first.")
            return
        shape = shape.upper()
        if shape not in ("SQUARE", "CIRCLE", "TRIANGLE"):
            print("Invalid Shape2D test shape:", shape)
            return
        self.motors_off()
        if not self.send_command("SHAPE2D:TEST:" + shape):
            print("Failed to select Shape2D test shape:", shape)
            return
        self.shape2d_test_shape.set(shape.title())
        print("Shape2D test shape:", shape)
    def enable_shape2d_test_haptics(self):
        if not self.shape2d_test_mode_active:
            print("Enter Shape2D Test Mode first.")
            return
        if self.shape2d_test_shape.get() == "None":
            print("Select a Shape2D test shape before enabling haptics.")
            return
        self.motors_off()
        if not self.send_command("SHAPE2D:TEST:HAPTICS:ON"):
            print("Failed to enable Shape2D test haptics.")
            return
        self.shape2d_test_haptics_enabled = True
        self.shape2d_test_haptics_status.set("ON")
        print("Shape2D Test Mode haptics enabled.")
    def disable_shape2d_test_haptics(self):
        self.motors_off()
        if self.shape2d_test_mode_active:
            self.send_command("SHAPE2D:TEST:HAPTICS:OFF")
        self.shape2d_test_haptics_enabled = False
        self.shape2d_test_haptics_status.set("OFF")
        print("Shape2D Test Mode haptics disabled.")
    def exit_shape2d_test_mode(self):
        self.motors_off()
        if self.shape2d_test_mode_active and not self.send_command("SHAPE2D:TEST:EXIT"):
            print("Could not confirm Shape2D Test Mode exit with HoloLens.")
        self.shape2d_test_mode_active = False
        self.shape2d_test_haptics_enabled = False
        self.shape2d_test_mode_status.set("INACTIVE")
        self.shape2d_test_shape.set("None")
        self.shape2d_test_haptics_status.set("OFF")
        print("Shape2D Test Mode disabled.")
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
        # Permit output only in explicitly enabled test mode OR
        # during an active participant trial confirmed by Unity STATUS.
        test_allowed = self.shape2d_test_mode_active and self.shape2d_test_haptics_enabled
        participant_allowed = (not self.shape2d_test_mode_active and
                               self.participant_trial_active and
                               condition == "Haptic Only" and trial_status == "ACTIVE")
        if not (test_allowed or participant_allowed):
            motor1 = motor2 = motor3 = 0
        self.set_motor(1, motor1)
        self.set_motor(2, motor2)
        self.set_motor(3, motor3)
        maximum = max(motor1, motor2, motor3)
        if maximum <= 0:
            self.motor_status.set("OFF (0%)")
        else:
            self.motor_status.set(
                "M1:{}%  M2:{}%  M3:{}%".format(
                    motor1, motor2, motor3
                )
            )
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
            "OFF (0%)" if success else "OFF NOT CONFIRMED - CHECK HARDWARE"
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
    def send_esp32_heartbeat(self, now):
        """Heartbeat only while the Unity STATUS connection is healthy."""
        if (now - self.last_esp32_heartbeat_time) * 1000 < ESP32_HEARTBEAT_MS:
            return
        if self.send_esp32_command("PING", quiet=True):
            self.last_esp32_heartbeat_time = now
            # PING produces PONG. Discard diagnostics/acknowledgements
            # so serial input does not accumulate indefinitely.
            try:
                self.esp32_serial.reset_input_buffer()
            except (serial.SerialException, OSError):
                pass
        else:
            self.disable_local_test_mode_after_outage()
            self.motors_off()

    def disable_local_test_mode_after_outage(self):
        """Require explicit re-arming after a connection failure."""
        self.shape2d_test_mode_active = False
        self.shape2d_test_haptics_enabled = False
        self.shape2d_test_mode_status.set("INACTIVE")
        self.shape2d_test_shape.set("None")
        self.shape2d_test_haptics_status.set("OFF")

    def poll_unity_status(self):
        response = self.request_status()
        now = time.monotonic()
        valid = bool(response) and self.process_status(response)
        if valid:
            self.last_hololens_response_time = now
            self.watchdog_off_sent = False
            self.send_esp32_heartbeat(now)
        else:
            # Fail closed immediately on a missing/malformed STATUS.
            # Do not keep the firmware watchdog alive during an outage.
            if not self.watchdog_off_sent:
                print("HoloLens STATUS missing/invalid - motors OFF")
                self.disable_local_test_mode_after_outage()
                self.motors_off()
                self.watchdog_off_sent = True
        self.root.after(HAPTIC_POLL_MS, self.poll_unity_status)

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
        if status in ("COMPLETE", "IDLE", "EXPERIMENT COMPLETE", "TEST MODE"):
            self.participant_trial_active = False
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
    # EMERGENCY STOP
    # =====================================================
    def emergency_stop(self):
        self.participant_trial_active = False
        print("SHAPE2D EMERGENCY STOP REQUESTED")
        # FIRST: physical glove OFF.
        self.motors_off()
        self.shape2d_test_mode_active = False
        self.shape2d_test_haptics_enabled = False
        self.shape2d_test_mode_status.set("INACTIVE")
        self.shape2d_test_shape.set("None")
        self.shape2d_test_haptics_status.set("OFF")
        # SECOND: stop Shape2D on Unity/HoloLens.
        success = self.send_command("SHAPE2D:EMERGENCY")
        if success:
            print("Shape2D emergency stop sent to HoloLens.")
    # =====================================================
    # SAFE APPLICATION CLOSE
    # =====================================================
    def on_close(self):
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
            self.send_command("SHAPE2D:EMERGENCY")
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
