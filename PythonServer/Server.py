#!/usr/bin/env python3
"""
NumeroZoo Computer Vision Server (MediaPipe Tasks + TCP Socket).

Captures webcam frames, performs real-time person segmentation and 0-10 finger
counting with temporal stabilization, and streams RGBA frames + gesture counts
to Unity over a local TCP socket.
"""

import argparse
import math
import os
import signal
import socket
import struct
import sys
import threading
import time
from pathlib import Path

import cv2
import mediapipe as mp
import numpy as np
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 5555
DEFAULT_CAMERA = 0
WIDTH = 640
HEIGHT = 480
MASK_THRESHOLD = 0.35
STABILITY_TIME = 0.5
DISTANCE_THRESHOLD = 1.1


def resolve_model_path(filename: str) -> str:
    """Locate MediaPipe model files across source and PyInstaller layouts."""
    candidates = []

    if hasattr(sys, "_MEIPASS"):
        candidates.append(Path(sys._MEIPASS) / filename)

    script_dir = Path(__file__).resolve().parent
    exe_dir = Path(sys.executable).resolve().parent
    candidates.extend([
        script_dir / "models" / filename,
        script_dir / filename,
        exe_dir / "_internal" / filename,
        exe_dir / filename,
        Path.cwd() / "models" / filename,
        Path.cwd() / filename,
    ])

    for path in candidates:
        if path.is_file():
            return str(path)

    searched = ", ".join(str(p) for p in candidates)
    raise FileNotFoundError(f"Model '{filename}' not found. Searched: {searched}")


def dist(a, b) -> float:
    return math.sqrt((a.x - b.x) ** 2 + (a.y - b.y) ** 2)


def count_fingers(hand_landmarks, handedness_label: str) -> int:
    lm = hand_landmarks
    fingers = 0
    wrist = lm[0]

    if handedness_label == "Right":
        thumb_open = lm[4].x > lm[3].x
    else:
        thumb_open = lm[4].x < lm[3].x

    if thumb_open:
        fingers += 1

    finger_pairs = [(8, 6), (12, 10), (16, 14), (20, 18)]
    for tip, pip in finger_pairs:
        if dist(lm[tip], wrist) > dist(lm[pip], wrist) * DISTANCE_THRESHOLD:
            fingers += 1

    return fingers


