
import fnmatch
import os
import shutil
import threading
import queue
from pathlib import Path

import tkinter as tk
from tkinter import ttk, filedialog, messagebox


# ============================================================
# FILE COLLECTOR V1
# ============================================================

APP_TITLE = "File Collector V1"


# ============================================================
# FILTER PRESETS
# ============================================================

FILTER_PRESETS = {

    "Visual Studio / .NET": [
        ".vs/",
        "bin/",
        "obj/",
        "Debug/",
        "Release/",
        "x64/",
        "x86/",
        "*.user",
        "*.suo",
        "*.userosscache",
        "*.sln.docstates",
        "*.VC.db",
        "*.VC.VC.opendb",
    ],

    "Git": [
        ".git/",
    ],

    "Temporary": [
        "*.tmp",
        "*.temp",
        "*.bak",
        "*.old",
        "*.orig",
        "*.swp",
        "*.swo",
        "*~",
        ".DS_Store",
        "Thumbs.db",
        "ehthumbs.db",
    ],

    "Node.js": [
        "node_modules/",
        ".npm/",
        ".parcel-cache/",
        ".next/",
        ".nuxt/",
        ".cache/",
        "coverage/",
        "dist/",
    ],

    "Python": [
        "__pycache__/",
        "*.pyc",
        "*.pyo",
        "*.pyd",
        ".pytest_cache/",
        ".mypy_cache/",
        ".ruff_cache/",
        ".venv/",
        "venv/",
        "env/",
        ".tox/",
        "htmlcov/",
    ],
}


# ============================================================
# FILTER ENGINE
# ============================================================

class FilterEngine:

    def __init__(self):
        self.patterns = []

    def set_patterns(self, patterns):

        self.patterns = []

        for pattern in patterns:

            pattern = pattern.strip()

            if not pattern:
                continue

            if pattern.startswith("#"):
                continue

            self.patterns.append(
                pattern.replace("\\", "/")
            )

    def should_ignore(self, file_path, root_path):

        try:
            relative = file_path.relative_to(root_path)

        except ValueError:
            return False

        relative_path = str(relative).replace(
            "\\",
            "/"
        )

        parts = relative_path.split("/")

        filename = parts[-1]

        for pattern in self.patterns:

            directory_rule = pattern.endswith("/")

            if directory_rule:

                pattern = pattern.rstrip("/")

                # Match directory components
                for part in parts[:-1]:

                    if fnmatch.fnmatch(
                        part,
                        pattern
                    ):
                        return True

                # Match complete relative path
                if fnmatch.fnmatch(
                    relative_path,
                    pattern
                ):
                    return True

                continue

            # Match relative path
            if fnmatch.fnmatch(
                relative_path,
                pattern
            ):
                return True

            # Match filename
            if fnmatch.fnmatch(
                filename,
                pattern
            ):
                return True

            # Match any component
            for part in parts:

                if fnmatch.fnmatch(
                    part,
                    pattern
                ):
                    return True

        return False

    def should_ignore_directory(
        self,
        directory_path,
        root_path
    ):

        try:

            relative = directory_path.relative_to(
                root_path
            )

        except ValueError:

            return False

        relative_path = str(relative).replace(
            "\\",
            "/"
        )

        directory_name = directory_path.name

        for pattern in self.patterns:

            if not pattern.endswith("/"):
                continue

            pattern = pattern.rstrip("/")

            if fnmatch.fnmatch(
                directory_name,
                pattern
            ):
                return True

            if fnmatch.fnmatch(
                relative_path,
                pattern
            ):
                return True

        return False


# ============================================================
# FILE SCANNER
# ============================================================

