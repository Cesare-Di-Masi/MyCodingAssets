#!/usr/bin/env python3
"""
Circolari Bot — controlla le circolari scolastiche di ispascalcomandini.it
e invia le nuove su Telegram.

Setup:
    pip install requests beautifulsoup4 python-telegram-bot python-dotenv

    Crea un file .env nella stessa cartella:
        BOT_TOKEN=123456:ABC-your-token
        CHAT_ID=123456789,987654321          # più chat ID separati da virgola

        # opzionali, per gli alert via email se il bot smette di funzionare:
        SMTP_HOST=smtp.gmail.com
        SMTP_PORT=587
        SMTP_USER=tuo@gmail.com
        SMTP_PASS=app-password
        ALERT_EMAIL_TO=tuo@gmail.com

Modalità ONE-SHOT (compatibile con Task Scheduler / cron, come prima):
    python circolari_bot.py                 # check normale (solo pagina 1, si ferma agli ID già visti)
    python circolari_bot.py --max-pages 3    # forza a controllare fino a 3 pagine
    python circolari_bot.py --dry-run        # scarica e mostra cosa manderebbe, senza inviare nulla
    python circolari_bot.py --first-run      # bootstrap iniziale: scarica tutto lo storico e lo marca come "già inviato"
    python circolari_bot.py --tipologia Famiglie          # invia solo circolari di una certa tipologia
    python circolari_bot.py --include-keywords "sciopero,gita"   # invia solo se il titolo/descrizione contiene una di queste parole
    python circolari_bot.py --exclude-keywords "sport"           # NON invia se contiene una di queste parole
    python circolari_bot.py --keep-pdfs 3    # tiene in locale gli ultimi 3 PDF invece di 1
    python circolari_bot.py --no-download    # non salva nessun PDF in locale

Modalità SERVE (processo persistente, sostituisce Task Scheduler/cron):
    python circolari_bot.py --serve --interval 15
        Fa il check periodico da solo ogni --interval minuti E risponde al
        comando /ultime nella chat Telegram (es. "/ultime 5" per le ultime 5).
        Va lasciato girare sempre attivo (es. come servizio).

PDF locali:
    Ogni volta che arriva una circolare nuova, il PDF (o i PDF) allegato
    viene scaricato in downloads/. Per default viene tenuto solo l'ultimo
    scaricato: appena ne arriva uno nuovo, i precedenti vengono cancellati
    in automatico (--keep-pdfs N per tenerne di più).

Alert email:
    Se per 3 esecuzioni di fila il bot non riesce proprio a raggiungere il
    sito della scuola, e sono configurate le variabili SMTP_*, manda una
    email di allerta (una sola volta, non ad ogni esecuzione fallita).

Cron/systemd (Linux):
    */15 * * * * cd /path/to/bot && /usr/bin/python3 circolari_bot.py >> bot.log 2>&1

Task Scheduler (Windows):
    Usa run_bot.bat con l'Utilità di pianificazione (modalità one-shot),
    oppure lancia `circolari_bot.py --serve` una volta all'avvio se vuoi
    anche /ultime.
"""

from __future__ import annotations

import argparse
import asyncio
import hashlib
import html
import json
import logging
import os
import re
import smtplib
import time
from dataclasses import dataclass, field
from email.mime.text import MIMEText
from logging.handlers import RotatingFileHandler
from pathlib import Path
from typing import Optional
from urllib.parse import urljoin

import requests
from bs4 import BeautifulSoup

try:
    from dotenv import load_dotenv
    load_dotenv()
except ImportError:
    pass  # .env support is optional; env vars can be set another way

from telegram import Bot, Update
from telegram.error import RetryAfter, TimedOut, NetworkError
from telegram.ext import Application, CommandHandler, ContextTypes

# --- CONFIGURATION ---
BOT_TOKEN = os.environ.get("BOT_TOKEN", "")
CHAT_IDS = [c.strip() for c in os.environ.get("CHAT_ID", "").split(",") if c.strip()]