def start_stdin_watchdog(stop_event: threading.Event) -> None:
    """Exit cleanly when Unity closes the redirected stdin pipe."""
    if sys.stdin is None or sys.stdin.isatty():
        return

    def _watch():
        try:
            while sys.stdin.read(1):
                pass
        except Exception:
            pass
        stop_event.set()

    watcher = threading.Thread(target=_watch, daemon=True)
    watcher.start()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="NumeroZoo MediaPipe Vision Server")
    parser.add_argument("--host", default=DEFAULT_HOST, help="Bind address (default: 127.0.0.1)")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT, help="TCP port (default: 5555)")
    parser.add_argument("--camera", type=int, default=DEFAULT_CAMERA, help="Webcam index (default: 0)")
    parser.add_argument("--verbose", action="store_true", help="Log stable gesture changes")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    stop_event = threading.Event()

    def _handle_signal(_signum, _frame):
        stop_event.set()

    signal.signal(signal.SIGINT, _handle_signal)
    if hasattr(signal, "SIGTERM"):
        signal.signal(signal.SIGTERM, _handle_signal)

    start_stdin_watchdog(stop_event)

    try:
        hand_model = resolve_model_path("hand_landmarker.task")
        seg_model = resolve_model_path("selfie_segmenter.tflite")
    except FileNotFoundError as exc:
        print(f"Model error: {exc}", file=sys.stderr, flush=True)
        return 1

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.settimeout(0.2)

    try:
        server.bind((args.host, args.port))
        server.listen(1)
    except OSError as exc:
        print(f"Socket bind failed on {args.host}:{args.port}: {exc}", file=sys.stderr, flush=True)
        server.close()
        return 1

    cap = cv2.VideoCapture(args.camera)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, WIDTH)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, HEIGHT)

    if not cap.isOpened():
        print(f"Warning: could not open camera index {args.camera} at startup.", file=sys.stderr, flush=True)

    hand_options = vision.HandLandmarkerOptions(
        base_options=python.BaseOptions(model_asset_path=hand_model),
        num_hands=2,
        min_hand_detection_confidence=0.5,
        min_hand_presence_confidence=0.5,
        min_tracking_confidence=0.5,
    )
    seg_options = vision.ImageSegmenterOptions(
        base_options=python.BaseOptions(model_asset_path=seg_model),
        output_confidence_masks=True,
    )

    print(f"Vision server listening on {args.host}:{args.port} (camera={args.camera})", flush=True)

    rgba_buffer = np.empty((HEIGHT, WIDTH, 4), dtype=np.uint8)

    try:
        with vision.ImageSegmenter.create_from_options(seg_options) as segmenter, \
             vision.HandLandmarker.create_from_options(hand_options) as detector:

            while not stop_event.is_set():
                try:
                    conn, addr = server.accept()
                except socket.timeout:
                    continue
                except OSError:
                    break

                conn.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                print(f"Unity client connected: {addr}", flush=True)

                stable_gesture = 0
                last_valid_gesture = 0
                candidate_gesture = 0
                candidate_start = time.time()
                last_logged_gesture = -1

                try:
                    while not stop_event.is_set():
                        if not cap.isOpened():
                            cap.open(args.camera)
                            time.sleep(0.1)
                            continue

                        ret, frame = cap.read()
                        if not ret or frame is None:
                            time.sleep(0.02)
                            continue

                        if frame.shape[1] != WIDTH or frame.shape[0] != HEIGHT:
                            frame = cv2.resize(frame, (WIDTH, HEIGHT))

                        frame = cv2.flip(frame, 1)
                        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=frame_rgb)

                        # Person silhouette segmentation
                        seg_result = segmenter.segment(mp_image)
                        confidence = np.squeeze(seg_result.confidence_masks[0].numpy_view())
                        mask_float = np.where(confidence >= MASK_THRESHOLD, confidence, 0.0)
                        mask = (mask_float * 255.0).clip(0, 255).astype(np.uint8)

                        # Hand detection and 0-10 finger counting
                        result = detector.detect(mp_image)
                        hands_detected = 0
                        gesture_value = last_valid_gesture

                        if result.hand_landmarks and result.handedness:
                            hands_detected = len(result.hand_landmarks)
                            current_value = 0

                            for landmarks, handedness in zip(result.hand_landmarks, result.handedness):
                                label = handedness[0].category_name
                                current_value += count_fingers(landmarks, label)

                            current_value = min(current_value, 10)
                            gesture_value = current_value
                            last_valid_gesture = current_value

                            if gesture_value != candidate_gesture:
                                candidate_gesture = gesture_value
                                candidate_start = time.time()

                        if hands_detected > 0 and (time.time() - candidate_start) >= STABILITY_TIME:
                            stable_gesture = candidate_gesture

                        if args.verbose and stable_gesture != last_logged_gesture:
                            print(f"Stable gesture: {stable_gesture} (hands={hands_detected})", flush=True)
                            last_logged_gesture = stable_gesture

                        rgba_buffer[:, :, :3] = frame_rgb
                        rgba_buffer[:, :, 3] = mask
                        payload = rgba_buffer.tobytes()
                        header = struct.pack("<IIII", WIDTH, HEIGHT, len(payload), stable_gesture)
                        conn.sendall(header + payload)

                except (BrokenPipeError, ConnectionResetError, OSError):
                    print("Unity client disconnected.", flush=True)
                finally:
                    try:
                        conn.close()
                    except OSError:
                        pass
    finally:
        cap.release()
        server.close()
        print("Vision server stopped cleanly.", flush=True)

    return 0


if __name__ == "__main__":
    os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
    os.environ.setdefault("GLOG_minloglevel", "2")
    sys.exit(main())
