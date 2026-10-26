import fnmatch
import os
import queue
import shutil
import threading
from pathlib import Path

import tkinter as tk
from tkinter import ttk, filedialog, messagebox


# ============================================================
# FILE COLLECTOR V1 (SINGLE-FILE MERGE)
# ============================================================

APP_TITLE = "File Collector V1 - Single File Aggregator"


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
            if not pattern or pattern.startswith("#"):
                continue
            self.patterns.append(pattern.replace("\\", "/"))

    def should_ignore(self, file_path, root_path):
        try:
            relative = file_path.relative_to(root_path)
        except ValueError:
            return False

        relative_path = str(relative).replace("\\", "/")
        parts = relative_path.split("/")
        filename = parts[-1]

        for pattern in self.patterns:
            directory_rule = pattern.endswith("/")

            if directory_rule:
                pattern = pattern.rstrip("/")
                for part in parts[:-1]:
                    if fnmatch.fnmatch(part, pattern):
                        return True
                if fnmatch.fnmatch(relative_path, pattern):
                    return True
                continue

            if fnmatch.fnmatch(relative_path, pattern):
                return True
            if fnmatch.fnmatch(filename, pattern):
                return True
            for part in parts:
                if fnmatch.fnmatch(part, pattern):
                    return True

        return False

    def should_ignore_directory(self, directory_path, root_path):
        try:
            relative = directory_path.relative_to(root_path)
        except ValueError:
            return False

        relative_path = str(relative).replace("\\", "/")
        directory_name = directory_path.name

        for pattern in self.patterns:
            if not pattern.endswith("/"):
                continue

            pattern = pattern.rstrip("/")
            if fnmatch.fnmatch(directory_name, pattern):
                return True
            if fnmatch.fnmatch(relative_path, pattern):
                return True

        return False


# ============================================================
# FILE SCANNER
# ============================================================

class FileScanner:

    def __init__(self, sources, output_file, filter_engine):
        self.sources = sources
        self.output_file = output_file
        self.filter_engine = filter_engine

    def scan(self, progress_callback=None, cancel_event=None):
        files = []
        seen_files = set()
        
        output_resolved = self.output_file.resolve() if self.output_file else None

        for source in self.sources:
            source = source.resolve()
            if not source.exists() or not source.is_dir():
                continue

            for root, dirs, filenames in os.walk(source):
                if cancel_event and cancel_event.is_set():
                    return files

                root_path = Path(root)

                # Remove ignored directories
                filtered_dirs = []
                for directory in dirs:
                    directory_path = root_path / directory
                    if self.filter_engine.should_ignore_directory(directory_path, source):
                        continue
                    filtered_dirs.append(directory)

                dirs[:] = filtered_dirs

                # Process files
                for filename in filenames:
                    if cancel_event and cancel_event.is_set():
                        return files

                    file_path = root_path / filename

                    try:
                        resolved = file_path.resolve()
                    except OSError:
                        continue

                    # Avoid reading target output file if it's placed inside a source folder
                    if output_resolved and resolved == output_resolved:
                        continue

                    key = str(resolved).lower()
                    if key in seen_files:
                        continue
                    seen_files.add(key)

                    if self.filter_engine.should_ignore(file_path, source):
                        continue

                    try:
                        if not file_path.is_file():
                            continue
                        size = file_path.stat().st_size
                    except OSError:
                        continue

                    files.append({"source": file_path, "size": size})

                    if progress_callback:
                        progress_callback(len(files), file_path)

        return files


# ============================================================
# SINGLE FILE MERGE ENGINE
# ============================================================

class MergeEngine:

    def __init__(self, output_file):
        self.output_file = output_file

    def combine_files(self, files, progress_callback=None, log_callback=None, cancel_event=None):
        self.output_file.parent.mkdir(parents=True, exist_ok=True)
        merged_count = 0
        failed_count = 0
        total_bytes = 0

        # Open target single file in write mode with UTF-8 encoding
        with open(self.output_file, "w", encoding="utf-8", errors="replace") as out_file:
            for index, item in enumerate(files, start=1):
                if cancel_event and cancel_event.is_set():
                    break

                source = item["source"]

                try:
                    # Write header block per file for context clarity
                    out_file.write(f"\n{'='*80}\n")
                    out_file.write(f"FILE: {source}\n")
                    out_file.write(f"{'='*80}\n\n")

                    with open(source, "r", encoding="utf-8", errors="replace") as in_file:
                        shutil.copyfileobj(in_file, out_file)

                    out_file.write("\n")  # Trailing newline separator

                    merged_count += 1
                    total_bytes += item["size"]

                    if log_callback:
                        log_callback(f"[MERGED] {source}")

                except Exception as error:
                    failed_count += 1
                    if log_callback:
                        log_callback(f"[ERROR] Could not read {source}: {error}")

                if progress_callback:
                    progress_callback(index, len(files), merged_count, failed_count, source)

        return {"merged": merged_count, "failed": failed_count, "bytes": total_bytes}