SMTP_HOST = os.environ.get("SMTP_HOST", "")
SMTP_PORT = int(os.environ.get("SMTP_PORT", "587") or 587)
SMTP_USER = os.environ.get("SMTP_USER", "")
SMTP_PASS = os.environ.get("SMTP_PASS", "")
ALERT_EMAIL_TO = os.environ.get("ALERT_EMAIL_TO", "")

SCHOOL_CIRCULARS_URL = "https://www.ispascalcomandini.it/comunicati"
BASE_DIR = Path(__file__).resolve().parent
SENT_ITEMS_FILE = BASE_DIR / "sent_circulars.json"
DOWNLOAD_DIR = BASE_DIR / "downloads"
LOG_FILE = BASE_DIR / "bot.log"
FAILURE_STATE_FILE = BASE_DIR / "failure_state.json"
FAILURE_ALERT_THRESHOLD = 3

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
        "(KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
    )
}

REQUEST_TIMEOUT = 15
MAX_HTTP_RETRIES = 3
TELEGRAM_MSG_LIMIT = 4096

# --- LOGGING (console + file con rotazione: max 2MB, tiene 5 file vecchi) ---
log = logging.getLogger("circolari_bot")
log.setLevel(logging.INFO)
_formatter = logging.Formatter("%(asctime)s [%(levelname)s] %(message)s", datefmt="%Y-%m-%d %H:%M:%S")

_console_handler = logging.StreamHandler()
_console_handler.setFormatter(_formatter)
log.addHandler(_console_handler)

_file_handler = RotatingFileHandler(LOG_FILE, maxBytes=2 * 1024 * 1024, backupCount=5, encoding="utf-8")
_file_handler.setFormatter(_formatter)
log.addHandler(_file_handler)


@dataclass
class Circular:
    id: str
    title: str
    description: str
    published: str
    tipologia: str
    attachments: list[tuple[str, str]] = field(default_factory=list)


# --- PERSISTENCE ---

def load_sent_items() -> set[str]:
    if SENT_ITEMS_FILE.exists():
        try:
            return set(json.loads(SENT_ITEMS_FILE.read_text()))
        except (json.JSONDecodeError, OSError) as e:
            log.warning("Impossibile leggere %s (%s), riparto da zero", SENT_ITEMS_FILE, e)
    return set()


def save_sent_items(sent_items: set[str]) -> None:
    # Scrittura atomica: prima su file temporaneo, poi rinomina.
    # Così se il processo muore a metà scrittura non si corrompe il file esistente.
    tmp = SENT_ITEMS_FILE.with_suffix(".tmp")
    tmp.write_text(json.dumps(sorted(sent_items)))
    tmp.replace(SENT_ITEMS_FILE)


# --- ALERTING (email, per quando anche Telegram potrebbe essere il problema) ---

def load_failure_count() -> int:
    if FAILURE_STATE_FILE.exists():
        try:
            return json.loads(FAILURE_STATE_FILE.read_text()).get("consecutive_failures", 0)
        except (json.JSONDecodeError, OSError):
            return 0
    return 0


def save_failure_count(n: int) -> None:
    FAILURE_STATE_FILE.write_text(json.dumps({"consecutive_failures": n}))


def send_alert_email(subject: str, body: str) -> None:
    if not (SMTP_HOST and SMTP_USER and SMTP_PASS and ALERT_EMAIL_TO):
        log.warning("Alert email non inviato (SMTP_* non configurato in .env): %s", subject)
        return
    try:
        msg = MIMEText(body)
        msg["Subject"] = subject
        msg["From"] = SMTP_USER
        msg["To"] = ALERT_EMAIL_TO
        with smtplib.SMTP(SMTP_HOST, SMTP_PORT, timeout=15) as server:
            server.starttls()
            server.login(SMTP_USER, SMTP_PASS)
            server.send_message(msg)
        log.info("Alert email inviato a %s", ALERT_EMAIL_TO)
    except Exception as e:
        log.error("Invio alert email fallito: %s", e)