class FileScanner:

    def __init__(
        self,
        sources,
        destination,
        filter_engine
    ):

        self.sources = sources
        self.destination = destination
        self.filter_engine = filter_engine

    def scan(
        self,
        progress_callback=None,
        cancel_event=None
    ):

        files = []

        # Prevent the same physical file from being
        # processed multiple times if source folders overlap.
        seen_files = set()

        destination_resolved = (
            self.destination.resolve()
        )

        for source in self.sources:

            source = source.resolve()

            if not source.exists():
                continue

            if not source.is_dir():
                continue

            for root, dirs, filenames in os.walk(
                source
            ):

                if (
                    cancel_event
                    and cancel_event.is_set()
                ):
                    return files

                root_path = Path(root)

                # ------------------------------------------------
                # Remove ignored directories.
                # This prevents os.walk from entering them.
                # ------------------------------------------------

                filtered_dirs = []

                for directory in dirs:

                    directory_path = (
                        root_path / directory
                    )

                    if self.filter_engine.should_ignore_directory(
                        directory_path,
                        source
                    ):
                        continue

                    # Never enter destination
                    try:

                        directory_path.resolve().relative_to(
                            destination_resolved
                        )

                        continue

                    except ValueError:
                        pass

                    filtered_dirs.append(
                        directory
                    )

                dirs[:] = filtered_dirs

                # ------------------------------------------------
                # Process files
                # ------------------------------------------------

                for filename in filenames:

                    if (
                        cancel_event
                        and cancel_event.is_set()
                    ):
                        return files

                    file_path = (
                        root_path / filename
                    )

                    try:

                        resolved = (
                            file_path.resolve()
                        )

                    except OSError:

                        continue

                    # Never process destination files
                    try:

                        resolved.relative_to(
                            destination_resolved
                        )

                        continue

                    except ValueError:
                        pass

                    # Avoid duplicate files from overlapping
                    # source folders.
                    key = str(resolved).lower()

                    if key in seen_files:
                        continue

                    seen_files.add(key)

                    # Apply filters
                    if self.filter_engine.should_ignore(
                        file_path,
                        source
                    ):
                        continue

                    try:

                        if not file_path.is_file():
                            continue

                        size = file_path.stat().st_size

                    except OSError:

                        continue

                    files.append({
                        "source": file_path,
                        "size": size
                    })

                    if progress_callback:

                        progress_callback(
                            len(files),
                            file_path
                        )

        return files


# ============================================================
# COPY ENGINE
# ============================================================

class CopyEngine:

    def __init__(self, destination):

        self.destination = destination

    def get_unique_destination(
        self,
        filename
    ):

        target = (
            self.destination / filename
        )

        if not target.exists():
            return target

        counter = 1

        stem = target.stem
        suffix = target.suffix

        while True:

            candidate = (
                self.destination
                / f"{stem}_{counter}{suffix}"
            )

            if not candidate.exists():
                return candidate

            counter += 1

    def copy_files(
        self,
        files,
        progress_callback=None,
        log_callback=None,
        cancel_event=None
    ):

        self.destination.mkdir(
            parents=True,
            exist_ok=True
        )

        copied = 0
        failed = 0
        total_bytes = 0

        for index, item in enumerate(
            files,
            start=1
        ):

            if (
                cancel_event
                and cancel_event.is_set()
            ):
                break

            source = item["source"]

            try:

                target = (
                    self.get_unique_destination(
                        source.name
                    )
                )

                shutil.copy2(
                    source,
                    target
                )

                copied += 1

                total_bytes += item["size"]

                if log_callback:

                    log_callback(
                        f"[COPIED] {source}"
                    )

                    log_callback(
                        f"         -> {target}"
                    )

                if progress_callback:

                    progress_callback(
                        index,
                        len(files),
                        copied,
                        failed,
                        source
                    )

            except Exception as error:

                failed += 1

                if log_callback:

                    log_callback(
                        f"[ERROR] {source}: {error}"
                    )

                if progress_callback:

                    progress_callback(
                        index,
                        len(files),
                        copied,
                        failed,
                        source
                    )

        return {
            "copied": copied,
            "failed": failed,
            "bytes": total_bytes
        }


# ============================================================
# MAIN APPLICATION
# ============================================================