# ============================================================
# MAIN APPLICATION
# ============================================================

class FileCollectorApp:

    def __init__(self, root):
        self.root = root
        self.root.title(APP_TITLE)
        self.root.geometry("1000x760")
        self.root.minsize(850, 650)

        self.sources = []
        self.output_file = None
        self.scanned_files = []

        self.filter_engine = FilterEngine()
        self.worker_thread = None
        self.cancel_event = threading.Event()
        self.ui_queue = queue.Queue()

        self.create_style()
        self.create_ui()
        self.process_queue()

    def create_style(self):
        style = ttk.Style()
        try:
            style.theme_use("vista")
        except tk.TclError:
            pass

        style.configure("Title.TLabel", font=("Segoe UI", 18, "bold"))
        style.configure("Section.TLabel", font=("Segoe UI", 11, "bold"))
        style.configure("Action.TButton", font=("Segoe UI", 10, "bold"))

    def create_ui(self):
        main = ttk.Frame(self.root, padding=15)
        main.pack(fill="both", expand=True)

        # Title
        ttk.Label(main, text="FILE AGGREGATOR", style="Title.TLabel").pack(anchor="w")
        ttk.Label(
            main,
            text="Recursively gather and combine all file contents into one single text file.",
        ).pack(anchor="w", pady=(0, 15))

        # Sources
        ttk.Label(main, text="SOURCE FOLDERS", style="Section.TLabel").pack(anchor="w")
        source_frame = ttk.Frame(main)
        source_frame.pack(fill="both", expand=False, pady=(5, 10))

        self.source_list = tk.Listbox(source_frame, height=6, selectmode=tk.EXTENDED)
        self.source_list.pack(side="left", fill="both", expand=True)

        source_scroll = ttk.Scrollbar(
            source_frame, orient="vertical", command=self.source_list.yview
        )
        source_scroll.pack(side="right", fill="y")
        self.source_list.config(yscrollcommand=source_scroll.set)

        source_buttons = ttk.Frame(main)
        source_buttons.pack(fill="x", pady=(0, 15))

        ttk.Button(source_buttons, text="+ Add Folder", command=self.add_folder).pack(
            side="left", padx=(0, 5)
        )
        ttk.Button(
            source_buttons, text="Remove Selected", command=self.remove_selected
        ).pack(side="left", padx=5)
        ttk.Button(source_buttons, text="Clear", command=self.clear_sources).pack(
            side="left", padx=5
        )

        # Output Target File
        ttk.Label(main, text="TARGET SINGLE FILE", style="Section.TLabel").pack(anchor="w")
        output_frame = ttk.Frame(main)
        output_frame.pack(fill="x", pady=(5, 15))

        self.output_var = tk.StringVar()
        ttk.Entry(output_frame, textvariable=self.output_var).pack(
            side="left", fill="x", expand=True
        )
        ttk.Button(
            output_frame,
            text="Select Output File",
            command=self.select_output_file,
        ).pack(side="left", padx=(8, 0))

        # Filter Presets
        ttk.Label(main, text="FILTER PRESETS", style="Section.TLabel").pack(anchor="w")
        filter_frame = ttk.Frame(main)
        filter_frame.pack(fill="x", pady=(5, 10))

        self.filter_vars = {}
        for name in FILTER_PRESETS:
            var = tk.BooleanVar(value=False)
            self.filter_vars[name] = var
            ttk.Checkbutton(filter_frame, text=name, variable=var).pack(
                side="left", padx=(0, 15)
            )

        # Custom Filters
        ttk.Label(
            main, text="CUSTOM .gitignore-STYLE PATTERNS", style="Section.TLabel"
        ).pack(anchor="w")
        custom_frame = ttk.Frame(main)
        custom_frame.pack(fill="x", pady=(5, 10))

        self.custom_patterns = tk.Text(custom_frame, height=4, wrap="none")
        self.custom_patterns.pack(fill="x", expand=True)
        self.custom_patterns.insert(
            "1.0",
            "# One pattern per line\n# Examples:\n# *.log\n# cache/\n",
        )

        # Actions
        action_frame = ttk.Frame(main)
        action_frame.pack(fill="x", pady=(5, 10))

        self.preview_button = ttk.Button(
            action_frame,
            text="PREVIEW",
            style="Action.TButton",
            command=self.start_preview,
        )
        self.preview_button.pack(side="left", padx=(0, 8))

        self.merge_button = ttk.Button(
            action_frame,
            text="COMBINE TO SINGLE FILE",
            style="Action.TButton",
            command=self.start_merge,
        )
        self.merge_button.pack(side="left", padx=8)

        self.cancel_button = ttk.Button(
            action_frame, text="CANCEL", command=self.cancel_operation, state="disabled"
        )
        self.cancel_button.pack(side="left", padx=8)

        # Progress
        self.progress = ttk.Progressbar(main, mode="determinate")
        self.progress.pack(fill="x", pady=(5, 5))

        self.status_var = tk.StringVar(value="Ready.")
        ttk.Label(main, textvariable=self.status_var).pack(anchor="w")

        # Stats
        self.stats_var = tk.StringVar(
            value="Files found: 0 | Merged: 0 | Errors: 0"
        )
        ttk.Label(main, textvariable=self.stats_var).pack(anchor="w", pady=(5, 5))

        # Log
        ttk.Label(main, text="LOG", style="Section.TLabel").pack(anchor="w")
        log_frame = ttk.Frame(main)
        log_frame.pack(fill="both", expand=True, pady=(5, 0))

        self.log_text = tk.Text(log_frame, height=8, wrap="none", state="disabled")
        self.log_text.pack(side="left", fill="both", expand=True)

        log_scroll = ttk.Scrollbar(
            log_frame, orient="vertical", command=self.log_text.yview
        )
        log_scroll.pack(side="right", fill="y")
        self.log_text.config(yscrollcommand=log_scroll.set)

    def add_folder(self):
        folder = filedialog.askdirectory(title="Select source folder")
        if not folder:
            return
        folder = str(Path(folder).resolve())
        if folder in self.sources:
            return
        self.sources.append(folder)
        self.source_list.insert(tk.END, folder)
        self.scanned_files = []
        self.log(f"[SOURCE ADDED] {folder}")

    def remove_selected(self):
        selected = list(self.source_list.curselection())
        for index in reversed(selected):
            folder = self.sources[index]
            self.sources.pop(index)
            self.source_list.delete(index)
            self.log(f"[SOURCE REMOVED] {folder}")
        self.scanned_files = []

    def clear_sources(self):
        self.sources.clear()
        self.source_list.delete(0, tk.END)
        self.scanned_files = []
        self.log("[SOURCES CLEARED]")

    def select_output_file(self):
        file_path = filedialog.asksaveasfilename(
            title="Select target combined file",
            defaultextension=".txt",
            filetypes=[("Text Files", "*.txt"), ("All Files", "*.*")],
        )
        if not file_path:
            return
        file_path = str(Path(file_path).resolve())
        self.output_file = Path(file_path)
        self.output_var.set(file_path)
        self.log(f"[OUTPUT FILE] {file_path}")

    def get_active_patterns(self):
        patterns = []
        for name, var in self.filter_vars.items():
            if var.get():
                patterns.extend(FILTER_PRESETS[name])

        custom_text = self.custom_patterns.get("1.0", tk.END)
        for line in custom_text.splitlines():
            line = line.strip()
            if line and not line.startswith("#"):
                patterns.append(line)
        return patterns

    def validate(self):
        if not self.sources:
            messagebox.showwarning("Missing source", "Add at least one source folder.")
            return False

        if not self.output_file:
            messagebox.showwarning(
                "Missing output file", "Select a target single output file."
            )
            return False

        for source_string in self.sources:
            source = Path(source_string).resolve()

            if not source.exists():
                messagebox.showerror(
                    "Invalid source", f"Source does not exist:\n\n{source}"
                )
                return False

            if not source.is_dir():
                messagebox.showerror(
                    "Invalid source", f"Not a directory:\n\n{source}"
                )
                return False

        return True

    def prepare_operation(self):
        self.cancel_event.clear()
        self.preview_button.config(state="disabled")
        self.merge_button.config(state="disabled")
        self.cancel_button.config(state="normal")
        self.progress["value"] = 0

    def cancel_operation(self):
        self.cancel_event.set()
        self.status_var.set("Cancelling...")
        self.log("[USER] Requested cancellation.")

    def log(self, message):
        self.log_text.config(state="normal")
        self.log_text.insert(tk.END, message + "\n")
        self.log_text.see(tk.END)
        self.log_text.config(state="disabled")

    def queue_message(self, msg_type, *args):
        self.ui_queue.put((msg_type, args))

    def process_queue(self):
        try:
            while True:
                msg_type, args = self.ui_queue.get_nowait()

                if msg_type == "log":
                    self.log(args[0])
                elif msg_type == "status":
                    self.status_var.set(args[0])
                elif msg_type == "scan_progress":
                    count, current_file = args
                    self.status_var.set(f"Scanning... Found {count} files")
                    self.stats_var.set(
                        f"Files found: {count} | Merged: 0 | Errors: 0"
                    )
                elif msg_type == "preview_done":
                    count, total_bytes = args
                    mb = total_bytes / (1024 * 1024)
                    self.status_var.set("Preview complete.")
                    self.stats_var.set(
                        f"Files found: {count} ({mb:.2f} MB) | Merged: 0 | Errors: 0"
                    )
                    self.log(
                        f"[SCAN COMPLETE] Found {count} files ({mb:.2f} MB)."
                    )
                    self.reset_ui_buttons()
                elif msg_type == "merge_progress":
                    index, total, merged, failed, current_file = args
                    pct = (index / total) * 100 if total > 0 else 0
                    self.progress["value"] = pct
                    self.status_var.set(f"Merging {index}/{total}: {current_file.name}")
                    self.stats_var.set(
                        f"Files found: {total} | Merged: {merged} | Errors: {failed}"
                    )
                elif msg_type == "merge_done":
                    results = args[0]
                    merged = results["merged"]
                    failed = results["failed"]
                    mb = results["bytes"] / (1024 * 1024)
                    self.status_var.set("File combination complete.")
                    self.stats_var.set(
                        f"Files found: {merged + failed} | Combined: {merged} ({mb:.2f} MB) | Errors: {failed}"
                    )
                    self.log(
                        f"[COMBINE COMPLETE] Merged: {merged}, Failed: {failed}, Processed Size: {mb:.2f} MB"
                    )
                    self.reset_ui_buttons()
                elif msg_type == "finished":
                    self.status_var.set("Operation cancelled.")
                    self.log("[CANCELLED] Operation aborted.")
                    self.reset_ui_buttons()

        except queue.Empty:
            pass

        self.root.after(100, self.process_queue)

    def reset_ui_buttons(self):
        self.preview_button.config(state="normal")
        self.merge_button.config(state="normal")
        self.cancel_button.config(state="disabled")

    def start_preview(self):
        if not self.validate():
            return
        self.scanned_files = []
        self.prepare_operation()
        self.worker_thread = threading.Thread(
            target=self.preview_worker, daemon=True
        )
        self.worker_thread.start()

    def preview_worker(self):
        patterns = self.get_active_patterns()
        self.filter_engine.set_patterns(patterns)

        self.queue_message("log", "[SCAN] Starting scan...")
        self.queue_message("status", "Scanning...")

        scanner = FileScanner(
            [Path(source) for source in self.sources],
            self.output_file,
            self.filter_engine,
        )

        files = scanner.scan(
            progress_callback=lambda count, file: self.queue_message(
                "scan_progress", count, file
            ),
            cancel_event=self.cancel_event,
        )

        self.scanned_files = files

        if self.cancel_event.is_set():
            self.queue_message("finished")
            return

        total_size = sum(item["size"] for item in files)
        self.queue_message("preview_done", len(files), total_size)

    def start_merge(self):
        if not self.validate():
            return

        if self.scanned_files:
            answer = messagebox.askyesno(
                "Start Merge?",
                f"Combine content from {len(self.scanned_files):,} files into:\n\n{self.output_file}\n\nContinue?",
            )
            if not answer:
                return

            self.prepare_operation()
            self.worker_thread = threading.Thread(
                target=self.merge_worker, daemon=True
            )
            self.worker_thread.start()
            return

        self.prepare_operation()
        self.worker_thread = threading.Thread(
            target=self.merge_scan_worker, daemon=True
        )
        self.worker_thread.start()

    def merge_scan_worker(self):
        patterns = self.get_active_patterns()
        self.filter_engine.set_patterns(patterns)

        self.queue_message("status", "Scanning before merge...")
        self.queue_message("log", "[SCAN] Starting scan...")

        scanner = FileScanner(
            [Path(source) for source in self.sources],
            self.output_file,
            self.filter_engine,
        )

        files = scanner.scan(
            progress_callback=lambda count, file: self.queue_message(
                "scan_progress", count, file
            ),
            cancel_event=self.cancel_event,
        )

        self.scanned_files = files

        if self.cancel_event.is_set():
            self.queue_message("finished")
            return

        self.merge_worker()

    def merge_worker(self):
        self.queue_message("log", "[MERGE] Combining content into target file...")
        self.queue_message("status", "Merging files...")

        engine = MergeEngine(self.output_file)
        results = engine.combine_files(
            self.scanned_files,
            progress_callback=lambda idx, total, mer, fail, file: self.queue_message(
                "merge_progress", idx, total, mer, fail, file
            ),
            log_callback=lambda msg: self.queue_message("log", msg),
            cancel_event=self.cancel_event,
        )

        if self.cancel_event.is_set():
            self.queue_message("finished")
            return

        self.queue_message("merge_done", results)


# ============================================================
# ENTRY POINT
# ============================================================

if __name__ == "__main__":
    root = tk.Tk()
    app = FileCollectorApp(root)
    root.mainloop()