def track_scrape_health(page1_reachable: bool) -> None:
    """Manda un'email solo dopo N esecuzioni consecutive in cui il sito non è raggiungibile."""
    failures = load_failure_count()
    if page1_reachable:
        if failures >= FAILURE_ALERT_THRESHOLD:
            send_alert_email(
                "Circolari Bot: tornato online",
                "Il sito della scuola è di nuovo raggiungibile, il bot ha ripreso a funzionare normalmente.",
            )
        save_failure_count(0)
        return

    failures += 1
    save_failure_count(failures)
    if failures == FAILURE_ALERT_THRESHOLD:
        send_alert_email(
            "Circolari Bot: problema persistente",
            f"Il bot non riesce a raggiungere {SCHOOL_CIRCULARS_URL} da {failures} esecuzioni consecutive. "
            f"Controlla il log in {LOG_FILE}.",
        )
    elif failures > FAILURE_ALERT_THRESHOLD:
        log.warning("Sito ancora irraggiungibile (fallimento #%d), alert già inviato.", failures)


# --- SCRAPING ---

def fetch_with_retry(url: str) -> Optional[requests.Response]:
    for attempt in range(1, MAX_HTTP_RETRIES + 1):
        try:
            resp = requests.get(url, headers=HEADERS, timeout=REQUEST_TIMEOUT)
            resp.raise_for_status()
            return resp
        except requests.RequestException as e:
            log.warning("Tentativo %d/%d fallito per %s: %s", attempt, MAX_HTTP_RETRIES, url, e)
            if attempt < MAX_HTTP_RETRIES:
                time.sleep(2 * attempt)  # backoff lineare
    log.error("Impossibile scaricare %s dopo %d tentativi", url, MAX_HTTP_RETRIES)
    return None


def get_last_page(soup: BeautifulSoup) -> int:
    pages = []
    for a in soup.select("nav.pagination-wrapper a.page-link"):
        href = a.get("href", "")
        if "page=" in href:
            try:
                pages.append(int(href.split("page=")[-1]))
            except ValueError:
                pass
    return max(pages) if pages else 1


def parse_item(item) -> Optional[Circular]:
    title_el = item.select_one("h3.media-heading")
    if not title_el:
        return None

    title = title_el.get_text(strip=True)
    desc_el = title_el.find_next_sibling("p")
    description = desc_el.get_text(strip=True) if desc_el else ""

    published = ""
    tipologia = ""
    for li in item.select("li.list-group-item"):
        text = li.get_text(" ", strip=True)
        strong = li.find("strong")
        if text.startswith("Pubblicato il:") and strong:
            published = strong.get_text(strip=True)
        elif text.startswith("Tipologia:") and strong:
            tipologia = strong.get_text(strip=True)

    attachments: list[tuple[str, str]] = []
    for box in item.select("div.box-allegato-new"):
        filename_el = box.select_one("p.box-allegato-titolo")
        filename = filename_el.get_text(strip=True) if filename_el else "Allegato"

        download_link = box.select_one('a[href*="download=1"]') or box.select_one('a[href*="/Documenti/"]')
        if download_link:
            href = urljoin(SCHOOL_CIRCULARS_URL, download_link["href"])
            if (filename, href) not in attachments:
                attachments.append((filename, href))

    if attachments:
        circular_id = attachments[0][1]
    else:
        text_for_id = f"{title}|{description}|{published}|{tipologia}"
        circular_id = hashlib.sha256(text_for_id.encode()).hexdigest()

    return Circular(
        id=circular_id,
        title=title,
        description=description,
        published=published,
        tipologia=tipologia,
        attachments=attachments,
    )