class FileCollectorApp:

    def __init__(self, root):

        self.root = root

        self.root.title(
            APP_TITLE
        )

        self.root.geometry(
            "1000x760"
        )

        self.root.minsize(
            850,
            650
        )

        # ----------------------------------------------------
        # APPLICATION STATE
        # ----------------------------------------------------

        self.sources = []

        self.destination = None

        # IMPORTANT:
        # This list survives Preview -> Copy.
        self.scanned_files = []

        self.filter_engine = (
            FilterEngine()
        )

        self.worker_thread = None

        self.cancel_event = (
            threading.Event()
        )

        self.ui_queue = (
            queue.Queue()
        )

        # ----------------------------------------------------
        # UI
        # ----------------------------------------------------

        self.create_style()
        self.create_ui()

        self.process_queue()

    # ========================================================
    # STYLE
    # ========================================================

    def create_style(self):

        style = ttk.Style()

        try:

            style.theme_use(
                "vista"
            )

        except tk.TclError:
            pass

        style.configure(
            "Title.TLabel",
            font=(
                "Segoe UI",
                18,
                "bold"
            )
        )

        style.configure(
            "Section.TLabel",
            font=(
                "Segoe UI",
                11,
                "bold"
            )
        )

        style.configure(
            "Action.TButton",
            font=(
                "Segoe UI",
                10,
                "bold"
            )
        )

    # ========================================================
    # CREATE UI
    # ========================================================

    def create_ui(self):

        main = ttk.Frame(
            self.root,
            padding=15
        )

        main.pack(
            fill="both",
            expand=True
        )

        # ----------------------------------------------------
        # TITLE
        # ----------------------------------------------------

        ttk.Label(
            main,
            text="FILE COLLECTOR",
            style="Title.TLabel"
        ).pack(
            anchor="w"
        )

        ttk.Label(
            main,
            text=(
                "Recursively collect files from "
                "multiple folders into one directory."
            )
        ).pack(
            anchor="w",
            pady=(0, 15)
        )

        # ----------------------------------------------------
        # SOURCE FOLDERS
        # ----------------------------------------------------

        ttk.Label(
            main,
            text="SOURCE FOLDERS",
            style="Section.TLabel"
        ).pack(
            anchor="w"
        )

        source_frame = ttk.Frame(
            main
        )

        source_frame.pack(
            fill="both",
            expand=False,
            pady=(5, 10)
        )

        self.source_list = tk.Listbox(
            source_frame,
            height=7,
            selectmode=tk.EXTENDED
        )

        self.source_list.pack(
            side="left",
            fill="both",
            expand=True
        )

        source_scroll = ttk.Scrollbar(
            source_frame,
            orient="vertical",
            command=self.source_list.yview
        )

        source_scroll.pack(
            side="right",
            fill="y"
        )

        self.source_list.config(
            yscrollcommand=source_scroll.set
        )

        source_buttons = ttk.Frame(
            main
        )

        source_buttons.pack(
            fill="x",
            pady=(0, 15)
        )

        ttk.Button(
            source_buttons,
            text="+ Add Folder",
            command=self.add_folder
        ).pack(
            side="left",
            padx=(0, 5)
        )

        ttk.Button(
            source_buttons,
            text="Remove Selected",
            command=self.remove_selected
        ).pack(
            side="left",
            padx=5
        )

        ttk.Button(
            source_buttons,
            text="Clear",
            command=self.clear_sources
        ).pack(
            side="left",
            padx=5
        )

        # ----------------------------------------------------
        # DESTINATION
        # ----------------------------------------------------

        ttk.Label(
            main,
            text="DESTINATION",
            style="Section.TLabel"
        ).pack(
            anchor="w"
        )

        destination_frame = ttk.Frame(
            main
        )

        destination_frame.pack(
            fill="x",
            pady=(5, 15)
        )

        self.destination_var = (
            tk.StringVar()
        )

        ttk.Entry(
            destination_frame,
            textvariable=self.destination_var
        ).pack(
            side="left",
            fill="x",
            expand=True
        )

        ttk.Button(
            destination_frame,
            text="Select Destination",
            command=self.select_destination
        ).pack(
            side="left",
            padx=(8, 0)
        )

        # ----------------------------------------------------
        # FILTER PRESETS
        # ----------------------------------------------------

        ttk.Label(
            main,
            text="FILTER PRESETS",
            style="Section.TLabel"
        ).pack(
            anchor="w"
        )

        filter_frame = ttk.Frame(
            main
        )

        filter_frame.pack(
            fill="x",
            pady=(5, 10)
        )

        self.filter_vars = {}

        for name in FILTER_PRESETS:

            var = tk.BooleanVar(
                value=False
            )

            self.filter_vars[name] = var

            ttk.Checkbutton(
                filter_frame,
                text=name,
                variable=var
            ).pack(
                side="left",
                padx=(0, 15)
            )

        # ----------------------------------------------------
        # CUSTOM FILTERS
        # ----------------------------------------------------

        ttk.Label(
            main,
            text="CUSTOM .gitignore-STYLE PATTERNS",
            style="Section.TLabel"
        ).pack(
            anchor="w"
        )

        custom_frame = ttk.Frame(
            main
        )

        custom_frame.pack(
            fill="x",
            pady=(5, 10)
        )

        self.custom_patterns = tk.Text(
            custom_frame,
            height=5,
            wrap="none"
        )

        self.custom_patterns.pack(
            fill="x",
            expand=True
        )

        self.custom_patterns.insert(
            "1.0",
            "# One pattern per line\n"
            "# Examples:\n"
            "# *.log\n"
            "# cache/\n"
            "# *.secret\n"
        )

        # ----------------------------------------------------
        # ACTION BUTTONS
        # ----------------------------------------------------

        action_frame = ttk.Frame(
            main
        )

        action_frame.pack(
            fill="x",
            pady=(5, 10)
        )

        self.preview_button = ttk.Button(
            action_frame,
            text="PREVIEW",
            style="Action.TButton",
            command=self.start_preview
        )

        self.preview_button.pack(
            side="left",
            padx=(0, 8)
        )

        self.copy_button = ttk.Button(
            action_frame,
            text="COPY FILES",
            style="Action.TButton",
            command=self.start_copy
        )

        self.copy_button.pack(
            side="left",
            padx=8
        )

        self.cancel_button = ttk.Button(
            action_frame,
            text="CANCEL",
            command=self.cancel_operation,
            state="disabled"
        )

        self.cancel_button.pack(
            side="left",
            padx=8
        )

        # ----------------------------------------------------
        # PROGRESS
        # ----------------------------------------------------

        self.progress = ttk.Progressbar(
            main,
            mode="determinate"
        )

        self.progress.pack(
            fill="x",
            pady=(5, 5)
        )

        self.status_var = tk.StringVar(
            value="Ready."
        )

        ttk.Label(
            main,
            textvariable=self.status_var
        ).pack(
            anchor="w"
        )

        # ----------------------------------------------------
        # STATISTICS
        # ----------------------------------------------------

        self.stats_var = tk.StringVar(
            value=(
                "Files found: 0 | "
                "Copied: 0 | "
                "Errors: 0"
            )
        )

        ttk.Label(
            main,
            textvariable=self.stats_var
        ).pack(
            anchor="w",
            pady=(5, 5)
        )

        # ----------------------------------------------------
        # LOG
        # ----------------------------------------------------

        ttk.Label(
            main,
            text="LOG",
            style="Section.TLabel"
        ).pack(
            anchor="w"
        )

        log_frame = ttk.Frame(
            main
        )

        log_frame.pack(
            fill="both",
            expand=True,
            pady=(5, 0)
        )

        self.log_text = tk.Text(
            log_frame,
            height=10,
            wrap="none",
            state="disabled"
        )

        self.log_text.pack(
            side="left",
            fill="both",
            expand=True
        )

        log_scroll = ttk.Scrollbar(
            log_frame,
            orient="vertical",
            command=self.log_text.yview
        )

        log_scroll.pack(
            side="right",
            fill="y"
        )

        self.log_text.config(
            yscrollcommand=log_scroll.set
        )

    # ========================================================
    # SOURCE MANAGEMENT
    # ========================================================

    def add_folder(self):

        folder = filedialog.askdirectory(
            title="Select source folder"
        )

        if not folder:
            return

        folder = str(
            Path(folder).resolve()
        )

        if folder in self.sources:
            return

        self.sources.append(
            folder
        )

        self.source_list.insert(
            tk.END,
            folder
        )

        # A changed source list invalidates
        # the previous preview.
        self.scanned_files = []

        self.log(
            f"[SOURCE ADDED] {folder}"
        )

    def remove_selected(self):

        selected = list(
            self.source_list.curselection()
        )

        for index in reversed(
            selected
        ):

            folder = self.sources[
                index
            ]

            self.sources.pop(
                index
            )

            self.source_list.delete(
                index
            )

            self.log(
                f"[SOURCE REMOVED] {folder}"
            )

        # Previous preview is no longer valid.
        self.scanned_files = []

    def clear_sources(self):

        self.sources.clear()

        self.source_list.delete(
            0,
            tk.END
        )

        self.scanned_files = []

        self.log(
            "[SOURCES CLEARED]"
        )

    # ========================================================
    # DESTINATION
    # ========================================================

    def select_destination(self):

        folder = filedialog.askdirectory(
            title="Select destination folder"
        )

        if not folder:
            return

        folder = str(
            Path(folder).resolve()
        )

        self.destination = Path(
            folder
        )

        self.destination_var.set(
            folder
        )

        # Previous preview is no longer valid
        # because the destination changed.
        self.scanned_files = []

        self.log(
            f"[DESTINATION] {folder}"
        )

    # ========================================================
    # FILTERS
    # ========================================================

    def get_active_patterns(self):

        patterns = []

        # Presets
        for name, var in (
            self.filter_vars.items()
        ):

            if var.get():

                patterns.extend(
                    FILTER_PRESETS[name]
                )

        # Custom patterns
        custom_text = (
            self.custom_patterns.get(
                "1.0",
                tk.END
            )
        )

        for line in custom_text.splitlines():

            line = line.strip()

            if (
                line
                and not line.startswith("#")
            ):

                patterns.append(
                    line
                )

        return patterns

    # ========================================================
    # VALIDATION
    # ========================================================

    def validate(self):

        if not self.sources:

            messagebox.showwarning(
                "Missing source",
                "Add at least one source folder."
            )

            return False

        if not self.destination:

            messagebox.showwarning(
                "Missing destination",
                "Select a destination folder."
            )

            return False

        destination = (
            self.destination.resolve()
        )

        for source_string in self.sources:

            source = Path(
                source_string
            ).resolve()

            if not source.exists():

                messagebox.showerror(
                    "Invalid source",
                    f"Source does not exist:\n\n{source}"
                )

                return False

            if not source.is_dir():

                messagebox.showerror(
                    "Invalid source",
                    f"Not a directory:\n\n{source}"
                )

                return False

            # Source cannot equal destination
            if source == destination:

                messagebox.showerror(
                    "Invalid folders",
                    (
                        "A source folder cannot "
                        "be the destination folder."
                    )
                )

                return False

            # Destination cannot be inside source
            try:

                destination.relative_to(
                    source
                )

                messagebox.showerror(
                    "Invalid folders",
                    (
                        "The destination cannot be "
                        "inside a source folder.\n\n"
                        f"Source:\n{source}\n\n"
                        f"Destination:\n{destination}"
                    )
                )

                return False

            except ValueError:
                pass

        return True

    # ========================================================
    # OPERATION PREPARATION
    # ========================================================

    def prepare_operation(self):

        self.cancel_event.clear()

        self.preview_button.config(
            state="disabled"
        )

        self.copy_button.config(
            state="disabled"
        )

        self.cancel_button.config(
            state="normal"
        )

        self.progress["value"] = 0

        # IMPORTANT:
        #
        # Do NOT clear self.scanned_files here.
        #
        # Preview -> Copy depends on this list.
        #

    # ========================================================
    # PREVIEW
    # ========================================================

    def start_preview(self):

        if not self.validate():
            return

        # A new preview invalidates the old preview.
        self.scanned_files = []

        self.prepare_operation()

        self.worker_thread = threading.Thread(
            target=self.preview_worker,
            daemon=True
        )

        self.worker_thread.start()

    def preview_worker(self):

        patterns = (
            self.get_active_patterns()
        )

        self.filter_engine.set_patterns(
            patterns
        )

        self.queue_message(
            "log",
            "[SCAN] Starting scan..."
        )

        self.queue_message(
            "status",
            "Scanning..."
        )

        scanner = FileScanner(
            [
                Path(source)
                for source in self.sources
            ],
            self.destination,
            self.filter_engine
        )

        files = scanner.scan(
            progress_callback=(
                lambda count, file:
                self.queue_message(
                    "scan_progress",
                    count,
                    file
                )
            ),
            cancel_event=self.cancel_event
        )

        self.scanned_files = files

        if self.cancel_event.is_set():

            self.queue_message(
                "finished"
            )

            return

        total_size = sum(
            item["size"]
            for item in files
        )

        self.queue_message(
            "preview_done",
            len(files),
            total_size
        )

    # ========================================================
    # COPY
    # ========================================================

    def start_copy(self):

        if not self.validate():
            return

        # ----------------------------------------------------
        # CASE 1:
        # We already have a valid preview.
        # ----------------------------------------------------

        if self.scanned_files:

            answer = messagebox.askyesno(
                "Start copy?",
                (
                    f"Copy {len(self.scanned_files):,} files "
                    f"into:\n\n"
                    f"{self.destination}\n\n"
                    "Continue?"
                )
            )

            if not answer:
                return

            self.prepare_operation()

            self.worker_thread = threading.Thread(
                target=self.copy_worker,
                daemon=True
            )

            self.worker_thread.start()

            return

        # ----------------------------------------------------
        # CASE 2:
        # No preview exists.
        # Scan first.
        # ----------------------------------------------------

        self.prepare_operation()

        self.worker_thread = threading.Thread(
            target=self.copy_scan_worker,
            daemon=True
        )

        self.worker_thread.start()

    def copy_scan_worker(self):

        patterns = (
            self.get_active_patterns()
        )

        self.filter_engine.set_patterns(
            patterns
        )

        self.queue_message(
            "status",
            "Scanning before copy..."
        )

        self.queue_message(
            "log",
            "[SCAN] Starting scan..."
        )

        scanner = FileScanner(
            [
                Path(source)
                for source in self.sources
            ],
            self.destination,
            self.filter_engine
        )

        files = scanner.scan(
            progress_callback=(
                lambda count, file:
                self.queue_message(
                    "scan_progress",
                    count,
                    file
                )
            ),
            cancel_event=self.cancel_event
        )

        self.scanned_files = files

        if self.cancel_event.is_set():

            self.queue_message(
                "finished"
            )

            return

        total_size = sum(
            item["size"]
            for item in files
        )

        self.queue_message(
            "request_copy_confirmation",
            len(files),
            total_size
        )

    def copy_worker(self):

        self.queue_message(
            "status",
            "Copying files..."
        )

        self.queue_message(
            "log",
            "[COPY] Starting..."
        )

        engine = CopyEngine(
            self.destination
        )

        result = engine.copy_files(
            self.scanned_files,
            progress_callback=(
                lambda current,
                total,
                copied,
                failed,
                file:

                self.queue_message(
                    "copy_progress",
                    current,
                    total,
                    copied,
                    failed,
                    file
                )
            ),
            log_callback=(
                lambda text:
                self.queue_message(
                    "log",
                    text
                )
            ),
            cancel_event=self.cancel_event
        )

        self.queue_message(
            "copy_done",
            result
        )

    # ========================================================
    # CANCEL
    # ========================================================

    def cancel_operation(self):

        self.cancel_event.set()

        self.status_var.set(
            "Cancelling..."
        )

        self.log(
            "[CANCEL] Cancellation requested."
        )

        self.cancel_button.config(
            state="disabled"
        )

    # ========================================================
    # MESSAGE QUEUE
    # ========================================================

    def queue_message(
        self,
        message_type,
        *data
    ):

        self.ui_queue.put(
            (
                message_type,
                data
            )
        )

    def process_queue(self):

        try:

            while True:

                message_type, data = (
                    self.ui_queue.get_nowait()
                )

                self.handle_message(
                    message_type,
                    data
                )

        except queue.Empty:
            pass

        self.root.after(
            50,
            self.process_queue
        )

    # ========================================================
    # MESSAGE HANDLER
    # ========================================================

    def handle_message(
        self,
        message_type,
        data
    ):

        # ----------------------------------------------------
        # LOG
        # ----------------------------------------------------

        if message_type == "log":

            self.log(
                data[0]
            )

        # ----------------------------------------------------
        # STATUS
        # ----------------------------------------------------

        elif message_type == "status":

            self.status_var.set(
                data[0]
            )

        # ----------------------------------------------------
        # SCAN PROGRESS
        # ----------------------------------------------------

        elif message_type == "scan_progress":

            count, file = data

            self.status_var.set(
                f"Scanning: {count:,} files found"
            )

            self.stats_var.set(
                f"Files found: {count:,}"
            )

        # ----------------------------------------------------
        # PREVIEW COMPLETE
        # ----------------------------------------------------

        elif message_type == "preview_done":

            count, total_size = data

            self.progress["value"] = 100

            size_text = (
                self.format_size(
                    total_size
                )
            )

            self.status_var.set(
                "Preview complete."
            )

            self.stats_var.set(
                (
                    f"Files to copy: {count:,} | "
                    f"Total size: {size_text}"
                )
            )

            self.log(
                f"[PREVIEW] {count:,} files"
            )

            self.log(
                f"[PREVIEW] Total size: {size_text}"
            )

            self.log(
                "[PREVIEW] Ready to copy."
            )

            self.finish_operation()

        # ----------------------------------------------------
        # SCAN -> COPY CONFIRMATION
        # ----------------------------------------------------

        elif (
            message_type
            == "request_copy_confirmation"
        ):

            count, total_size = data

            size_text = (
                self.format_size(
                    total_size
                )
            )

            answer = messagebox.askyesno(
                "Start copy?",
                (
                    f"{count:,} files found.\n"
                    f"Total size: {size_text}\n\n"
                    f"Destination:\n"
                    f"{self.destination}\n\n"
                    "Start copying?"
                )
            )

            if answer:

                self.prepare_operation()

                self.worker_thread = (
                    threading.Thread(
                        target=self.copy_worker,
                        daemon=True
                    )
                )

                self.worker_thread.start()

            else:

                self.finish_operation()

        # ----------------------------------------------------
        # COPY PROGRESS
        # ----------------------------------------------------

        elif message_type == "copy_progress":

            (
                current,
                total,
                copied,
                failed,
                file
            ) = data

            percentage = (
                current / total * 100
                if total
                else 0
            )

            self.progress["value"] = (
                percentage
            )

            self.status_var.set(
                (
                    f"Copying "
                    f"{current:,} / {total:,}"
                )
            )

            self.stats_var.set(
                (
                    f"Copied: {copied:,} | "
                    f"Errors: {failed:,}"
                )
            )

        # ----------------------------------------------------
        # COPY COMPLETE
        # ----------------------------------------------------

        elif message_type == "copy_done":

            result = data[0]

            self.progress["value"] = 100

            self.status_var.set(
                "Copy completed."
            )

            self.stats_var.set(
                (
                    f"Copied: "
                    f"{result['copied']:,} | "
                    f"Errors: "
                    f"{result['failed']:,} | "
                    f"Size: "
                    f"{self.format_size(result['bytes'])}"
                )
            )

            self.log(
                "=" * 60
            )

            self.log(
                "[DONE]"
            )

            self.log(
                f"Copied: {result['copied']:,}"
            )

            self.log(
                f"Errors: {result['failed']:,}"
            )

            self.log(
                "Total: "
                + self.format_size(
                    result["bytes"]
                )
            )

            self.log(
                "=" * 60
            )

            self.finish_operation()

            messagebox.showinfo(
                "Completed",
                (
                    "Copy operation completed.\n\n"
                    f"Files copied: "
                    f"{result['copied']:,}\n"
                    f"Errors: "
                    f"{result['failed']:,}\n"
                    f"Data copied: "
                    f"{self.format_size(result['bytes'])}"
                )
            )

        # ----------------------------------------------------
        # CANCELLED
        # ----------------------------------------------------

        elif message_type == "finished":

            self.status_var.set(
                "Operation cancelled."
            )

            self.finish_operation()

    # ========================================================
    # FINISH OPERATION
    # ========================================================

    def finish_operation(self):

        self.preview_button.config(
            state="normal"
        )

        self.copy_button.config(
            state="normal"
        )

        self.cancel_button.config(
            state="disabled"
        )

    # ========================================================
    # LOG
    # ========================================================

    def log(self, text):

        self.log_text.config(
            state="normal"
        )

        self.log_text.insert(
            tk.END,
            text + "\n"
        )

        self.log_text.see(
            tk.END
        )

        self.log_text.config(
            state="disabled"
        )

    # ========================================================
    # FORMAT FILE SIZE
    # ========================================================

    @staticmethod
    def format_size(size):

        units = [
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        ]

        value = float(size)

        for unit in units:

            if value < 1024:

                return (
                    f"{value:.2f} {unit}"
                )

            value /= 1024

        return (
            f"{value:.2f} PB"
        )


# ============================================================
# APPLICATION ENTRY POINT
# ============================================================

def main():

    root = tk.Tk()

    app = FileCollectorApp(
        root
    )

    root.mainloop()


if __name__ == "__main__":
    main()