def scrape_circulars(
    sent_items: set[str],
    max_pages: Optional[int] = None,
    stop_at_known: bool = True,
) -> tuple[list[Circular], bool]:
    """
    Scarica le circolari, pagina per pagina.

    Se stop_at_known=True (comportamento di default per i check periodici),
    si ferma alla prima circolare già vista — evita di riscaricare tutte
    le pagine ogni volta che lo script gira.

    Ritorna (circolari_trovate, pagina1_raggiungibile). Il secondo valore
    serve a distinguere "nessuna circolare nuova" da "il sito non risponde".
    """
    all_circulars: list[Circular] = []
    page = 1
    last_page = 1
    page1_ok = True

    while True:
        url = SCHOOL_CIRCULARS_URL if page == 1 else (
            f"{SCHOOL_CIRCULARS_URL}?tipo=comunicati&cerca=&categoria=&storico=1&aoo=&page={page}"
        )

        log.info("Scraping pagina %d: %s", page, url)
        response = fetch_with_retry(url)
        if response is None:
            if page == 1:
                page1_ok = False
            break

        soup = BeautifulSoup(response.text, "html.parser")

        if page == 1:
            last_page = get_last_page(soup)
            if max_pages:
                last_page = min(last_page, max_pages)

        items = soup.select("div.media.at-item.w-100")
        if not items:
            break

        hit_known = False
        for item in items:
            circular = parse_item(item)
            if not circular:
                continue
            if stop_at_known and circular.id in sent_items:
                hit_known = True
                break
            all_circulars.append(circular)

        if hit_known:
            log.info("Trovata circolare già inviata, mi fermo qui (pagina %d).", page)
            break

        if page >= last_page:
            break
        page += 1
        time.sleep(1)  # pausa educata tra le richieste

    return all_circulars, page1_ok


def filter_circulars(
    circulars: list[Circular],
    tipologia: Optional[str] = None,
    include_keywords: Optional[list[str]] = None,
    exclude_keywords: Optional[list[str]] = None,
) -> list[Circular]:
    result = circulars

    if tipologia:
        result = [c for c in result if tipologia.lower() in c.tipologia.lower()]

    if include_keywords:
        result = [
            c for c in result
            if any(kw.lower() in (c.title + " " + c.description).lower() for kw in include_keywords)
        ]

    if exclude_keywords:
        result = [
            c for c in result
            if not any(kw.lower() in (c.title + " " + c.description).lower() for kw in exclude_keywords)
        ]

    return result


# --- MESSAGE FORMATTING ---

def format_message(c: Circular) -> str:
    lines = ["📢 <b>Nuova circolare</b>", f"<b>{html.escape(c.title)}</b>"]
    if c.description:
        lines.append(html.escape(c.description))
    if c.published:
        lines.append(f"📅 Pubblicato: {html.escape(c.published)}")
    if c.tipologia:
        lines.append(f"🏷 Tipologia: {html.escape(c.tipologia)}")
    if c.attachments:
        lines.append("📎 Allegati:")
        for filename, href in c.attachments:
            lines.append(f'• <a href="{html.escape(href)}">{html.escape(filename)}</a>')
    msg = "\n".join(lines)

    if len(msg) > TELEGRAM_MSG_LIMIT:
        # Taglia la descrizione per stare dentro al limite, invece di
        # far fallire silenziosamente l'invio.
        overflow = len(msg) - TELEGRAM_MSG_LIMIT + 20
        c.description = c.description[: max(0, len(c.description) - overflow)] + "…"
        return format_message(c)

    return msg


# --- DOWNLOAD PDF LOCALI ---

def sanitize_filename(name: str) -> str:
    """Rimuove i caratteri non validi nei nomi file su Windows/Linux e accorcia se troppo lungo."""
    name = re.sub(r'[<>:"/\\|?*]', "_", name).strip()
    if not name:
        name = "allegato"
    if not name.lower().endswith(".pdf"):
        name += ".pdf"
    if len(name) > 150:
        name = name[:146] + ".pdf"
    return name


def download_attachments(c: Circular) -> list[Path]:
    """Scarica tutti gli allegati di una circolare in downloads/. Ritorna i path salvati."""
    if not c.attachments:
        return []

    DOWNLOAD_DIR.mkdir(exist_ok=True)
    saved: list[Path] = []
    for filename, href in c.attachments:
        response = fetch_with_retry(href)
        if response is None:
            log.warning("Download fallito per '%s' (%s)", filename, href)
            continue

        dest = DOWNLOAD_DIR / sanitize_filename(filename)
        try:
            dest.write_bytes(response.content)
            saved.append(dest)
            log.info("Salvato allegato locale: %s", dest.name)
        except OSError as e:
            log.warning("Impossibile scrivere %s: %s", dest, e)

    return saved


def prune_old_pdfs(keep: int) -> None:
    """Tiene solo i `keep` PDF più recenti in downloads/, cancella gli altri in sequenza."""
    if not DOWNLOAD_DIR.exists():
        return
    files = sorted(DOWNLOAD_DIR.glob("*.pdf"), key=lambda p: p.stat().st_mtime, reverse=True)
    for old in files[keep:]:
        try:
            old.unlink()
            log.info("Rimosso allegato vecchio: %s", old.name)
        except OSError as e:
            log.warning("Impossibile rimuovere %s: %s", old, e)


# --- TELEGRAM ---

async def send_with_retry(bot: Bot, chat_id: str, text: str, max_retries: int = 3) -> bool:
    for attempt in range(1, max_retries + 1):
        try:
            await bot.send_message(
                chat_id=chat_id,
                text=text,
                parse_mode="HTML",
                disable_web_page_preview=True,
            )
            return True
        except RetryAfter as e:
            wait = float(getattr(e, "retry_after", 5))
            log.warning("Rate-limited da Telegram, aspetto %.1fs", wait)
            await asyncio.sleep(wait)
        except (TimedOut, NetworkError) as e:
            log.warning("Errore di rete (tentativo %d/%d): %s", attempt, max_retries, e)
            await asyncio.sleep(2 * attempt)
        except Exception as e:
            log.error("Errore invio Telegram non recuperabile (chat %s): %s", chat_id, e)
            return False
    return False


# --- LOGICA CENTRALE (usata sia dal one-shot che dal --serve) ---

async def perform_check(bot: Bot, args: argparse.Namespace) -> None:
    sent_items = load_sent_items()

    max_pages = args.max_pages
    stop_at_known = not args.first_run
    if args.first_run:
        max_pages = max_pages or 14  # tutto lo storico
        log.info("Modalità first-run: scarico tutto lo storico senza inviare notifiche.")

    log.info("Controllo nuove circolari...")
    circulars, page1_ok = scrape_circulars(sent_items, max_pages=max_pages, stop_at_known=stop_at_known)
    track_scrape_health(page1_ok)
    log.info("Trovate %d circolari nuove.", len(circulars))

    include_kw = [k.strip() for k in args.include_keywords.split(",")] if args.include_keywords else None
    exclude_kw = [k.strip() for k in args.exclude_keywords.split(",")] if args.exclude_keywords else None

    before = len(circulars)
    circulars = filter_circulars(circulars, tipologia=args.tipologia, include_keywords=include_kw, exclude_keywords=exclude_kw)
    if before != len(circulars):
        log.info("Filtrate: %d -> %d", before, len(circulars))

    if args.first_run:
        for c in circulars:
            sent_items.add(c.id)
        save_sent_items(sent_items)
        log.info("First-run completato: %d circolari marcate come già viste.", len(circulars))
        return

    if args.dry_run:
        for c in circulars:
            log.info("[DRY RUN] Invierei: %s — %s", c.title, c.description)
        return

    new_count = 0
    # Invia dal più vecchio al più recente, così l'ordine in chat è cronologico
    for c in reversed(circulars):
        msg = format_message(c)
        all_ok = True
        for chat_id in CHAT_IDS:
            ok = await send_with_retry(bot, chat_id, msg)
            if not ok:
                all_ok = False
                log.error("Invio fallito per '%s' verso chat %s.", c.title, chat_id)

        if all_ok:
            sent_items.add(c.id)
            new_count += 1
            log.info("Inviata a %d chat: %s", len(CHAT_IDS), c.title)
            save_sent_items(sent_items)  # salva incrementalmente: se crasha a metà non si perde il lavoro fatto

            if not args.no_download:
                download_attachments(c)

            await asyncio.sleep(1)

    if not args.no_download:
        prune_old_pdfs(args.keep_pdfs)

    log.info("Fatto. Inviate %d nuove circolari.", new_count)


# --- MODALITÀ SERVE: comando /ultime + check periodico in background ---

async def cmd_ultime(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    n = 5
    if context.args:
        try:
            n = max(1, min(20, int(context.args[0])))
        except ValueError:
            pass

    await update.message.reply_text(f"Cerco le ultime {n} circolari...")
    sent_items = load_sent_items()
    circulars, page1_ok = scrape_circulars(sent_items, max_pages=1, stop_at_known=False)

    if not page1_ok:
        await update.message.reply_text("Il sito della scuola non è raggiungibile in questo momento.")
        return
    if not circulars:
        await update.message.reply_text("Nessuna circolare trovata.")
        return

    for c in circulars[:n]:
        await update.message.reply_text(format_message(c), parse_mode="HTML", disable_web_page_preview=True)


async def scheduled_job(context: ContextTypes.DEFAULT_TYPE) -> None:
    args: argparse.Namespace = context.job.data
    await perform_check(context.bot, args)


def run_serve(args: argparse.Namespace) -> None:
    if not BOT_TOKEN or not CHAT_IDS:
        log.error("BOT_TOKEN o CHAT_ID mancanti. Impostali in un file .env o come variabili d'ambiente.")
        return

    application = Application.builder().token(BOT_TOKEN).build()
    application.add_handler(CommandHandler("ultime", cmd_ultime))

    application.job_queue.run_repeating(
        scheduled_job,
        interval=args.interval * 60,
        first=10,
        data=args,
        name="check_circolari",
    )

    log.info("Bot in modalità --serve: check ogni %d minuti, /ultime attivo.", args.interval)
    application.run_polling()


# --- ENTRY POINT ---

async def run_once(args: argparse.Namespace) -> None:
    if not args.dry_run and (not BOT_TOKEN or not CHAT_IDS):
        log.error("BOT_TOKEN o CHAT_ID mancanti. Impostali in un file .env o come variabili d'ambiente.")
        return
    bot = Bot(token=BOT_TOKEN) if BOT_TOKEN else None
    await perform_check(bot, args)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Bot notifiche circolari scolastiche")
    parser.add_argument("--max-pages", type=int, default=1, help="Numero massimo di pagine da controllare (default: 1)")
    parser.add_argument("--dry-run", action="store_true", help="Scarica e mostra le circolari senza inviarle")
    parser.add_argument("--first-run", action="store_true", help="Bootstrap: marca tutto lo storico come già visto senza notificare")
    parser.add_argument("--tipologia", type=str, default=None, help="Invia solo circolari che contengono questa tipologia (es. 'Famiglie')")
    parser.add_argument("--include-keywords", type=str, default=None, help="Invia solo se titolo/descrizione contiene una di queste parole (separate da virgola)")
    parser.add_argument("--exclude-keywords", type=str, default=None, help="NON inviare se titolo/descrizione contiene una di queste parole (separate da virgola)")
    parser.add_argument("--keep-pdfs", type=int, default=1, help="Quanti PDF locali tenere in downloads/ (default: 1, cancella i precedenti in sequenza)")
    parser.add_argument("--no-download", action="store_true", help="Non scaricare nessun PDF in locale")
    parser.add_argument("--serve", action="store_true", help="Processo persistente: check periodico automatico + comando /ultime")
    parser.add_argument("--interval", type=int, default=15, help="Minuti tra un check e l'altro in modalità --serve (default: 15)")
    return parser.parse_args()


if __name__ == "__main__":
    parsed = parse_args()
    if parsed.serve:
        run_serve(parsed)
    else:
        asyncio.run(run_once(parsed))